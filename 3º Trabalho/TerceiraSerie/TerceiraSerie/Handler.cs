using System;
using System.Collections.Generic;
using System.IO;

namespace TerceiraSerie {
    class Handler {
        /// <summary>
        /// Data structure that supports message processing dispatch.
        /// </summary>
        private static readonly Dictionary<string, Action<string[], StreamWriter>> MESSAGE_HANDLERS;

        /// <summary>
        /// The handler's input (from the TCP connection)
        /// </summary>
        private readonly StreamReader input;

        /// <summary>
        /// The handler's output (to the TCP connection)
        /// </summary>
        private readonly StreamWriter output;

        static Handler() {
            MESSAGE_HANDLERS = new Dictionary<string, Action<string[], StreamWriter>>();
            MESSAGE_HANDLERS["SET"] = ProcessSetMessage;
            MESSAGE_HANDLERS["GET"] = ProcessGetMessage;
            MESSAGE_HANDLERS["KEYS"] = ProcessKeysMessage;
        }

        /// <summary>
        ///	Initiates an instance with the given parameters.
        /// </summary>
        /// <param name="connection">The TCP connection to be used.</param>
        /// <param name="log">the Logger instance to be used.</param>
        public Handler(Stream connection) {
            output = new StreamWriter(connection);
            input = new StreamReader(connection);
        }

        /// <summary>
        /// Handles SET messages.
        /// </summary>
        private static void ProcessSetMessage(string[] cmd, StreamWriter wr) {
            if (cmd.Length - 1 != 2) {
                wr.WriteLine("(error) wrong number of arguments (given {0}, expected 2)\n", cmd.Length - 1);
            }
            string key = cmd[1];
            string value = cmd[2];
            Store.Instance.Set(key, value);
            wr.WriteLine("OK\n");
        }

        /// <summary>
        /// Handles GET messages.
        /// </summary>
        private static void ProcessGetMessage(string[] cmd, StreamWriter wr) {
            if (cmd.Length - 1 != 1) {
                wr.WriteLine("(error) wrong number of arguments (given {0}, expected 1)\n", cmd.Length - 1);
            }
            string value = Store.Instance.Get(cmd[1]);
            if (value != null) {
                wr.WriteLine("\"{0}\"\n", value);
            }
            else {
                wr.WriteLine("(nil)\n");
            }
        }

        /// <summary>
        /// Handles KEYS messages.
        /// </summary>
        private static void ProcessKeysMessage(string[] cmd, StreamWriter wr) {
            if (cmd.Length - 1 != 0) {
                wr.WriteLine("(error) wrong number of arguments (given {0}, expected 0)\n", cmd.Length - 1);
            }
            int ix = 1;
            foreach (string key in Store.Instance.Keys()) {
                wr.WriteLine("{0}) \"{1}\"", ix++, key);
            }
            wr.WriteLine();
        }

        /// <summary>
        /// Performs request servicing.
        /// </summary>
        public void Run(string request) {
            try {
                string[] cmd = request.Trim().Split(' ');
                Action<string[], StreamWriter> handler = null;
                if (cmd.Length < 1 || !MESSAGE_HANDLERS.TryGetValue(cmd[0], out handler)) {
                    return;
                }
                // Dispatch request processing
                handler(cmd, output);
                output.Flush();
            }
            catch (IOException ioe) {
            }
            finally
            {
                input.Close();
                output.Close();
            }
        }
    }
}