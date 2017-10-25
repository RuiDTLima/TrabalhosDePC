using System;
using System.Collections.Generic;
using System.Threading;

namespace ThreadSave {
    public class Pairing<T, U> {
        public class Thread<A> {
            public A value;
            public object thread;
            public bool ready;

            public Thread(A value, object thread, bool ready) {
                this.value = value;
                this.thread = thread;
                this.ready = ready;
            }
        }

        private LinkedList<Thread<T>> writeT = new LinkedList<Thread<T>>();
        private LinkedList<Thread<U>> writeU = new LinkedList<Thread<U>>();
        private LinkedList<T> unusableT = new LinkedList<T>();
        private LinkedList<U> unusableU = new LinkedList<U>();
        private readonly object mon = new object();
        
        /**
         * throws ThreadInterruptedException, TimeoutException
         */
        public Tuple<T, U> Provide(T value, int timeout) {
            lock (mon) {
                if (TimeOut.NotTime(timeout)) {
                    unusableT.AddLast(value);
                    return null;
                }
                if (unusableT.Contains(value))
                    return null;

                int time = TimeOut.EndTime(timeout);
                int remaining = TimeOut.Remaining(time);
                Thread<T> current = new Thread<T>(value, this, true);
                writeT.AddLast(current);

                while (writeU.Count == 0 || !current.ready) {
                    try {
                        current.ready = false;
                        SyncUtils.Wait(mon, current.thread, remaining);
                    }catch (ThreadInterruptedException) {

                        writeT.Remove(current);
                        if (!unusableT.Contains(value))
                            unusableT.AddLast(value);
                        if (writeT.Count != 0) {
                            writeT.First.Value.ready = true;
                            SyncUtils.Pulse(mon, writeT.First.Value.thread);
                        }
                        throw;
                    }

                    remaining = TimeOut.Remaining(time);
                    if (TimeOut.InvalidTime(remaining)) {
                        writeT.Remove(current);
                        if (!unusableT.Contains(value))
                            unusableT.AddLast(value);
                        throw new TimeoutException();
                    }
                }

                if (TimeOut.InvalidTime(remaining)) {
                    writeT.Remove(current);
                    if (!unusableT.Contains(value))
                        unusableT.AddLast(value);
                    throw new TimeoutException();
                }

                writeU.First.Value.ready = true;
                SyncUtils.Pulse(mon, writeU.First.Value.thread);
                Tuple<T,U> tuple = new Tuple<T, U>(value, writeU.First.Value.value);
                U temp = writeU.First.Value.value;
                unusableT.AddLast(value);   // marca o valor do tuplo T como invalido para futuras utilizações
                writeU.RemoveFirst();   // Remove o valor que vai ser escrito proveniente do outro povider
                return tuple;
            }
        }

        /**
         * throws ThreadInterruptedException, TimeoutException
         */
        public Tuple<T, U> Provide(U value, int timeout) {
            lock (mon) {
                if (TimeOut.NotTime(timeout)) {
                    unusableU.AddLast(value);
                    return null;
                }
                if (unusableU.Contains(value))
                    return null;

                int time = TimeOut.EndTime(timeout);
                int remaining = TimeOut.Remaining(time);
                Thread<U> current = new Thread<U>(value, this, false);
                writeU.AddLast(current);

                while (writeT.Count == 0 && !current.ready){
                    try {
                        SyncUtils.Wait(mon, current.thread, remaining);
                    }catch (ThreadInterruptedException) {

                        writeU.Remove(current);
                        if (!unusableU.Contains(value))
                            unusableU.AddLast(value);
                        if (writeU.Count != 0){
                            writeU.First.Value.ready = true;
                            SyncUtils.Pulse(mon, writeU.First.Value.thread);
                        }
                        throw;
                    }

                    remaining = TimeOut.Remaining(time);
                    if (TimeOut.InvalidTime(remaining)) {
                        writeU.Remove(current);
                        if (!unusableU.Contains(value))
                            unusableU.AddLast(value);
                        throw new TimeoutException();
                    }
                }

                if (TimeOut.InvalidTime(remaining)){
                    writeU.Remove(current);
                    if (!unusableU.Contains(value))
                        unusableU.AddLast(value);
                    throw new TimeoutException();
                }

                writeT.First.Value.ready = true;
                SyncUtils.Pulse(mon, writeT.First.Value.thread);
                Tuple<T, U> tuplo = new Tuple<T, U>(writeT.First.Value.value, value);
                T temp = writeT.First.Value.value;
                unusableU.AddLast(value); // marca o valor do tuplo U como invalido para futuras utilizações
                writeT.RemoveFirst();   // Remove o valor que vai ser escrito proveniente do outro povider
                return tuplo;
            }
        }
    }
}