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

            Task task = new Listener(log).Run(cancellationToken);

            string command = "";
            while (!command.ToLower().Equals("exit")) {
                Console.WriteLine("Write exit to finish server");
                command = Console.ReadLine();
            }
            log.Shutdown();
        }
    }
}