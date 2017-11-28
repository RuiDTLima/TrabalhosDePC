using System;
using System.Threading;

namespace ThreadSave {
    public class ExpirableLazy<T> where T : class {
        /**
         *  Os objectos desta classe usados são representativos do estado da execução. 
         */
        private class ExpirableObject<T> where T : class {
            public readonly T value;
            public readonly DateTime validUntil;

            public ExpirableObject(T value, DateTime validUntil) {
                this.value = value;
                this.validUntil = validUntil;
            }
        }

        private readonly Func<T> provider;  // A função que indica como calcular um novo valor
        private readonly TimeSpan timeToLive;   // O tempo durante o qual o valor é válido
        private readonly object mon = new object();
        private readonly ExpirableObject<T> busyObject;     // used to represent the fact that the value is being calculated
        private volatile ExpirableObject<T> valueObject;    // used to store previous calculated value
        private volatile int waiters;       // indica quantas threads estao bloqueadas à espera de um novo valor

        public ExpirableLazy(Func<T> provider, TimeSpan timeToLive) {
            this.provider = provider;
            this.timeToLive = timeToLive;
            busyObject = new ExpirableObject<T>(null, DateTime.UtcNow);
            valueObject = null;
            waiters = 0;
        }

        /*
         *  Para evitar o uso da zona de exclusão mútua, a qual prejudica a eficiência do programa, é 
         *  chamado no inicio da execução do get o método TryAcquire, este método é chamado novamente
         *  logo após a acquisição da zona de exclusão mútua, uma vez que desde o momento da última chamada 
         *  ao método e o momento da acquisição do lock pode ter passado muito tempo, o que poderá ter 
         *  modificado as condição de continuação de execução do código. Caso esta chamada ainda não tenha
         *  sucesso a thread é bloqueada à espera que um novo valor esteja pronto a ser calculado. Quando o
         *  TryAcquire retorne true e o valueObject seja igual ao busy significa que deve ser calculado o 
         *  novo valor, e após o calculo do novo valor, o mesmo é retornado
         */
        public T Value {
            get {
                ExpirableObject<T> returnValue;
                if (TryAcquire(out returnValue))
                    return returnValue.value;
                if (returnValue != busyObject) {
                    lock (mon) {
                        waiters++;
                        do {
                            if (TryAcquire(out returnValue)) {
                                waiters--;
                                if (returnValue == busyObject)
                                    break;
                                else
                                    return returnValue.value;
                            }
                            try {
                                Monitor.Wait(mon);
                            }
                            catch (ThreadInterruptedException) {
                                waiters--;
                                if (waiters > 0 && (valueObject == null || (valueObject != busyObject && DateTime.UtcNow.CompareTo(valueObject.validUntil) > 0)))
                                    Monitor.Pulse(mon);
                                throw;
                            }
                        } while (true);
                    }
                }
                Calculating();
                return valueObject.value;
            }
        }

        /**
         *  Caso esteja a ser calculado um novo valor é retornado false.Caso já exista um valor e este 
         *  seja válido é retornado true. Caso nenhuma destas situações se verifique significa que deve
         *  ser calculado um novo valor, nesse caso é igualado o valueObject ao busy, para indicar que 
         *  vai ser calculado um novo valor e é returnado false. Isto acontece quando o valor corrente é
         *  igual ao valueObject indicando assim que não houve alteração do estado desde o inicio do 
         *  ciclo.
         */
        private bool TryAcquire(out ExpirableObject<T> returnValue) {
            while (true) {
                ExpirableObject<T> current = valueObject;
                returnValue = null;

                if (valueObject == busyObject)
                    return false;

                if (valueObject != null && DateTime.UtcNow.CompareTo(valueObject.validUntil) <= 0) {
                    returnValue = valueObject;
                    return true;
                }

                if (Interlocked.CompareExchange(ref valueObject, busyObject, current) == current) {
                    returnValue = busyObject;
                    return false;
                }
            }
        }

        /**
         *  Chama o provider para calcular o novo valor. Caso no fim da chamada ao provider o valueObject 
         *  ainda seja igual ao busy significa que foi nesta thread que ocorreu o cálculo do novo valor, e por 
         *  isso o valueObject será igualado a uma nova instancia de ExpirableObject, com o novo valor e com 
         *  o tempo de vida renovado, depois são acordadas todas as threads à espera de um novo valor.
         */
        private void Calculating() {
            if (valueObject == busyObject) {
                T _value;
                try {
                    _value = provider();
                }
                catch (Exception) {
                    if (valueObject == busyObject)
                        valueObject = null;
                    if (waiters > 0) {
                        Monitor.Enter(mon);
                        Monitor.Pulse(mon);
                        Monitor.Exit(mon);
                    }
                    throw;
                }
                Interlocked.CompareExchange(ref valueObject, new ExpirableObject<T>(_value, DateTime.UtcNow.Add(timeToLive)), busyObject);
                if (waiters > 0) {
                    Monitor.Enter(mon);
                    Monitor.PulseAll(mon);
                    Monitor.Exit(mon);
                }
            }
        }
    }
}