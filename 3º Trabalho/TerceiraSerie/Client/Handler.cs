using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Client {
    public class Handler {
        private static readonly Dictionary<string, Action<string[]>> MESSAGE_HANDLERS;
        private static ushort PORT = 8080;
        
        static Handler() {
            MESSAGE_HANDLERS = new Dictionary<string, Action<string[]>>();
            MESSAGE_HANDLERS["SET"] = ProcessSetRequest;
            MESSAGE_HANDLERS["GET"] = ProcessGetRequest;
            MESSAGE_HANDLERS["KEYS"] = ProcessKeysRequest;
            MESSAGE_HANDLERS["SHUTDOWN"] = ProcessShutdownRequest;
        }

        private static void ProcessSetRequest(string[] arg1) {
            if(arg1.Length - 1 != 2) {
                Console.WriteLine("(error) Expected 2 parameters received {0}", arg1.Length - 1);
                return;
            }

            string key = arg1[1];
            string value = arg1[2];

            using (TcpClient client = new TcpClient()) {
                client.Connect(IPAddress.Loopback, PORT);

                StreamWriter output = new StreamWriter(client.GetStream());
                StreamReader input = new StreamReader(client.GetStream());

                // Send request type line
                output.WriteLine("SET {0} {1}", key, value);
                output.Flush();

                string line = input.ReadLine();
                input.ReadLine();

                if (line != "OK")
                    throw new Exception("Invalid response format");

                output.Close();
                client.Close();
            } 
        }

        private static void ProcessGetRequest(string[] arg1) {
            if (arg1.Length - 1 != 1) {
                Console.WriteLine("(error) Expected 1 parameters received {0}", arg1.Length - 1);
                return;
            }

            string key = arg1[1];

            using (TcpClient client = new TcpClient()) {
                client.Connect(IPAddress.Loopback, PORT);

                StreamWriter output = new StreamWriter(client.GetStream());
                StreamReader input = new StreamReader(client.GetStream());

                // Send request type line
                output.WriteLine("GET {0}", key);
                output.Flush();
                string line = input.ReadLine();
                input.ReadLine();

                output.Close();
                client.Close();
                if (line == "(nil)")
                    Console.WriteLine(line);
                else if (line.StartsWith("\"") && line.EndsWith("\"")) {
                    Console.WriteLine(line.Substring(1, line.Length - 2));
                }
                else
                    throw new Exception("Invalid response format");
            }
        }

        private static void ProcessKeysRequest(string[] arg1) {
            if (arg1.Length != 1) {
                Console.WriteLine("(error) Expected 0 parameters received {0}", arg1.Length);
                return;
            }

            using (TcpClient client = new TcpClient()) {
                client.Connect(IPAddress.Loopback, PORT);

                StreamWriter output = new StreamWriter(client.GetStream());
                StreamReader input = new StreamReader(client.GetStream());

                // Send request type line
                output.WriteLine("KEYS");
                output.Flush();
                string line = input.ReadLine();
                while (line != null && !line.Equals("")) {
                    Console.WriteLine(line);
                    line = input.ReadLine();
                }

                output.Close();
                client.Close();
            }
        }

        private static void ProcessShutdownRequest(string[] arg1) {
            if (arg1.Length != 1) {
                Console.WriteLine("(error) Expected 0 parameters received {0}", arg1.Length);
                return;
            }

            using (TcpClient client = new TcpClient()) {
                client.Connect(IPAddress.Loopback, PORT);

                StreamWriter output = new StreamWriter(client.GetStream());

                // Send request type line
                output.Write("SHUTDOWN");
                output.Flush();

                output.Close();
                client.Close();
            }
        }

        public void Run(string request) {
            string[] cmd = request.Trim().Split(' ');
            Action<string[]> handler = null;
            if (cmd.Length < 1 || !MESSAGE_HANDLERS.TryGetValue(cmd[0], out handler)) {
                return;
            }
            // Dispatch request processing
            handler(cmd);
        }
    }
}