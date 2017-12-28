using System.Collections.Generic;

namespace ServerTAP {
    class Store {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        private static readonly Store _instance = new Store();

        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static Store Instance {
            get {
                return _instance;
            }
        }

        /// <summary>
        /// The dictionary instance.
        /// </summary>
        private readonly Dictionary<string, string> _store;

        /// <summary>
        /// Initiates the store instance.
        /// </summary>
        private Store() {
            _store = new Dictionary<string, string>();
        }

        /// <summary>
        /// Sets the key value. 
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Set(string key, string value) {
            _store[key] = value;
        }

        /// <summary>
        /// Gets the key value. 
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public string Get(string key) {
            string value = null;
            _store.TryGetValue(key, out value);
            return value;
        }

        /// <summary>
        /// Gets all keys. 
        /// </summary>        
        public IEnumerable<string> Keys() {
            return _store.Keys;
        }
    }
}