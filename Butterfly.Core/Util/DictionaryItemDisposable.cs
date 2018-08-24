/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

 using System;
using System.Collections.Generic;

namespace Butterfly.Core.Util {
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
