/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core.Database.Event {
    public enum TransactionState {
        Uncommitted,
        Committed,
    }

    /// <summary>
    /// Represents a series of <see cref="DataEvent"/> instances resulting either
    /// from an initial query or from committing an <see cref="IDatabase"/> transaction
    /// </summary>
    public class DataEventTransaction {
        public readonly DateTime dateTime;
        public readonly DataEvent[] dataEvents;

        protected readonly Dictionary<string, object> storage = new Dictionary<string, object>();

        public DataEventTransaction(DateTime dateTime, DataEvent[] dataEvents) {
            this.dateTime = dateTime;
            this.dataEvents = dataEvents;
        }

        public void Store(string key, object obj) => this.storage[key] = obj;

        public object Fetch(string key) {
            if (this.storage.TryGetValue(key, out object obj)) return obj;
            else return null;
        }

        public override string ToString() {
            return $"{{dateTime={dateTime},dataEvents={string.Join(";", (object[])dataEvents)}}}";
        }

        public static DataEventTransaction FilterDataEvents(DataEventTransaction dataEventTransaction, Func<DataEvent, bool> dataEventFilter) {
            return new DataEventTransaction(dataEventTransaction.dateTime, dataEventTransaction.dataEvents.Where(dataEventFilter).ToArray());
        }
    }
}
