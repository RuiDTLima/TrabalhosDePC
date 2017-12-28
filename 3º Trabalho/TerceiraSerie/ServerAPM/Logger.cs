using System;
using System.Collections;
using System.IO;
using System.Threading;

namespace ServerAPM {
	// Logger single-threaded.
	public class Logger {
		private readonly TextWriter writer;
		private DateTime start_time;
		private int num_requests;
        private readonly Queue messageQueue = new Queue();
        private readonly object hasElements = new object();
        private bool isShutdown;

        public Logger() : this(Console.Out) {
        }

		public Logger(string logfile) : this(new StreamWriter(new FileStream(logfile, FileMode.Append, FileAccess.Write))) {
        }

		public Logger(TextWriter awriter){
		    num_requests = 0;
		    writer = awriter;
            isShutdown = false;
            Thread loggingThread = new Thread(new ThreadStart(Log));
            loggingThread.IsBackground = true;
            loggingThread.Start();
		}

		public void LogMessage(string msg) {
            lock (hasElements) {
                messageQueue.Enqueue(String.Format("{0}: {1}", DateTime.Now, msg));
                Monitor.Pulse(hasElements);
            }
        }

        public void Shutdown() {
            isShutdown = true;
            lock (hasElements) {
                Monitor.Pulse(hasElements);
            }
        }

        private void Start() {
            start_time = DateTime.Now;
            writer.WriteLine();
            writer.WriteLine(String.Format("::- LOG STARTED @ {0} -::", DateTime.Now));
            writer.WriteLine();
        }

        private void Stop() {
			long elapsed = DateTime.Now.Ticks - start_time.Ticks;
			writer.WriteLine();
            writer.WriteLine(String.Format("{0}: Running for {1} second(s)", DateTime.Now, elapsed / 10000000L));
            writer.WriteLine(String.Format("{0}: Number of request(s): {1}", DateTime.Now, num_requests));
			writer.WriteLine();
			writer.WriteLine(String.Format("::- LOG STOPPED @ {0} -::", DateTime.Now));
			writer.Close();
		}

        private void Log() {
            Start();
            while (true) {
                if (isShutdown)
                    break;
                lock (hasElements) {
                    Monitor.Wait(hasElements, 10000);
                }

                if (isShutdown)
                    break;

                Queue currentQueue = new Queue(messageQueue);
                messageQueue.Clear();

                foreach(string message in currentQueue) {
                    writer.WriteLine(message);
                }
            }
            Stop();
        }
	}
}