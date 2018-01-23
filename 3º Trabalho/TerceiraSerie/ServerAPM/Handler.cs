using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ServerAPM {
    public class Handler {
        /// <summary>
        /// Data structure that supports message processing dispatch.
        /// </summary>
        private static readonly Dictionary<string, Action<string[], StreamWriter, Logger, Listener>> MESSAGE_HANDLERS;
        private static Dictionary<string, BGetValues> bgetWaiters;  // Estrutura de dados para manter as tasks a realizar quando a chave correspondente for inserida no servidor
        /// <summary>
        /// The handler's input (from the TCP connection)
        /// </summary>
        private readonly StreamReader input;

        /// <summary>
        /// The handler's output (to the TCP connection)
        /// </summary>
        private readonly StreamWriter output;
        private Logger log;

        static Handler() {
            bgetWaiters = new Dictionary<string, BGetValues>();
            MESSAGE_HANDLERS = new Dictionary<string, Action<string[], StreamWriter, Logger, Listener>>();
            MESSAGE_HANDLERS["SET"] = ProcessSetMessage;
            MESSAGE_HANDLERS["GET"] = ProcessGetMessage;
            MESSAGE_HANDLERS["BGET"] = ProcessBGetMessage;
            MESSAGE_HANDLERS["KEYS"] = ProcessKeysMessage;
            MESSAGE_HANDLERS["SHUTDOWN"] = ProcessShutDownMessage;
        }

        /// <summary>
        ///	Initiates an instance with the given parameters.
        /// </summary>
        /// <param name="connection">The TCP connection to be used.</param>
        /// <param name="log">the Logger instance to be used.</param>
        public Handler(Stream connection, Logger log) {
            this.log = log;
            output = new StreamWriter(connection);
            input = new StreamReader(connection);
        }

        /// <summary>
        /// Handles SET messages.
        /// </summary>
        private static void ProcessSetMessage(string[] cmd, StreamWriter wr, Logger log, Listener listener) {
            if (cmd.Length - 1 != 2) {
                string errorMessage = String.Format("ERROR - Handler: ProcessSetMessage - Wrong number of arguments (given {0}, expected 2)", cmd.Length - 1);
                log.LogMessage(errorMessage);
                wr.WriteLine(errorMessage);
            }
            string key = cmd[1];
            string value = cmd[2];

            log.LogMessage(String.Format("Handler: ProcessSetMessage - Setting key: {0} with value: {1}", key, value));
            Store.Instance.Set(key, value);

            log.LogMessage("Handler: ProcessSetMessage - New Pair of key-value stored with success");
            wr.WriteLine("OK\n");
            BGetValues bgetTask;
            if(bgetWaiters.TryGetValue(key, out bgetTask))
                bgetTask.taskCompletionSource.SetResult(value);
        }

        /// <summary>
        /// Handles GET messages.
        /// </summary>
        private static void ProcessGetMessage(string[] cmd, StreamWriter wr, Logger log, Listener listener) {
            if (cmd.Length - 1 != 1) {
                string errorMessage = String.Format("ERROR - Handler: ProcessGetMessage - Wrong number of arguments (given {0}, expected 1)", cmd.Length - 1);
                log.LogMessage(errorMessage);
                wr.WriteLine(errorMessage);
            }
            string key = cmd[1];
            string value = Store.Instance.Get(key);

            if (value != null) {
                log.LogMessage(String.Format("Handler: ProcessGetMessage - key: {0} correspondes to value: {1}", key, value));
                wr.WriteLine("\"{0}\"\n", value);
            }
            else {
                log.LogMessage(String.Format("Handler: ProcessGetMessage - key {0} does not have any value", key));
                wr.WriteLine("(nil)\n");
            }
        }

        /**
         * Controla o comando BGET sendo que quando este comando é executado caso não exista ainda nenhum valor associado à chave recebida como comando, então
         * e tenha sido passado como timeout o valor zero então é logo enviado para o cliente a resposta nil. Caso contrário é adicionada ao dicionário  
         * bgetWaiters a task criada neste método e é feita uma espera através do método de sincronização Task.wait que esta task esteja completa, o que 
         * acontece quando um cliente insere no servidor um valor para a chave passada aqui como parâmetro, a qual também é a chave do dicionário para recuperar
         * a task e realizar set result para ser possivel continuar, quando isso acontecer é enviado ao cliente como resposta o valor associado à chave. Caso isso
         * não acontece dentro do timeout especificado pelo cliente então é enviado para o cliente a resposta nil. A task uma vez inserida no dicionário bgetWaiters
         * apenas é removida quando algum não existe nenhum cliente que ainda possa benificiar dela, ou seja não existe nenhum cliente com o comando BGet ainda 
         * pendente sobre a mesma chave, isto é controlado pelo valor com campo waitingClients da instância de BGetValues inserida no dicionário para a chave, pois 
         * assim que este chegar a zero significa que já não existe nenhum cliente pendente neste situação. A manipulação do dicionário bgetWaiters é feita dentro de
         * uma zona de exclusão mútua uma vez que é possivel que vários clientes tentem realizar o comando BGet ao mesmo tempo e sem essa proteção acontecia que os 
         * vários clientes viam que não havia nenhum cliente á espera que fosse inserido no servidor um valor associado à chave e assim os vários clientes tentavam
         * inserir no dicionário a mesma task o que não é possivel. Ao invés do uso de uma zona de exclusão mútua podia ser feita uma sincronização non-blocking
         * no entanto a complexidade que isso envolveria não benificia o código, sendo que o lock é mantido apenas durante o tempo estritamente necessário e o menor
         * possivel, sendo assim benéfico o seu uso
         */
        private static void ProcessBGetMessage(string[] cmd, StreamWriter wr, Logger log, Listener listener) {
            if (cmd.Length - 1 != 2) {
                string errorMessage = String.Format("ERROR - Handler: ProcessBGetMessage - Wrong number of arguments (given {0}, expected 2)", cmd.Length - 1);
                log.LogMessage(errorMessage);
                wr.WriteLine(errorMessage);
                return;
            }

            string key = cmd[1];
            string value;
            int timeout;
            if(!int.TryParse(cmd[2], out timeout)) {
                string errorMessage = String.Format("ERROR - Handler: ProcessBGetMessage - Wrong type of argument timeout should be int it was {0}", cmd[2].GetType());
                log.LogMessage(errorMessage);
                wr.WriteLine(errorMessage);
                return;
            }

            if (Store.Instance.ExistsKey(key)) {
                value = Store.Instance.Get(key);
                log.LogMessage(String.Format("Handler: ProcessBGetMessage - key: {0} exists and correspondes to value: {1}", key, value));
                wr.WriteLine("\"{0}\"\n", value);
                return;
            }

            if(timeout == 0) {
                log.LogMessage(String.Format("Handler: ProcessGetMessage - key {0} does not have any value and wasn't provided a timeout", key));
                wr.WriteLine("(nil)\n");
                return;
            }

            BGetValues bgetValue;
            lock (bgetWaiters) {
                if (!bgetWaiters.ContainsKey(key)) {
                    TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
                    bgetValue = new BGetValues(tcs);
                    bgetWaiters.Add(key, bgetValue);
                }
                else {
                    bgetWaiters.TryGetValue(key, out bgetValue);
                    bgetValue.waitingClients++;
                }
            }

            if (bgetValue.taskCompletionSource.Task.Wait(timeout)) {
                value = bgetValue.taskCompletionSource.Task.Result;
                log.LogMessage(String.Format("Handler: ProcessBGetMessage - key: {0} exists and correspondes to value: {1}", key, value));
                wr.WriteLine("\"{0}\"\n", value);
                bgetWaiters.Remove(key);
                return;
            }
            log.LogMessage(String.Format("Handler: ProcessGetMessage - key {0} does not have any value and one wasn't provided within the timeout", key));
            wr.WriteLine("(nil)\n");
            if (bgetValue.waitingClients == 1)
                bgetWaiters.Remove(key);
        }

        /// <summary>
        /// Handles KEYS messages.
        /// </summary>
        private static void ProcessKeysMessage(string[] cmd, StreamWriter wr, Logger log, Listener listener) {
            if (cmd.Length - 1 != 0) {
                string errorMessage = String.Format("ERROR - Handler: ProcessKeysMessage - Wrong number of arguments (given {0}, expected 0)", cmd.Length - 1);
                log.LogMessage(errorMessage);
                wr.WriteLine(errorMessage);
            }
            int ix = 1;

            log.LogMessage("Handler: ProcessKeysMessage - The server contains the following keys:");
            foreach (string key in Store.Instance.Keys()) {
                String set = String.Format("{0}) \"{1}\"", ix++, key);
                log.LogMessage(set);
                wr.WriteLine(set);
            }
            wr.WriteLine();
        }

        /// <summary>
        /// Handles SHUTDOWN message
        /// </summary>
        /// <param name="cmd">o conjunto de comandos, que neste consiste na palavra SHUTDOWN</param>
        /// <param name="wr">o writer para onde deve ser escrito o outpu</param>
        /// <param name="log">o logger usado para fazer log da aplicação</param>
        /// <param name="listener">o listener correspondente ao servidor que vai ser desligado</param>
        private static void ProcessShutDownMessage(string[] cmd, StreamWriter wr, Logger log, Listener listener) {
            if (cmd.Length - 1 != 0) {
                string errorMessage = String.Format("ERROR - Handler: ProcessShutdownMessage - Wrong number of arguments (given {0}, expected 0)", cmd.Length - 1);
                log.LogMessage(errorMessage);
                wr.WriteLine(errorMessage);
            }
            listener.ShutdownAndWaitTermination();
        }

        /// <summary>
        /// Performs request servicing.
        /// </summary>
        public void Run(string request, Listener listener) {
            try {
                string[] cmd = request.Trim().Split(' ');
                Action<string[], StreamWriter, Logger, Listener> handler = null;
                if (cmd.Length < 1 || !MESSAGE_HANDLERS.TryGetValue(cmd[0], out handler)) {
                    log.LogMessage("ERROR - Handler: Run - Unknown message type");
                    return;
                }
                // Dispatch request processing
                handler(cmd, output, log, listener);
                output.Flush();
            }
            catch (IOException ioe) {
                log.LogMessage(String.Format("ERROR - Handler: Run - Connection closed by client {0}", ioe.Message));
            }
            finally {
                input.Close();
                output.Close();
            }
        }

        /**
         * Classe para representar a associação de uma task a realizar perante a inserção no servidor de uma dada chave
         * e o número de clientes à espera para realizar essa task. O número de clientes é usado para saber quando deve
         * ser removida do Dictionary a task, isso acontece quando não há cliente á espera da chave ou quando a task é
         * executada. 
         */
        private class BGetValues {
            public readonly TaskCompletionSource<string> taskCompletionSource;
            public int waitingClients;

            public BGetValues(TaskCompletionSource<string> taskCompletionSource) {
                this.taskCompletionSource = taskCompletionSource;
                waitingClients = 1;
            }
        }
    }
}