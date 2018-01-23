using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServerTAP {
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

            CancellationTokenSource cancellationToken = new CancellationTokenSource();

            Listener listener = new Listener(log);
            Task task = listener.Run(cancellationToken);

            Console.WriteLine("Waiting For shutdown to be called.");
            task.Wait();

            log.Shutdown();
            while (!log.isLogFinished()) ;
        }
    }
}