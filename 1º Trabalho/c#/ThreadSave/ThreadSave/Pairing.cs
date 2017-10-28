using System;
using System.Collections.Generic;
using System.Threading;

namespace ThreadSave {
    public class Pairing<T, U> {
        private LinkedList<Thread<T>> writeT = new LinkedList<Thread<T>>();
        private LinkedList<Thread<U>> writeU = new LinkedList<Thread<U>>();
        private readonly object mon = new object();
        
        /**
         * Adiciona um elemento do tipo T à sua fila e caso já exista algum elemento do tipo U à espera, este é acordado e é retornado um tuplo com o valor
         * recebido neste método e no outro provide. Caso contrário é avaliado se o tempo de vida da thread é válido ou não. Em caso afirmativo a thread é
         * bloqueada à espera que seja escrito um valor do tipo U dentro do seu tempo de vida
         * throws ThreadInterruptedException, TimeoutException
         */
        public Tuple<T, U> Provide(T value, int timeout) {
            lock (mon) {
                Tuple<T, U> tuple;
                Thread<T> current = new Thread<T>(value, this, false);
                writeT.AddLast(current);

                if (writeU.Count != 0) {
                    writeU.First.Value.ready = true;
                    SyncUtils.Pulse(mon, writeU.First.Value.thread);
                    tuple = new Tuple<T, U>(value, writeU.First.Value.value);
                    writeU.RemoveFirst();
                    return tuple;
                }

                if (TimeOut.NoWait(timeout)) {
                    writeT.RemoveLast();
                    return null;
                }

                int time = TimeOut.Start(timeout);
                int remaining = TimeOut.Remaining(time);

                while (true) {
                    try {
                        //current.ready = false;
                        SyncUtils.Wait(mon, current.thread, remaining);
                    }catch (ThreadInterruptedException) {
                        if (current.ready) {
                            writeU.First.Value.ready = true;
                            SyncUtils.Pulse(mon, writeU.First.Value.thread);
                            tuple = new Tuple<T, U>(value, writeU.First.Value.value);
                            writeU.RemoveFirst();
                            Thread.CurrentThread.Interrupt();
                            return tuple;
                        }

                        writeT.Remove(current);
                        if (writeT.Count != 0) {
                            writeT.First.Value.ready = true;
                            SyncUtils.Pulse(mon, writeT.First.Value.thread);
                        }
                        throw;
                    }
                    if (current.ready) {
                        writeU.First.Value.ready = true;
                        SyncUtils.Pulse(mon, writeU.First.Value.thread);
                        tuple = new Tuple<T, U>(value, writeU.First.Value.value);
                        writeU.RemoveFirst();
                        return tuple;
                    }

                    remaining = TimeOut.Remaining(time);
                    if (TimeOut.IsTimeout(remaining)) {
                        writeT.Remove(current);
                        throw new TimeoutException();
                    }
                }
            }
        }

        /**
         * Adiciona um elemento do tipo U à sua fila e caso já exista algum elemento do tipo T à espera, este é acordado e é retornado um tuplo com o valor
         * recebido neste método e no outro provide. Caso contrário é avaliado se o tempo de vida da thread é válido ou não. Em caso afirmativo a thread é
         * bloqueada à espera que seja escrito um valor do tipo T dentro do seu tempo de vida
         * throws ThreadInterruptedException, TimeoutException
         */
        public Tuple<T, U> Provide(U value, int timeout) {
            lock (mon) {
                Tuple<T, U> tuple;
                Thread<U> current = new Thread<U>(value, this, false);
                writeU.AddLast(current);

                if (writeT.Count != 0) {
                    writeT.First.Value.ready = true;
                    SyncUtils.Pulse(mon, writeT.First.Value.thread);
                    tuple = new Tuple<T, U>(writeT.First.Value.value, value);
                    writeT.RemoveFirst();
                    return tuple;
                }

                if (TimeOut.NoWait(timeout)) {
                    writeU.RemoveLast();
                    return null;
                }

                int time = TimeOut.Start(timeout);
                int remaining = TimeOut.Remaining(time);

                while (true){
                    try {
                        current.ready = false;
                        SyncUtils.Wait(mon, current.thread, remaining);
                    }catch (ThreadInterruptedException) {

                        if (current.ready) {
                            writeT.First.Value.ready = true;
                            SyncUtils.Pulse(mon, writeT.First.Value.thread);
                            tuple = new Tuple<T, U>(writeT.First.Value.value, value);
                            writeU.RemoveFirst();

                            Thread.CurrentThread.Interrupt();
                            return tuple;
                        }

                        writeU.Remove(current);
                        if (writeU.Count != 0){
                            writeU.First.Value.ready = true;
                            SyncUtils.Pulse(mon, writeU.First.Value.thread);
                        }
                        throw;
                    }

                    if (current.ready) {
                        writeT.First.Value.ready = true;
                        SyncUtils.Pulse(mon, writeT.First.Value.thread);
                        tuple = new Tuple<T, U>(writeT.First.Value.value, value);
                        writeU.RemoveFirst();
                        return tuple;
                    }

                    remaining = TimeOut.Remaining(time);
                    if (TimeOut.IsTimeout(remaining)) {
                        writeU.Remove(current);
                        throw new TimeoutException();
                    }
                }
            }
        }

        /**
         *  Classe auxiliar para representar as threads que transmitem informação pelo provide 
         */
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
    }
}