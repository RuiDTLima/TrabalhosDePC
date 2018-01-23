using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TestServer {
    /**
     *  Aplicação de linha de comandos para testar o correcto funcionado tanto do servidor definido com APM como o definido com TAP, bastando
     *  para isso iniciar o servidor que se deseja testar antes de correr esta aplicação. Aqui são executados um conjunto de comandos pre-definidos
     *  por vários threads cada uma a realizar várias conecções ao servidor. O correcto funcionamento do servidor é validado avaliando o output na 
     *  consola, devendo ser tomado em consideração que a escrita na consola pode aparecer fora de ordem. Devido funcionamento do comando BGET para
     *  um melhor uso dos testes aconselha-se a que depois de cada teste o servidor seja reiniciado.
     */
    class ServerClient {
        private static readonly Dictionary<string, Action<Tuple<string[], TcpClient>>> MESSAGE_HANDLERS;
        private static string[] requests = { "SET hoje 7", "SET amanha 8", "SET depois 40", "GET amanha", "GET hoje", "GET depois", "KEYS" };
        private static readonly int PORT = 8080;

        static ServerClient() {
            MESSAGE_HANDLERS = new Dictionary<string, Action<Tuple<string[], TcpClient>>>();
            MESSAGE_HANDLERS["SET"] = ProcessSetRequest;
            MESSAGE_HANDLERS["GET"] = ProcessGetRequest;
            MESSAGE_HANDLERS["BGET"] = ProcessBGetRequest;
            MESSAGE_HANDLERS["KEYS"] = ProcessKeysRequest;
        }

        static void Main(string[] args) {
            Thread[] threads = new Thread[10];
            for (int i = 0; i < threads.Length; i++) {
                int li = i;
                threads[i] = new Thread(() => {
                    foreach (string request in requests) {
                        using (TcpClient client = new TcpClient()) {
                            client.Connect(IPAddress.Loopback, PORT);
                            string[] cmd = request.Trim().Split(' ');

                            Action<Tuple<string[], TcpClient>> serverClient = null;

                            if (!MESSAGE_HANDLERS.TryGetValue(cmd[0], out serverClient)) {
                                return;
                            }
                            // Dispatch request processing
                            serverClient(new Tuple<String[], TcpClient>(cmd, client));
                        }
                    }
                });
            }

            for(int i = 0; i < threads.Length; i++) {
                threads[i].Start();
            }

            Thread thread = new Thread(() => TestBGetCommand());
            thread.Start();
        }

        private static void ProcessSetRequest(Tuple<string[], TcpClient> args) {
            string[] param = args.Item1;
            TcpClient client = args.Item2;

            string key = param[1];
            string value = param[2];

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

        private static void ProcessGetRequest(Tuple<string[], TcpClient> args) {
            string[] param = args.Item1;
            TcpClient client = args.Item2;

            string key = param[1];

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

        private static void ProcessBGetRequest(Tuple<string[], TcpClient> args) {
            string[] param = args.Item1;
            TcpClient client = args.Item2;

            string key = param[1];
            string timeout = param[2];

            StreamWriter output = new StreamWriter(client.GetStream());
            StreamReader input = new StreamReader(client.GetStream());

            // Send request type line
            output.WriteLine("BGET {0} {1}", key, timeout);
            output.Flush();
            string line = input.ReadLine();
            input.ReadLine();

            output.Close();
            client.Close();
            if (line == "(nil)")
                Console.WriteLine("BGET response {0}",line);
            else if (line.StartsWith("\"") && line.EndsWith("\"")) {
                Console.WriteLine("BGET response {0}", line.Substring(1, line.Length - 2));
            }
            else
                throw new Exception("Invalid response format");
        }

        private static void ProcessKeysRequest(Tuple<string[], TcpClient> args) {
            TcpClient client = args.Item2;

            StreamWriter output = new StreamWriter(client.GetStream());
            StreamReader input = new StreamReader(client.GetStream());

            // Send request type line
            output.WriteLine("KEYS");
            output.Flush();
            string line = input.ReadLine();
            while (line != null && !line.Equals("")){
                Console.WriteLine(line);
                line = input.ReadLine();
            }

            output.Close();
            client.Close();
        }

        private static void TestBGetCommand() {
            using (TcpClient client = new TcpClient()) {
                client.Connect(IPAddress.Loopback, PORT);
                string[] cmd = { "BGET", "ISEL", "0" };

                Action<Tuple<string[], TcpClient>> serverClient = null;

                if (!MESSAGE_HANDLERS.TryGetValue(cmd[0], out serverClient)) {
                    return;
                }
                // Dispatch request processing
                serverClient(new Tuple<String[], TcpClient>(cmd, client));
            }

            Thread bgetThread1 = new Thread(() => WaitBGet());

            Thread bgetThread2 = new Thread(() => WaitBGet());

            Thread setThread = new Thread(() => {
                using (TcpClient client = new TcpClient()) {
                    client.Connect(IPAddress.Loopback, PORT);
                    string[] cmd = { "SET", "ISEL", "20" };

                    Action<Tuple<string[], TcpClient>> serverClient = null;

                    if (!MESSAGE_HANDLERS.TryGetValue(cmd[0], out serverClient)) {
                        return;
                    }
                    // Dispatch request processing
                    serverClient(new Tuple<String[], TcpClient>(cmd, client));
                }
            });

            bgetThread1.Start();
            bgetThread2.Start();
            Thread.Sleep(1000);
            setThread.Start();
        }

        private static void WaitBGet() {
            using (TcpClient client = new TcpClient()) {
                client.Connect(IPAddress.Loopback, PORT);
                string[] cmd = { "BGET", "ISEL", "60000" };

                Action<Tuple<string[], TcpClient>> serverClient = null;

                if (!MESSAGE_HANDLERS.TryGetValue(cmd[0], out serverClient)) {
                    return;
                }
                // Dispatch request processing
                serverClient(new Tuple<String[], TcpClient>(cmd, client));
            }
        }
    }
}