using System;
using System.Collections.Generic;
using System.IO;

namespace ServerAPM {
    class Handler {
        /// <summary>
        /// Data structure that supports message processing dispatch.
        /// </summary>
        private static readonly Dictionary<string, Action<string[], StreamWriter, Logger>> MESSAGE_HANDLERS;

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
            MESSAGE_HANDLERS = new Dictionary<string, Action<string[], StreamWriter, Logger>>();
            MESSAGE_HANDLERS["SET"] = ProcessSetMessage;
            MESSAGE_HANDLERS["GET"] = ProcessGetMessage;
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
        private static void ProcessSetMessage(string[] cmd, StreamWriter wr, Logger log) {
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
        }

        /// <summary>
        /// Handles GET messages.
        /// </summary>
        private static void ProcessGetMessage(string[] cmd, StreamWriter wr, Logger log) {
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

        /// <summary>
        /// Handles KEYS messages.
        /// </summary>
        private static void ProcessKeysMessage(string[] cmd, StreamWriter wr, Logger log) {
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

        private static void ProcessShutDownMessage(string[] cmd, StreamWriter wr, Logger log) {
            if (cmd.Length - 1 != 0) {
                string errorMessage = String.Format("ERROR - Handler: ProcessShutdownMessage - Wrong number of arguments (given {0}, expected 0)", cmd.Length - 1);
                log.LogMessage(errorMessage);
                wr.WriteLine(errorMessage);
            }
            Listener listener = new Listener();
            listener.ShutdownAndWaitTermination();
        }

        /// <summary>
        /// Performs request servicing.
        /// </summary>
        public void Run(string request) {
            try {
                string[] cmd = request.Trim().Split(' ');
                Action<string[], StreamWriter, Logger> handler = null;
                if (cmd.Length < 1 || !MESSAGE_HANDLERS.TryGetValue(cmd[0], out handler)) {
                    log.LogMessage("ERROR - Handler: Run - Unknown message type");
                    return;
                }
                // Dispatch request processing
                handler(cmd, output, log);
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
    }
}