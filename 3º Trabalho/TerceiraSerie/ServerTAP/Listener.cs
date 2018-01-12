using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerTAP {
    class Listener {
        private readonly int PORT_NUMBER = 8080;    // TCP port number in use
        private readonly int MAXREQUESTS = 10;  // maximum request the server can handle at the same time
        private readonly int BUFFER_SIZE = 1024;
        private readonly TcpListener server;
        private readonly Logger log;
        private const int WAIT_FOR_IDLE_TIME = 10000;
        private const int POLLING_INTERVAL = WAIT_FOR_IDLE_TIME / 100;
        private volatile Task task;
        private CancellationTokenSource cancelationTokenSource;

        /**
         *  Inicia as operações Aync do servidor
         */
        public Listener(Logger log) {
            this.log = log;
            server = new TcpListener(IPAddress.Loopback, PORT_NUMBER);
            server.Start();
        }

        /**
         * Indica se o shutdown ao servidor já foi pedido pelo cliente.
         */
        public bool isShutdown() {
            return cancelationTokenSource.IsCancellationRequested;
        }

        /**
         * Ponto de entrada da execução do servidor. Inicia a aceitação de coneções
         * com clientes.
         */
        public async Task Run(CancellationTokenSource token) {
            cancelationTokenSource = token; // tokenSource sobre o qual deve ser terminado a execução do servidor
            var cancelationToken = token.Token;
            var runningTasks = new HashSet<Task>(); // conjunto de tasks que estão a executar uma operação sobre a coneção com o cliente

            log.LogMessage("Listener: Run - Start listening to connections");
            do {
                try {
                    var connection = await server.AcceptTcpClientAsync();

                    runningTasks.Add(ProcessConnectionAsync(connection));

                    if (runningTasks.Count >= MAXREQUESTS)
                        runningTasks.Remove(await Task.WhenAny(runningTasks));  // caso tenha sido ultrapassado o limite de pedidos que o servidor pode responder em simultaneo, deve-se esperar de forma asincrona que uma das operações termine para remover do conjunto de tasks em execução pelo menos uma task
                } catch(SocketException e) {
                    log.LogMessage(String.Format("ERROR - Listener: Run - Socket Exception error code was {0}", e.ErrorCode));
                } catch(Exception e) {
                    log.LogMessage(String.Format("ERROR - Listener: Run - Occured Exception {0}", e.Message));
                }
            } while (!cancelationToken.IsCancellationRequested);
            task = Task.WhenAll(runningTasks);
            await task; // quando o servidor é para terminar espera pela terminação das coneções já activas representadas pelas tasks em task
            log.LogMessage("Listener: Run - finish listening to connections");
        }

        /**
         * Código a executar pela task que esta a tratar da coneção com um cliente. Aqui é lido a mensagem do cliente, indicando a operação que deseja
         * executar, sendo depois esta executada através do Handler.
         */
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
                
            } catch(Exception e) {
                log.LogMessage(String.Format("ERROR - Listener: ProcessConnectionAsync - Exception {0} occured", e.Message));
            } finally {
                if (stream != null)
                    stream.Close();
                connection.Close();
            }
        }

        /**
         * Método chamado para definir a terminação do funcionamento do servidor
         * Momento a partir do qual não são aceites novas conecções, tentando-se
         * terminar as conecções activas. 
         */
        public void ShutdownAndWaitTermination() {
            // Stop listening.
            server.Stop();
            log.LogMessage("Listener: ShutdownAndWaitTermination - Finish server");
            cancelationTokenSource.Cancel();
            task.Wait();
        }
    }
}