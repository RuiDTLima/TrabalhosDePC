using System;

namespace ServerAPM {
    public class Server {
        public static void Main(string[] args) {
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

            Console.WriteLine("Waiting For shutdown to be called.");
            while (!listener.isShutdown()) ;
            log.Shutdown();
            while (!log.isLogFinished()) ;
        }
    }
}