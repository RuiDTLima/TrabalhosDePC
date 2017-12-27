/*
 * INSTITUTO SUPERIOR DE ENGENHARIA DE LISBOA
 * Licenciatura em Engenharia Informática e de Computadores
 *
 * Programação Concorrente - Inverno de 2009-2010, Inverno de 1017-2018
 * Paulo Pereira, Pedro Félix
 *
 * Código base para a 3ª Série de Exercícios.
 *
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Tracker
{
    /// <summary>
    /// Handles client requests.
    /// </summary>
    public sealed class Handler
    {
        /// <summary>
        /// Data structure that supports message processing dispatch.
        /// </summary>
        private static readonly Dictionary<string, Action<string[], StreamWriter, Logger>> MESSAGE_HANDLERS;

        static Handler()
        {
            MESSAGE_HANDLERS = new Dictionary<string, Action<string[], StreamWriter, Logger>>();
            MESSAGE_HANDLERS["SET"] = ProcessSetMessage;
            MESSAGE_HANDLERS["GET"] = ProcessGetMessage;
            MESSAGE_HANDLERS["KEYS"] = ProcessKeysMessage;            
        }

        /// <summary>
        /// Handles SET messages.
        /// </summary>
        private static void ProcessSetMessage(string[] cmd, StreamWriter wr, Logger log)
        {
            if (cmd.Length - 1 != 2)
            {
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
        private static void ProcessGetMessage(string[] cmd, StreamWriter wr, Logger log)
        {
            if(cmd.Length - 1 != 1)
            {
                wr.WriteLine("(error) wrong number of arguments (given {0}, expected 1)\n", cmd.Length-1);
            }
            string value = Store.Instance.Get(cmd[1]);            
            if(value != null)
            {
                wr.WriteLine("\"{0}\"\n", value);
            }
            else
            {
                wr.WriteLine("(nil)\n");
            }
        }

        /// <summary>
        /// Handles KEYS messages.
        /// </summary>
        private static void ProcessKeysMessage(string[] cmd, StreamWriter wr, Logger log)
        {
            if (cmd.Length -1 != 0)
            {
                wr.WriteLine("(error) wrong number of arguments (given {0}, expected 0)\n", cmd.Length - 1);
            }
            int ix = 1;
            foreach(string key in Store.Instance.Keys())
            {
                wr.WriteLine("{0}) \"{1}\"", ix++, key);
            }
            wr.WriteLine();
        }
                
        /// <summary>
        /// The handler's input (from the TCP connection)
        /// </summary>
        private readonly StreamReader input;

        /// <summary>
        /// The handler's output (to the TCP connection)
        /// </summary>
        private readonly StreamWriter output;

        /// <summary>
        /// The Logger instance to be used.
        /// </summary>
        private readonly Logger log;

        /// <summary>
        ///	Initiates an instance with the given parameters.
        /// </summary>
        /// <param name="connection">The TCP connection to be used.</param>
        /// <param name="log">the Logger instance to be used.</param>
        public Handler(Stream connection, Logger log)
        {
            this.log = log;
            output = new StreamWriter(connection);
            input = new StreamReader(connection);
        }

        /// <summary>
        /// Performs request servicing.
        /// </summary>
        public void Run()
        {
            try
            {
                string request;                
                while ((request = input.ReadLine()) != null && request != string.Empty)
                {
                    string[] cmd = request.Trim().Split(' ');
                    Action<string[], StreamWriter, Logger> handler = null;
                    if (cmd.Length < 1 || !MESSAGE_HANDLERS.TryGetValue(cmd[0], out handler))
                    {
                        log.LogMessage("(error) unnown message type");
                        return;
                    }
                    // Dispatch request processing
                    handler(cmd, output, log);
                    output.Flush();
                }
            }
            catch (IOException ioe)
            {
                // Connection closed by the client. Log it!
                log.LogMessage(String.Format("Handler - Connection closed by client {0}", ioe));
            }
            finally
            {
                input.Close();
                output.Close();
            }
        }
    }

    /// <summary>
    /// This class instances are file tracking servers. They are responsible for accepting 
    /// and managing established TCP connections.
    /// </summary>
    public sealed class Listener
    {
        /// <summary>
        /// TCP port number in use.
        /// </summary>
        private readonly int portNumber;

        /// <summary> Initiates a tracking server instance.</summary>
        /// <param name="_portNumber"> The TCP port number to be used.</param>
        public Listener(int _portNumber) { portNumber = _portNumber; }

        /// <summary>
        ///	Server's main loop implementation.
        /// </summary>
        /// <param name="log"> The Logger instance to be used.</param>
        public void Run(Logger log)
        {
            TcpListener srv = null;
            try
            {
                srv = new TcpListener(IPAddress.Loopback, portNumber);
                srv.Start();
                while (true)
                {
                    log.LogMessage("Listener - Waiting for connection requests.");
                    using (TcpClient socket = srv.AcceptTcpClient())
                    {
                        socket.LingerState = new LingerOption(true, 10);
                        log.LogMessage(String.Format("Listener - Connection established with {0}.",
                            socket.Client.RemoteEndPoint));
                        // Instantiating protocol handler and associate it to the current TCP connection
                        Handler protocolHandler = new Handler(socket.GetStream(), log);
                        // Synchronously process requests made through de current TCP connection
                        protocolHandler.Run();
                    }                    
                }
            }
            finally
            {
                log.LogMessage("Listener - Ending.");
                srv.Stop();
            }
        }

    }

    class Program
    {
        
        /// <summary>
        ///	Application's starting point. Starts a tracking server that listens at the TCP port 
        ///	specified as a command line argument.
        /// </summary>
        public static void Main(string[] args)
        {
			String execName = AppDomain.CurrentDomain.FriendlyName.Split('.')[0];
            // Checking command line arguments
            if (args.Length > 1)
            {
                Console.WriteLine("Usage: {0} [<TCPPortNumber>]", execName);
                Environment.Exit(1);
            }

            ushort port = 8080;
			if (args.Length == 1) {
            	if (!ushort.TryParse(args[0], out port))
            	{
                	Console.WriteLine("Usage: {0} [<TCPPortNumber>]", execName);
                	return;
            	}
			}
			Console.WriteLine("--server starts listen on port {0}", port);

            // Start servicing
            Logger log = new Logger();
            log.Start();
            try
            {
                new Listener(port).Run(log);
            }
            finally
            {
                log.Stop();
            }
        }
    }
}
