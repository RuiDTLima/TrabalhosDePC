using System;

namespace Client {
    class Program {
        static void Main(String[] args) {
            String execName = AppDomain.CurrentDomain.FriendlyName.Split('.')[0];
            // Checking command line arguments
            if (args.Length > 1) {
                Console.WriteLine("Usage: {0} [<TCPPortNumber>]", execName);
                Environment.Exit(1);
            }

            string request = "";
            Handler handler = new Handler();
            Handler secondHandler = new Handler();
            while (true) {
                Console.WriteLine("\nClient connected to server use one of the following commands:\n\tSET <key> <value>\n\tGET <key>\n\tKEYS\n\tSHUTDOWN");
                request = Console.ReadLine();
                if (handler.Run(request) && secondHandler.Run(request))
                    break;
            }
        }
    }
}