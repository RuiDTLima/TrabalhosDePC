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
        private bool retry = false; // indica que o calculo de um novo valor não teve sucesso
        private readonly ExpirableObject<T> busyObject;     // used to represent the fact that the value is being calculated
        private volatile ExpirableObject<T> valueObject;    // used to store previous calculated value

        public ExpirableLazy(Func<T> provider, TimeSpan timeToLive) {
            this.provider = provider;
            this.timeToLive = timeToLive;
            busyObject = new ExpirableObject<T>(null, DateTime.UtcNow);
            valueObject = null;
        }

        /*
         *  Para evitar o uso da zona de exclusão mútua, a qual prejudica a eficiência do programa, é 
         *  chamado no inicio da execução do get o método TryAcquire, este método é chamado novamente
         *  logo após a acquisição da zona de exclusão mútua, uma vez desde o momento da última chamada ao 
         *  método e o momento da acquisição do lock pode ter passado muito tempo, o que poderá ter 
         *  modificado as condição de continuação de execução do código. Caso esta chamada ainda não tenha
         *  sucesso é thread é bloqueada à espera que um novo valor seja calculado. Depois de acordada, 
         *  chama novamente o método TryAcquire.
         */
        public T Value {
            get {
                if (TryAcquire())
                    return valueObject.value;

                lock (mon) {
                    if (TryAcquire())
                        return valueObject.value;

                    while (true) {
                        try {
                            Monitor.Wait(mon);
                        }
                        catch (ThreadInterruptedException) {
                            if (retry)
                                Monitor.Pulse(mon);
                            throw;
                        }

                        if (TryAcquire())
                            return valueObject.value;
                    }
                }
            }
        }

        /**
         *  Caso já exista um valor e este seja válido é retornado true. Caso esteja a ser calculado um novo
         *  valor é retornado false. Caso não exista nenhum valor, ou este esteja inválido, então é 
         *  calculado um novo valor e no fim do calculo é retornado true. Sabe-se que está a ser calculado
         *  um novo valor, o provider foi executado, quando o objecto que guarda o valor corresponder ao 
         *  objecto busy que indica que um cálculo está em curso. Caso a chamada ao provider lance uma
         *  excepção é necessário acordar um thread que esteja à espera do valor para essa poder calcular
         *  um novo valor, para isso adquire-se a zona de exclusão mútua, é colocado o booleano retry a 
         *  true e é acordada uma thread. Caso após o retorno da chamada ao provider, o objecto 
         *  valueObject ainda esteja igualado ao objecto busy, sabe-se que aquele retorno indica o novo 
         *  valor e por isso é necessário criar uma nova instância de ExpirableObject com o novo valor
         *  e o tempo de vida actualizado, isto é feito dentro de uma zona de exclusão mútua, pois todas
         *  as threads que estejam à espera de um novo elemento serão acordadas.
         */
        private bool TryAcquire() {
            while (true) {
                if (valueObject == busyObject)
                    return false;

                if (valueObject != null && DateTime.UtcNow.CompareTo(valueObject.validUntil) <= 0)
                    return true;

                if (valueObject == null || DateTime.UtcNow.CompareTo(valueObject.validUntil) > 0) {
                    valueObject = busyObject;
                    T _value;
                    try {
                        _value = provider();
                        retry = false;
                    }
                    catch (Exception) {
                        if (valueObject == busyObject)
                            valueObject = null;
                        Monitor.Enter(mon);
                        retry = true;
                        Monitor.Pulse(mon);
                        Monitor.Exit(mon);
                        throw;
                    }
                    if (valueObject == busyObject) {
                        Monitor.Enter(mon);
                        valueObject = new ExpirableObject<T>(_value, DateTime.UtcNow.Add(timeToLive));
                        Monitor.PulseAll(mon);
                        Monitor.Exit(mon);
                        return true;
                    }
                }
            }
        }
    }
}