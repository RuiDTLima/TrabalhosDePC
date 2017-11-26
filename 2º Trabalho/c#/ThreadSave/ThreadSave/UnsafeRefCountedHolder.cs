using System;
using System.Threading;

namespace ThreadSave {
    public class UnsafeRefCountedHolder<T> where T : class  {
        /**
         *  As variáveis são volatile de modo a que qualquer alteração ao seu estado seja visivel por todas
         *  as threads 
         */
        private volatile T value;
        private volatile int refCount;

        public UnsafeRefCountedHolder(T v) {
             value = v;
             refCount = 1;
        }

        /**
         *  Fica num ciclo infinito a tentar adicionar uma nova referência, caso o número de referências não se
         *  mantenha o mesmo, desde o ínicio do ciclo. Isto é feito para poder user este método por múltiplas
         *  threads, e assim garantir sem o uso de exclusão mútua que não se perdem actualizações. 
         */
        public void AddRef() {
            int auxRefCount;
            do {
                auxRefCount = refCount;
                if (auxRefCount == 0)
                    throw new InvalidOperationException();
            } while(Interlocked.CompareExchange(ref refCount, auxRefCount + 1, auxRefCount) != auxRefCount); 
        }

        /**
         *  Tenta retirar uma referência do contador. É feito num ciclo infinito, para garantir que não há
         *  perda de actualizações. Caso após a remoção o número de referências seja 0 é eliminado o valor
         *  através do Dispose.
         */
        public void ReleaseRef() {
            int auxRefCount;
            do {
                auxRefCount = refCount;
                if (auxRefCount == 0)
                    throw new InvalidOperationException();
                if (Interlocked.CompareExchange(ref refCount, auxRefCount - 1, auxRefCount) == 1) {
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
        }

        /*
         *  Versão do enunciado
         */
        /*private T value;
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
        }*/
    }
}