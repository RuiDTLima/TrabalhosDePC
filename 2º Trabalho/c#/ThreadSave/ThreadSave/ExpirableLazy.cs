using System;
using System.Threading;

namespace ThreadSave {
    public class ExpirableLazy<T> where T : class
    {
        private class ExpirableObject<T> where T : class
        {
            public readonly T value;
            public readonly DateTime validUntil;

            public ExpirableObject(T value, DateTime validUntil)
            {
                this.value = value;
                this.validUntil = validUntil;
            }
        }

        private readonly Func<T> provider;
        private readonly TimeSpan timeToLive;
        private readonly ExpirableObject<T> busyObject;     // used to represent the fact that the value is being calculated
        private volatile ExpirableObject<T> valueObject;    // used to store previous calculated value

        public ExpirableLazy(Func<T> provider, TimeSpan timeToLive)
        {
            this.provider = provider;
            this.timeToLive = timeToLive;
            busyObject = new ExpirableObject<T>(null, DateTime.UtcNow);
            valueObject = null;
        }

        /*
         *  Optimizações, o valor já se encontra disponivel; o valor não se encontra disponivel e não existe outra thread a calculá-lo
         */
        /*public T Value {
            get {
                while (true) {
                    if (valueObject != null && valueObject != busyObject && DateTime.UtcNow.CompareTo(valueObject.validUntil) <= 0)
                        return valueObject.value;
                    if(valueObject != busyObject && (valueObject == null || DateTime.UtcNow.CompareTo(valueObject.validUntil) > 0)) {
                        valueObject = busyObject;
                        T _value;
                        try {
                            _value = provider();
                        }
                        catch (Exception) {
                            if (valueObject == busyObject)
                                valueObject = null;
                            throw;
                        }
                        if (valueObject == busyObject) {
                            valueObject = new ExpirableObject<T>(_value, DateTime.UtcNow.Add(timeToLive));
                            return valueObject.value;
                        }
                        else {
                            return valueObject.value;
                        }
                    }
                }
            }
        }*/

        private readonly object mon = new object();
        private bool retry = false;
        /*public T Value {
            get {
                while (true) {
                    if (valueObject != null && valueObject != busyObject && DateTime.UtcNow.CompareTo(valueObject.validUntil) <= 0)
                        return valueObject.value;

                    if (valueObject != busyObject && (valueObject == null || DateTime.UtcNow.CompareTo(valueObject.validUntil) > 0)) {
                        valueObject = busyObject;
                        T _value;
                        try {
                            _value = provider();
                            retry = true;
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
                            return valueObject.value;
                        }
                        else {
                            return valueObject.value;
                        }
                    }

                    Monitor.Enter(mon);
                    try {
                        Monitor.Wait(mon);
                    } catch (ThreadInterruptedException) {
                        if(retry)
                            Monitor.Pulse(mon);
                        Monitor.Exit(mon);
                        throw;
                    }
                    Monitor.Exit(mon);
                }
            }
        }
        */
        /*private bool TryAcquire()
        {
            if (valueObject != null && valueObject != busyObject && DateTime.UtcNow.CompareTo(valueObject.validUntil) <= 0)
                return true;

            if (valueObject != busyObject && (valueObject == null || DateTime.UtcNow.CompareTo(valueObject.validUntil) > 0))
            {
                valueObject = busyObject;
                T _value;
                try
                {
                    _value = provider();
                }
                catch (Exception)
                {
                    if (valueObject == busyObject)
                        valueObject = null;
                    Monitor.Enter(mon);
                    Monitor.Pulse(mon);
                    Monitor.Exit(mon);
                    throw;
                }
                if (valueObject == busyObject)
                {
                    Monitor.Enter(mon);
                    valueObject = new ExpirableObject<T>(_value, DateTime.UtcNow.Add(timeToLive));
                    Monitor.PulseAll(mon);
                    Monitor.Exit(mon);
                    return true;
                }
                else
                {
                    return true;
                }
            }
            return false;
        }
        */

        private bool TryAcquire() {
            while (true) {
                if (valueObject != null && valueObject != busyObject && DateTime.UtcNow.CompareTo(valueObject.validUntil) <= 0)
                    return true;

                if (valueObject == busyObject)
                    return false;

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

        public T Value {
            get{
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
    }
}