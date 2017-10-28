using System.Collections.Generic;
using System.Threading;

namespace ThreadSave {
    public class TransferQueue<T>  {
        private readonly object mon = new object();
        /*  As listas de leitores e escritores segue a lógica FIFO  */
        private readonly LinkedList<CustomThread> readers = new LinkedList<CustomThread>(); // readers só são inseridos na lista se não houver writers
        private readonly LinkedList<T> writers = new LinkedList<T>();
               
        /**
         *  Insere na lista de escritores a mensagem e acorda o primeiro leitor da lista de leitores 
         */
        public void Put(T msg) {
            lock (mon) {
                writers.AddLast(msg);
                if(readers.Count != 0) {
                    readers.First.Value.ready = true;
                    SyncUtils.Pulse(mon, readers.First.Value.thread);
                }
            }
        }

        /**
         * É adicionado um elemento ao fim da lista de escritores. Caso já exista um leitor à espera de uma escrita o primeiro da fila é acordado. Caso 
         * contrário o escritor é colocado à espera que uma thread leitora vá buscar a sua informação, ou até ser ultrapassado o timeout, situação na qual
         * a mensagem é retirada da lista de escritores.
         * throws ThreadInterruptedException
         */
        public bool Transfer(T msg, int timeout) {
            lock(mon) {
                LinkedListNode<T> current = writers.AddLast(msg);

                if(readers.Count != 0) {
                    readers.First.Value.ready = true;
                    SyncUtils.Pulse(mon, readers.First.Value.thread);
                } else if (TimeOut.NoWait(timeout))
                {
                    writers.RemoveLast();
                    return false;
                }

                int time = TimeOut.Start(timeout);
                int remaining = TimeOut.Remaining(time);
                while (true) {
                    try {
                        SyncUtils.Wait(mon, current, remaining);
                    }
                    catch (ThreadInterruptedException) {
                        if (!writers.Contains(msg)) {
                            Thread.CurrentThread.Interrupt();
                            return true;
                        }
                        writers.Remove(msg);
                        throw;
                    }

                    if(!writers.Contains(msg))
                        return true;

                    remaining = TimeOut.Remaining(time);
                    if (TimeOut.IsTimeout(remaining)) {
                        return !writers.Remove(msg);
                    }
                }
            }
        }

        /**
         * É verificado se existe já alguma informação que foi escrita, situação na qual é retirada a mensagem da lista, retornada na parametro de saída 
         * rmsg e é acordado o escritor e retornado true. Caso contrário é verificado se o timeout passado é inválido, em caso afirmativo é retornado falso
         * Se não o leitor é adicionado ao fim da lista de leitores e espera que chegue uam mensagem que possa ler, antes do seu tempo de vida terminar.
         * throws ThreadInterruptedException
         */
        public bool Take(int timeout, out T rmsg) {
            lock (mon) {
                if(writers.Count != 0) {
                    rmsg = writers.First.Value;
                    SyncUtils.Pulse(mon, writers.First.Value);
                    writers.RemoveFirst();
                    return true;
                }

                if (TimeOut.NoWait(timeout)) {
                    rmsg = default(T);
                    return false;
                }

                int time = TimeOut.Start(timeout);
                int remaining = TimeOut.Remaining(time);

                CustomThread current = new CustomThread(false, this);
                readers.AddLast(current); // adicionar leitor actual à lista de leitores

                while (true) {
                    try {
                        SyncUtils.Wait(mon, current.thread, remaining);
                    }
                    catch (ThreadInterruptedException) {
                        readers.Remove(current); //remover da lista de leitores o leitor actual
                        if (writers.Count != 0 && current.ready) {
                            rmsg = writers.First.Value;
                            SyncUtils.Pulse(mon, writers.First.Value);
                            writers.RemoveFirst();
                            Thread.CurrentThread.Interrupt();
                            return true;
                        }

                        if (readers.Count != 0) {
                            readers.First.Value.ready = true;
                            SyncUtils.Pulse(mon, readers.First.Value.thread);
                        }
                        throw;
                    }

                    if (writers.Count != 0 && current.ready) {
                        rmsg = writers.First.Value;
                        SyncUtils.Pulse(mon, writers.First.Value);
                        writers.RemoveFirst();
                        return true;
                    }

                    remaining = TimeOut.Remaining(time);
                    if (TimeOut.IsTimeout(remaining)) {
                        readers.Remove(current);
                        rmsg = default(T);
                        return false;
                    }
                }
            }
        } 
        
        /**
         *  Classe auxiliar para representar a thread que seram os leitores 
         */
        public class CustomThread {
            public bool ready;
            public object thread;

            public CustomThread(bool ready, object thread) {
                this.ready = ready;
                this.thread = thread;
            }
        }
    }
}