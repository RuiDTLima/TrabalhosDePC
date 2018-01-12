using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ServerAPM {
    public sealed class Listener {
        private readonly int PORT_NUMBER = 8080;    // TCP port number in use
        private readonly int MAXREQUESTS = 10;  // maximum request the server can handle at the same time
        private readonly int MAX_RECURSIVE_IO_CALL = 2;
        private readonly TcpListener server;
        private readonly Logger log;
        private const int WAIT_FOR_IDLE_TIMEOUT = 10000;
        private const int POLLING_INTERVAL = WAIT_FOR_IDLE_TIMEOUT / 100;
        private volatile bool isShutingDown;
        private volatile int requestCount;  // current request count the server is processing
        private GenericAsyncResult<int> listenAsyncResult;
        private ThreadLocal<int> recursiveIOCall = new ThreadLocal<int>();

        /// <summary> Initiates a tracking server instance.</summary>
        public Listener(Logger log) {
            this.log = log;
            server = new TcpListener(IPAddress.Loopback, PORT_NUMBER);
            server.Start();
        }
        
        /**
         * Indica se o shutdown ao servidor já foi pedido pelo cliente. 
         */
        public bool isShutdown() {
            return isShutingDown;
        }

        /// <summary>
        ///	Server's main loop implementation.
        /// </summary>
        public IAsyncResult Run() {
            log.LogMessage("Listener - Start listening for connections");
            listenAsyncResult = new GenericAsyncResult<int>(null, null, false);
            server.BeginAcceptTcpClient(AcceptTcpClient, null);
            return listenAsyncResult;
        }

        /**
         * Callback passado ao BeginAcceptTcpClient, o qual vê se a sua chamada foi feita asincronamente ou sincronamente.
         * Caso tenha sido feita asincronamente apenas chama o CompleteRequest o qual termina a aceitação de um cliente.
         * Caso tenha sido feita sincronamente, avalia se o CompleteRequest pode ser chamado recursivamente ou se tem de
         * ser chamado por uma thread do threadPool, caso seja a primeira opção incrementa o total de chamadas recursivas
         * já efectuadas e depois da chamada ao CompleteRequest decrementa esse valor
         */
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

        /**
         * Termina a aceitação da conecção do cliente chamando o EndAcceptTcpClient e depois de avaliar se é possível
         * aceitar mais clientes nomeadamente avaliado o valor de isShutingDown para saber se é necessário terminar o 
         * servidor e o valor da quantidade actual de pedidos a efectuar, caso seja possivel chama-se novamente o 
         * BeginAcceptTcpClient. Depois prepara-se a leitura da connecção chamando o BeginRequestConnection e definindo
         * o seu callback.
         */
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

                    int beginRequest = Interlocked.Decrement(ref requestCount); // o cliente já acabou a sua comunicação com o servidor
                    if (!isShutingDown && beginRequest == MAXREQUESTS - 1)
                        server.BeginAcceptTcpClient(AcceptTcpClient, null); // reinicia-se uma connecção com um cliente aceitando novos clientes
                    else if(isShutingDown && beginRequest == 0) {
                        log.LogMessage("Listener - server finish in Callback");
                        listenAsyncResult.SetResult(0); // termina a execução do listener
                    }
                });
            } catch(SocketException e) {
                log.LogMessage(String.Format("ERROR - Listener: CompleteRequest - Socket Exception error code was {0}", e.ErrorCode));
            } catch(InvalidOperationException e) {
                log.LogMessage(String.Format("ERROR - Listener: CompleteRequest - Invalid Operation error message was {0}", e.Message));
            }
        }

        /**
         *  A comunicação própriamente dita com o cliente, sendo iniciado a leitura dos pedidos feitos pelo cliente
         *  através da chamada ao método BeginRead do stream estabelecido com o cliente, executando-se a operação pedida
         *  pelo cliente definida no Handler. 
         */
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
                    handler.Run(request, this);
                    asyncResult.SetResult(0);   // termina-se a escuta de comandos do cliente
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

        /**
         *  Método chamado para definir a terminação do funcionamento do servidor
         *  Momento a partir do qual não são aceites novas conecções, tentando-se
         *  terminar as conecções activas. 
         */
        public void ShutdownAndWaitTermination() {
            log.LogMessage("Server was requested to finish");

            server.Stop();
            isShutingDown = true;
            log.LogMessage("Server finished");
        }
    }
}