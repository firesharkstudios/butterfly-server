/*
 * Copyright 2017 Fireshark Studios, LLC
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Threading.Tasks;

using Butterfly.Database.Event;

namespace Butterfly.Database {
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
