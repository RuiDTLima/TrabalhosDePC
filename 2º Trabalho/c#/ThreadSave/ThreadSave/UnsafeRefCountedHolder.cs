using System;
using System.Threading;

namespace ThreadSave {
    public class UnsafeRefCountedHolder<T> where T : class  {
        /*private volatile T value;
        private volatile int refCount;

        public UnsafeRefCountedHolder(T v) {
             value = v;
             refCount = 1;
        }

        public void AddRef() {
            int auxRefCount;
            do
            {
                auxRefCount = refCount;
                if (refCount == 0)
                    throw new InvalidOperationException();
            } while(Interlocked.CompareExchange(ref refCount, auxRefCount + 1, auxRefCount) != auxRefCount); 
        }

        public void ReleaseRef() {
            int auxRefCount;
            do {
                auxRefCount = refCount;
                if (refCount == 0)
                    throw new InvalidOperationException();
                if (Interlocked.CompareExchange(ref refCount, auxRefCount - 1, auxRefCount) == 0) {
                    IDisposable disposable = value as IDisposable;
                    value = null;
                    if (disposable != null)
                        disposable.Dispose();
                    return;
                }
            } while (true);
        }

        public T Value {
            get {
                if(refCount == 0)
                    throw new InvalidOperationException();
                return value;
            }
        }*/

        private T value;
        private int refCount;
        public UnsafeRefCountedHolder(T v) {
            value = v;
            refCount = 1;
        }

        public void AddRef() {
            if (refCount == 0)
                throw new InvalidOperationException();
            refCount++;
        }

        public void ReleaseRef(){
            if (refCount == 0)
                throw new InvalidOperationException();
            if (--refCount == 0){
                IDisposable disposable = value as IDisposable;
                value = null;
                if (disposable != null)
                    disposable.Dispose();
            }
        }

        public T Value {
            get {
                if (refCount == 0)
                    throw new InvalidOperationException();
                return value;
            }
        }
    }
}