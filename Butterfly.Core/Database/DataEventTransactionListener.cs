/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Threading.Tasks;

using Butterfly.Core.Database.Event;

namespace Butterfly.Core.Database {
    /// <summary>
    /// Internal class used to store references to data event transaction listeners
    /// </summary>
    public class DataEventTransactionListener {
        public readonly Action<DataEventTransaction> listener;
        public readonly Func<DataEventTransaction, Task> listenerAsync;

        public DataEventTransactionListener(Action<DataEventTransaction> listener) {
            this.listener = listener;
            this.listenerAsync = null;
        }

        public DataEventTransactionListener(Func<DataEventTransaction, Task> listener) {
            this.listener = null;
            this.listenerAsync = listener;
        }
    }
}
