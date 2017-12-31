using System;

namespace ServerAPM {
    class Server {
        static void Main(string[] args) {
            String execName = AppDomain.CurrentDomain.FriendlyName.Split('.')[0];

            // Checking command line arguments
            if (args.Length != 0) {
                Console.WriteLine("Usage: {0} [<TCPPortNumber>]", execName);
                Environment.Exit(1);
            }

            // Start servicing
            Logger log = new Logger("Log.txt");

            Listener listener = new Listener(log);
            listener.Run();
            /*string command = "";
            while (!command.ToLower().Equals("exit")) {
                Console.WriteLine("Write exit to finish server");
                command = Console.ReadLine();
            }*/
            Console.WriteLine("Waiting For shutdown to be called.");
            while (!listener.getIsShutingDown()) ;
            log.Shutdown();
        }
    }
}