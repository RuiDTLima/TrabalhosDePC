using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreadSave {
    public class TransferQueue<T>  {
        private readonly object mon = new object();
        private readonly LinkedList<bool> readers = new LinkedList<bool>();
        private readonly LinkedList<bool> writers = new LinkedList<bool>();
               
        public void Put(T msg) {
            lock (mon) {
                writers.AddLast(true);
            }
        }

        /**
         * throws ThreadInterruptedException
         */
       /* public bool Transfer(T msg, int timeout) {
            lock(mon) {
                if (timeout == 0)
                    return false;
                
            }
        }*/

        /**
         * throws ThreadInterruptedException
         */
        /*public bool Take(int timeout, out T rmsg) {

        } */
    }
}
