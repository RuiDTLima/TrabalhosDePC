using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TerceiraSerie {
    public sealed class Listener {
        /// <summary>
        /// TCP port number in use.
        /// </summary>
        private readonly int PORT_NUMBER = 8080;
        private readonly int MAXREQUESTS = 10;  // maximum request the server can handle at the same time
        private readonly int MAX_RECURSIVE_IO_CALL = 2;
        private readonly TcpListener server;
        private volatile bool isShutingDown;
        private volatile int requestCount;  // current request count the server is processing
        private GenericAsyncResult<int> listenAsyncResult;
        private ThreadLocal<int> recursiveIOCall = new ThreadLocal<int>();
        private const int WAIT_FOR_IDLE_TIMEOUT = 10000;
        private const int POLLING_INTERVAL = WAIT_FOR_IDLE_TIMEOUT/100;
        private readonly Logger log;

        /// <summary> Initiates a tracking server instance.</summary>
        /// <param name="_portNumber"> The TCP port number to be used.</param>
        public Listener(Logger log) {
            this.log = log;
            server = new TcpListener(IPAddress.Loopback, PORT_NUMBER);
            server.Start();
        }

        public Listener() {
            server = new TcpListener(IPAddress.Loopback, PORT_NUMBER);
            server.Start();
        }

        /// <summary>
        ///	Server's main loop implementation.
        /// </summary>
        /// <param name="log"> The Logger instance to be used.</param>
        public IAsyncResult Run() {
            log.LogMessage("Listener - Start listening for connections");
            listenAsyncResult = new GenericAsyncResult<int>(null, null, false);
            server.BeginAcceptTcpClient(AcceptTcpClient, null);
            return listenAsyncResult;
        }

        private void AcceptTcpClient(IAsyncResult ar) {
            if (!ar.CompletedSynchronously) {
                log.LogMessage("Listener - Connection Completed Asynchronously");
                CompleteRequest(ar);
            }
            else {
                if (recursiveIOCall.Value < MAX_RECURSIVE_IO_CALL) {
                    log.LogMessage("Listener - Connection Completed Synchronously. Request to be completed recursively");
                    recursiveIOCall.Value++;
                    CompleteRequest(ar);
                    recursiveIOCall.Value--;
                }
                else {
                    log.LogMessage("Listener - Connection Completed Synchronously. Request to be completed in a new Thread");
                    ThreadPool.QueueUserWorkItem((_) => {
                        CompleteRequest(ar);
                    });
                }
            }
        }

        private void CompleteRequest(IAsyncResult ar) {
            TcpClient socket;
            try {
                socket = server.EndAcceptTcpClient(ar);

                log.LogMessage("Listener - finish connecting to client.");

                int currentRequest = Interlocked.Increment(ref requestCount);

                if (!isShutingDown && currentRequest < MAXREQUESTS)
                    server.BeginAcceptTcpClient(AcceptTcpClient, null);

                BeginRequestConnection(socket, (newAr) => {
                    try {
                        int notUsed = ((GenericAsyncResult<int>)newAr).Result;
                    }
                    catch (Exception e) {
                        log.LogMessage(String.Format("ERROR - Listener: BeginRequestConnection callback. - Exception {0} occured", e.Message));
                    }

                    int beginRequest = Interlocked.Decrement(ref requestCount);
                    if (!isShutingDown && beginRequest == MAXREQUESTS - 1)
                        server.BeginAcceptTcpClient(AcceptTcpClient, null);
                    else if(isShutingDown && beginRequest == 0) {
                        log.LogMessage("Listener - server finish in Callback");
                        listenAsyncResult.SetResult(0);
                    }
                });
            } catch(SocketException e) {
                log.LogMessage(String.Format("ERROR - Listener: CompleteRequest - Socket Exception error code was {0}", e.ErrorCode));
            }
        }

        private IAsyncResult BeginRequestConnection(TcpClient socket, AsyncCallback callback) {
            const int BUFFER_SIZE = 1024;
            GenericAsyncResult<int> asyncResult = new GenericAsyncResult<int>(callback, null, false);
            NetworkStream stream = null;
            try {
                stream = socket.GetStream();

                byte[] requestBuffer = new byte[BUFFER_SIZE];

                log.LogMessage("Beginning to Read client request");
                stream.BeginRead(requestBuffer, 0, requestBuffer.Length, (ar) => {
                    int bytesRead = stream.EndRead(ar);
                    string request = Encoding.ASCII.GetString(requestBuffer, 0, bytesRead);

                    log.LogMessage(String.Format("Finish reading client request and it was {0}", request));
                    Handler handler = new Handler(stream, log);
                    handler.Run(request);
                    asyncResult.SetResult(0);
                }, null);
            } catch(Exception e) {
                log.LogMessage(String.Format("ERROR - Listener: BeginRequestConnetion - Exception {0} occured", e.Message));
                socket.Close();

                if (stream != null)
                    stream.Close();
                asyncResult.SetException(e);
            }
            return asyncResult;
        }

        public void ShutdownAndWaitTermination() {
            log.LogMessage("Server was requested to finish");

            for (int i = 0; i < WAIT_FOR_IDLE_TIMEOUT; i += POLLING_INTERVAL) {
                if (!server.Pending())
                    break;
                Thread.Sleep(POLLING_INTERVAL);
            }

            server.Stop();
            isShutingDown = true;

            Interlocked.MemoryBarrier();
            if(requestCount == 0) {
                Console.WriteLine("Finish Shutdown");   // change to log
                listenAsyncResult.SetResult(0);
            }
            log.LogMessage("Server finished");
            int notUsed = ((GenericAsyncResult<int>)listenAsyncResult).Result;
        }
    }
}