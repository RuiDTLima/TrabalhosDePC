using System;
using System.Threading;

namespace ThreadSave {
    public class ExpirableLazy<T> where T : class {
        T _value = null;
        private Func<T> provider;
        private TimeSpan timeToLive;
        private DateTime validUntil;
        private readonly object mon = new object();
        private bool calculating = false;
        private bool retry = false;

        public ExpirableLazy(Func<T> provider, TimeSpan timeToLive) {
            this.timeToLive = timeToLive;
            this.provider = provider;
            validUntil = DateTime.UtcNow;           
        }

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

                if (_value == null || !validateValue()) {
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

        private bool validateValue()
        {
            return DateTime.UtcNow.CompareTo(validUntil) <= 0;
        }
    }
}