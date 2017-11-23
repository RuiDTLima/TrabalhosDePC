using System;
using System.Threading;

namespace ThreadSave {
    public class ExpirableLazy1<T> where T : class {
        T _value = null;    // guarda o valor calculado pelo provider
        private Func<T> provider;
        private TimeSpan timeToLive;    // tempo que o valor calculado pelo provider é valido
        private DateTime validUntil;    // data e hora até à qual o valor calculado pelo provider pode ser usado
        private readonly object mon = new object();
        private bool calculating = false;
        private bool retry = false;

        public ExpirableLazy1(Func<T> provider, TimeSpan timeToLive) {
            this.timeToLive = timeToLive;
            this.provider = provider;
            validUntil = DateTime.UtcNow;           
        }

        /*
         *  Enquanto alguma thread esteja a calcular um novo valor com recurso ao provider, as outras que fizerem get ficam 
         *  bloqueadas à espera da terminação do cálculo. Caso o valor ainda não tenha sido calculo ou caso o valor já 
         *  não seja válido devido ao seu tempo de vida a thread que fez o pedido calcula um novo valor, fora da 
         *  exclusão mútua uma vez que este cálculo pode ser muito demorado. Tendo o valor sido calculado, todas as 
         *  threads que estavam à espera do fim do calculo do valor são acordadas, para poderem returnar o novo valor 
         *  acabado de calcular. Caso a chamada a provider lance excepção é acordada uma das threads bloqueadas à espera
         *  do fim do calculo para poder ela realizar a chamada ao provider
         */
        public T Value {
            get {       // throws InvalidOperationException, ThreadInterruptedException
                Monitor.Enter(mon);
                    
                while(calculating) {
                    try {
                        Monitor.Wait(mon);
                    }
                    catch (ThreadInterruptedException) {
                        if (retry)
                            Monitor.Pulse(mon);
                        Monitor.Exit(mon);
                        throw;
                    }
                }

                if (_value == default(T) || !validateValue()) {
                    calculating = true;
                    Monitor.Exit(mon);

                    try {
                        _value = provider();
                        Monitor.Enter(mon);
                        Monitor.PulseAll(mon);
                        calculating = false;
                        retry = false;
                        validUntil = DateTime.UtcNow.Add(timeToLive);
                    }
                    catch (Exception) {
                        Monitor.Enter(mon);
                        calculating = false;
                        retry = true;
                        Monitor.Pulse(mon);
                        Monitor.Exit(mon);
                        throw;
                    }
                }
                Monitor.Exit(mon);
                return _value;
            }
        }

        /*
         *  Valida se o valor calculado ainda é valido, através do tempo actual e da data e hora até à qual é valido 
         */
        private bool validateValue() {
            return DateTime.UtcNow.CompareTo(validUntil) <= 0;
        }
    }
}