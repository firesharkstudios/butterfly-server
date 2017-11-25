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
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Nito.AsyncEx;
using NLog;

using Butterfly.Database.Event;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Database.Dynamic {
    public class DynamicSelectGroup : IDisposable {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly Func<DataEvent, bool> listenerDataEventFilter;
        protected readonly Action<DataEventTransaction> listener;
        protected readonly Func<DataEventTransaction, Task> asyncListener;

        protected readonly List<DynamicSelect> dynamicSelect = new List<DynamicSelect>();
        protected readonly ConcurrentQueue<DataEventTransaction> incomingDataEventTransactions = new ConcurrentQueue<DataEventTransaction>();

        protected readonly CancellationTokenSource runCancellationTokenSource = new CancellationTokenSource();
        protected readonly AsyncMonitor monitor = new AsyncMonitor();

        protected readonly List<IDisposable> disposables = new List<IDisposable>();

        public DynamicSelectGroup(Database database, Action<DataEventTransaction> listener, Func<DataEvent, bool> listenerDataEventFilter = null) {
            this.Database = database;
            this.listener = listener;
            this.listenerDataEventFilter = listenerDataEventFilter;
        }

        public DynamicSelectGroup(Database database, Func<DataEventTransaction, Task> asyncListener, Func<DataEvent, bool> listenerDataEventFilter = null) {
            this.Database = database;
            this.asyncListener = asyncListener;
            this.listenerDataEventFilter = listenerDataEventFilter;
        }

        public Database Database {
            get;
            protected set;
        }

        /// <summary>
        /// Creates an instance of a DynamicQuery.
        /// Must call StartAync() to send initial DataChangeTransaction and listen for new DataChangeTransactions for this DynamicQuery.
        /// </summary>
        public DynamicSelect CreateDynamicSelect(string sql, dynamic values = null, string name = null, string[] keyFieldNames = null) {
            DynamicSelect dynamicQuery = new DynamicSelect(this, sql, values, name, keyFieldNames);
            this.dynamicSelect.Add(dynamicQuery);
            return dynamicQuery;
        }

        /// <summary>
        /// Sends the initial DataChangeTransactions to the registered listener.
        /// Listens for DataChangeTransactions and send appropriately filtered DataChangeTransactions to the registered listener.
        /// Stops listening when DynamicQuerySet is disposed.
        /// </summary>
        /// <returns></returns>
        public async Task<DynamicSelectGroup> StartAsync() {
            logger.Debug("StartAsync");
            if (this.runCancellationTokenSource.IsCancellationRequested) throw new Exception("Cannot restart a stopped DynamicQuerySet");

            DataEvent[] dataEvents = await this.RequeryDynamicSelectsIfDirtyAsync(force: true);
            await this.SendToListenerAsync(new DataEventTransaction(DateTime.Now, dataEvents));

            this.disposables.Add(this.Database.AddUncommittedTransactionListener(this.ProcessUncommittedDataEventTransactionAsync));
            this.disposables.Add(this.Database.AddCommittedTransactionListener(this.ProcessCommittedDataEventTransactionAsync));
            Task backgroundTask = Task.Run(this.RunAsync);

            return this;
        }

        protected async Task ProcessUncommittedDataEventTransactionAsync(DataEventTransaction dataEventTransaction) {
            await this.LookupImpactedRecords(TransactionState.Uncommitted, dataEventTransaction);
        }

        protected async Task ProcessCommittedDataEventTransactionAsync(DataEventTransaction dataEventTransaction) {
            await this.LookupImpactedRecords(TransactionState.Committed, dataEventTransaction);
            this.incomingDataEventTransactions.Enqueue(dataEventTransaction);
            this.monitor.PulseAll();
        }

        protected async Task LookupImpactedRecords(TransactionState transactionState, DataEventTransaction dataEventTransaction) {
            // Find all impacted records and store them with the data event transaction
            foreach (var dynamicSelect in this.dynamicSelect) {
                foreach (var dataEvent in dataEventTransaction.dataEvents) {
                    if (dataEvent is ChangeDataEvent dataChange && HasImpactedRecords(transactionState, dataChange)) {
                        Dict[] impactedRecords = await dynamicSelect.GetImpactedRecordsAsync(dataChange);
                        string storageKey = GetImpactedRecordsStorageKey(dynamicSelect, dataEvent, transactionState);
                        dataEventTransaction.Store(storageKey, impactedRecords);
                    }
                }
            }
        }

        protected string GetImpactedRecordsStorageKey(DynamicSelect dynamicSelect, DataEvent dataEvent, TransactionState transactionState) {
            return $"{dynamicSelect.Id} {dataEvent.Id} {transactionState}";
        }

        protected bool HasImpactedRecords(TransactionState transactionState, DataEvent dataEvent) {
            switch (transactionState) {
                case TransactionState.Uncommitted:
                    return dataEvent.dataEventType == DataEventType.Update || dataEvent.dataEventType == DataEventType.Delete;
                case TransactionState.Committed:
                    return dataEvent.dataEventType == DataEventType.Update || dataEvent.dataEventType == DataEventType.Insert;
            }
            return false;
        }

        /// <summary>
        /// Processes queued data change transactions (runs on a background thread)
        /// </summary>
        /// <returns></returns>
        protected async Task RunAsync() {
            while (!this.runCancellationTokenSource.IsCancellationRequested) {
                if (this.incomingDataEventTransactions.TryDequeue(out DataEventTransaction dataEventTransaction)) {
                    logger.Debug($"RunAsync():dataEventTransaction={dataEventTransaction}");
                    List<DataEvent> newDataEvents = new List<DataEvent>();
                    foreach (var dataEvent in dataEventTransaction.dataEvents) {
                        logger.Debug($"RunAsync():dataEventTransaction.dataEvents.Length={dataEventTransaction.dataEvents.Length}");
                        foreach (var dynamicSelect in this.dynamicSelect) {
                            if (!dynamicSelect.HasDirtyParams) {
                                // Fetch the preCommitImpactedRecords
                                Dict[] preCommitImpactedRecords = null;
                                if (HasImpactedRecords(TransactionState.Uncommitted, dataEvent)) {
                                    string storageKey = GetImpactedRecordsStorageKey(dynamicSelect, dataEvent, TransactionState.Uncommitted);
                                    preCommitImpactedRecords = (Dict[])dataEventTransaction.Fetch(storageKey);
                                }

                                // Fetch the postCommitImpactedRecords
                                Dict[] postCommitImpactedRecords = null;
                                if (HasImpactedRecords(TransactionState.Committed, dataEvent)) {
                                    string storageKey = GetImpactedRecordsStorageKey(dynamicSelect, dataEvent, TransactionState.Committed);
                                    postCommitImpactedRecords = (Dict[])dataEventTransaction.Fetch(storageKey);
                                }

                                // Determine the changes from each data event on each dynamic select
                                ICollection<ChangeDataEvent> newChangeDataEvents = dynamicSelect.ProcessDataChange(dataEvent, preCommitImpactedRecords, postCommitImpactedRecords);
                                if (newChangeDataEvents != null) {
                                    foreach (var newChangeDataEvent in newChangeDataEvents) {
                                        dynamicSelect.UpdateDynamicParams(newChangeDataEvent);
                                        newDataEvents.Add(newChangeDataEvent);
                                    }
                                }
                            }
                        }
                    }

                    DataEvent[] initialDataEvents = await this.RequeryDynamicSelectsIfDirtyAsync();
                    newDataEvents.AddRange(initialDataEvents);

                    if (newDataEvents.Count > 0) {
                        await this.SendToListenerAsync(new DataEventTransaction(dataEventTransaction.dateTime, newDataEvents.ToArray()));
                    }
                }
                else {
                    using (var monitorWait = await this.monitor.EnterAsync(this.runCancellationTokenSource.Token)) {
                        await this.monitor.WaitAsync();
                    }
                }
            }
        }

        protected async Task SendToListenerAsync(DataEventTransaction dataEventTransaction) {
            DataEventTransaction newDataEventTransaction;
            if (this.listenerDataEventFilter!=null) {
                var newDataEvents = dataEventTransaction.dataEvents.Where(this.listenerDataEventFilter).ToArray();
                newDataEventTransaction = new DataEventTransaction(dataEventTransaction.dateTime, newDataEvents);
            }
            else {
                newDataEventTransaction = dataEventTransaction;
            }

            if (this.listener != null) {
                this.listener(newDataEventTransaction);
            }
            if (this.asyncListener != null) {
                await asyncListener(newDataEventTransaction);
            }
        }

        /// <summary>
        /// Return the initial query results if any of the query parameters have changed or if passed force=true
        /// </summary>
        /// <param name="force"></param>
        /// <returns></returns>
        protected async Task<DataEvent[]> RequeryDynamicSelectsIfDirtyAsync(bool force = false) {
            List<DataEvent> dataEvents = new List<DataEvent>();
            foreach (var dynamicSelect in this.dynamicSelect) {
                if (force || dynamicSelect.HasDirtyParams) {
                    DataEvent[] initialDataEvents = await dynamicSelect.GetInitialDataEventsAsync();
                    dataEvents.AddRange(initialDataEvents);
                    dynamicSelect.ResetDirtyOnDynamicParams();
                    dynamicSelect.UpdateDynamicParams(initialDataEvents);
                }
            }
            return dataEvents.ToArray();
        }

        public void Dispose() {
            logger.Debug("Dispose()");
            foreach (var disposable in this.disposables) {
                disposable.Dispose();
            }
            this.runCancellationTokenSource.Cancel();
        }

    }
}
