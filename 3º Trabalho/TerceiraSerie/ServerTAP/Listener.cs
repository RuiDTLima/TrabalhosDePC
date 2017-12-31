using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerTAP {
    class Listener {
        private readonly TcpListener server;
        private readonly Logger log;
        private readonly int MAXREQUESTS = 10;  // maximum request the server can handle at the same time
        private readonly int BUFFER_SIZE = 1024;
        private const int PORT_NUMBER = 8080;
        private const int WAIT_FOR_IDLE_TIME = 10000;
        private const int POLLING_INTERVAL = WAIT_FOR_IDLE_TIME / 100;
        private volatile int requestCount;  // current request count the server is processing
        private volatile Task task;
        private CancellationTokenSource cancelationTokenSource;

        public Listener(Logger log) {
            this.log = log;
            server = new TcpListener(IPAddress.Loopback, PORT_NUMBER);
            server.Start();
        }

        public bool isShutdown() {
            return cancelationTokenSource.IsCancellationRequested;
        }

        public async Task Run(CancellationTokenSource token) {
            var cancelationToken = token.Token;
            cancelationTokenSource = token;
            var runningTasks = new HashSet<Task>();

            log.LogMessage("Listener: Run - Start listening to connections");
            do {
                try {
                    var connection = await server.AcceptTcpClientAsync();

                    runningTasks.Add(ProcessConnectionAsync(connection));

                    if (runningTasks.Count >= MAXREQUESTS)
                        runningTasks.Remove(await Task.WhenAny(runningTasks));
                } catch(SocketException e) {
                    log.LogMessage(String.Format("ERROR - Listener: Run - Socket Exception error code was {0}", e.ErrorCode));
                } catch(Exception e) {
                    log.LogMessage(String.Format("ERROR - Listener: Run - Occured Exception {0}", e.Message));
                }
            } while (!cancelationToken.IsCancellationRequested);
            task = Task.WhenAll(runningTasks);
            await task;
            log.LogMessage("Listener: Run - finish listening to connections");
        }

        private async Task ProcessConnectionAsync(TcpClient connection) {
            NetworkStream stream = null;
            try {
                stream = connection.GetStream();
                byte[] requestBuffer = new byte[BUFFER_SIZE];

                int bytesRead = await stream.ReadAsync(requestBuffer, 0, requestBuffer.Length);

                string request = Encoding.ASCII.GetString(requestBuffer, 0, bytesRead);

                log.LogMessage(String.Format("Listener: ProcessConnectionAsync - Finish reading client request and it was {0}", request));

                Handler handler = new Handler(stream, log);
                handler.Run(request, this);

                Interlocked.Increment(ref requestCount);
            } catch(Exception e) {
                log.LogMessage(String.Format("ERROR - Listener: ProcessConnectionAsync - Exception {0} occured", e.Message));
            } finally {
                if (stream != null)
                    stream.Close();
                connection.Close();
            }
        }

        public void ShutdownAndWaitTermination() {
            // Stop listening.
            server.Stop();
            log.LogMessage("Listener: ShutdownAndWaitTermination - Finish server");
            cancelationTokenSource.Cancel();
            task.Wait();
        }
    }
}