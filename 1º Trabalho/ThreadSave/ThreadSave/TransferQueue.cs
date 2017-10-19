using System.Collections.Generic;
using System.Threading;

namespace ThreadSave {
    public class TransferQueue<T>  {
        public class Thread {
            public bool ready;
            public object thread;

            public Thread(bool ready, object thread)
            {
                this.ready = ready;
                this.thread = thread;
            }
        }

        private readonly object mon = new object();
        private readonly LinkedList<Thread> readers = new LinkedList<Thread>(); // readers só são inseridos na lista se não houver writers
        private readonly LinkedList<T> writers = new LinkedList<T>();
               
        public void Put(T msg) {
            lock (mon) {
                writers.AddLast(msg);
            }
        }

        /**
         * throws ThreadInterruptedException
         */
        public bool Transfer(T msg, int timeout) {
            lock(mon) {
                if (TimeOut.NotTime(timeout))
                    return false;
                LinkedListNode<T> current = writers.AddLast(msg);

                int time = TimeOut.EndTime(timeout);
                int remaining = TimeOut.Remaining(time);
                if(readers.Count != 0) {
                    SyncUtils.Pulse(mon, readers.First.Value.thread);
                }

                while (true) {
                    try {
                        SyncUtils.Wait(mon, current, remaining);
                    }
                    catch (ThreadInterruptedException) {
                        writers.Remove(msg);
                        throw;
                    }

                    remaining = TimeOut.Remaining(time);
                    if (TimeOut.InvalidTime(remaining)) {
                        return !writers.Remove(msg);
                        
                    }
                }
            }
        }

        /**
         * throws ThreadInterruptedException
         */
        public bool Take(int timeout, out T rmsg) {
            lock (mon) {
                if (TimeOut.NotTime(timeout)) {
                    rmsg = default(T);
                    return false;
                }

                int time = TimeOut.EndTime(timeout);
                int remaining = TimeOut.Remaining(time);

                while (writers.Count == 0) {
                    Thread current = new Thread(true, this);
                    readers.AddLast(current); // adicionar leitor actual à lista de leitores
                    try {
                        SyncUtils.Wait(mon, current.thread, remaining);
                    }
                    catch (ThreadInterruptedException) {
                        readers.Remove(current); //remover da lista de leitores o leitor actual
                        if(readers.Count != 0)
                            SyncUtils.Pulse(mon, readers.First.Value.thread);
                        throw;
                    }

                    remaining = TimeOut.Remaining(time);
                    if (TimeOut.InvalidTime(remaining)) {
                        rmsg = default(T);
                        return false;
                    }
                }

                if (TimeOut.InvalidTime(remaining)) {
                    rmsg = default(T);
                    return false;
                }
                rmsg = writers.First.Value;
                return true;
            }
        } 
    }
}