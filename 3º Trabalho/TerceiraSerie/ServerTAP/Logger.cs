using System;
using System.Collections;
using System.IO;
using System.Threading;

namespace ServerTAP {
    // Logger single-threaded.
    public class Logger {
        private readonly TextWriter writer;
        private readonly Queue messageQueue = new Queue();  // contentor onde são guardadas as mensagens à espera de serem escritas no servidor
        private readonly object hasElements = new object();
        private DateTime start_time;    // o DateTime em que o logger começou a funcionar, coincidindo com o inicio do servidor
        private int num_requests;   // o número de pedidos de log efectuados durante o tempo de vida do logger
        private bool isShutdown;
        private bool finishLog;

        public bool isLogFinished(){
            return finishLog;
        }

        public Logger() : this(Console.Out) {
        }

        public Logger(string logfile) : this(new StreamWriter(new FileStream(logfile, FileMode.Append, FileAccess.Write))) {
        }

        /**
         * Define-se onde será escrito o logger e a thread que trata dessa escrita sendo uma thread de baixa prioridade 
         */
        public Logger(TextWriter awriter) {
            num_requests = 0;
            writer = awriter;
            isShutdown = false;
            finishLog = false;
            Thread loggingThread = new Thread(new ThreadStart(Log));
            loggingThread.IsBackground = true;
            loggingThread.Start();
        }

        /**
         *  Insere uma nova mensagem no contentor de mensagens à espera de serem escritas, sendo incrementado
         *  o número de pedidos de escrita já feitos.
         */
        public void LogMessage(string msg) {
            lock (hasElements) {
                num_requests++;
                messageQueue.Enqueue(String.Format("{0}: {1}", DateTime.Now, msg));
                Monitor.Pulse(hasElements);
            }
        }

        /**
         *  Termina a execução do logger acordado a thread de escrita para terminar a escrita das mensagens que
         *  ainda estão à espera. 
         */
        public void Shutdown() {
            isShutdown = true;
            lock (hasElements) {
                Monitor.Pulse(hasElements);
            }
        }

        /**
         *  Inicia o conteudo do logger indicando a hora em que começou 
         */
        private void Start() {
            start_time = DateTime.Now;
            writer.WriteLine();
            writer.WriteLine(String.Format("::- LOG STARTED @ {0} -::", DateTime.Now));
            writer.WriteLine();
        }

        /**
         *  Encerra o conteudo do logger indicando o tempo que esteve em execução, o número de pedidos de escrita 
         *  atendidos e a hora a que terminou a execução do logger, fechando no fim a ligação ao ficheiro de escrita 
         *  do logger 
         */
        private void Stop() {
            long elapsed = DateTime.Now.Ticks - start_time.Ticks;
            writer.WriteLine();
            writer.WriteLine(String.Format("{0}: Running for {1} second(s)", DateTime.Now, elapsed / 10000000L));
            writer.WriteLine(String.Format("{0}: Number of request(s): {1}", DateTime.Now, num_requests));
            writer.WriteLine();
            writer.WriteLine(String.Format("::- LOG STOPPED @ {0} -::", DateTime.Now));
            writer.Close();
        }

        /**
         *  Método de execução da thread de baixa de prioridade. Fica num loop infinito numa espera passiva
         *  que exista novas mensagens a serem escritas no log, fazendo para isso copia do contentor de mensagens
         *  e limpa o contento geral. 
         */
        private void Log() {
            Start();
            while (true) {
                if (isShutdown)
                    break;
                lock (hasElements) {
                    Monitor.Wait(hasElements, 10000);
                }

                if (isShutdown)
                    break;

                Queue currentQueue;
                lock (hasElements) {
                    currentQueue = new Queue(messageQueue);
                    messageQueue.Clear();
                }

                foreach (string message in currentQueue) {
                    writer.WriteLine(message);
                }
            }
            Stop();
            finishLog = true;
        }
    }
}