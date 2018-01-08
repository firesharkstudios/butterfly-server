using System;
using System.Collections.Generic;

namespace Butterfly.Util {
    public class DictionaryItemDisposable<T, U> : IDisposable {

        protected readonly Dictionary<T, U> dictionary;
        protected readonly T key;

        public DictionaryItemDisposable(Dictionary<T, U> list, T key, U item) {
            this.dictionary = list;
            this.key = key;
            this.dictionary.Add(key, item);
        }

        public void Dispose() {
            this.dictionary.Remove(this.key);
        }
    }
}
