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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NLog;

using Butterfly.Database.Event;
using Butterfly.Util;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Database.Dynamic {
    /// <summary>
    /// Represents a specific view (SELECT statement) that should be executed
    /// to return the initial data as a sequence of <see cref="DataEvent"/> instances
    /// and should publish <see cref="DataEvent"/> instances when any data in the
    /// view changes
    /// </summary>
    public class DynamicView {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly DynamicViewSet dynamicViewSet;
        protected readonly SelectStatement statement;
        protected readonly Dict statementParams;
        protected readonly string name;
        protected readonly string[] keyFieldNames;

        protected readonly List<ChildDynamicParam> childDynamicParams = new List<ChildDynamicParam>();

        public DynamicView(DynamicViewSet dynamicQuerySet, string sql, dynamic parameters = null, string name = null, string[] keyFieldNames = null) {
            this.Id = Guid.NewGuid().ToString();
            this.dynamicViewSet = dynamicQuerySet; 
            this.statement = new SelectStatement(dynamicQuerySet.Database, sql);
            this.statementParams = this.statement.ConvertParamsToDict(parameters);

            if (string.IsNullOrEmpty(name)) {
                if (this.statement.TableRefs.Length != 1) throw new System.Exception("Must specify name if the DynamicView contains multiple table references");
                this.name = this.statement.TableRefs[0].table.Name;
            }
            else {
                this.name = name;
            }

            if (keyFieldNames == null) {
                if (this.statement.TableRefs.Length != 1) throw new System.Exception("Must specify key field names if the DynamicView contains multiple table references");
                this.keyFieldNames = this.statement.TableRefs[0].table.Indexes[0].FieldNames;
            }
            else {
                this.keyFieldNames = keyFieldNames;
            }
        }

        public string Id {
            get;
            protected set;
        }

        public string Name => this.name;

        /*
        public BaseDynamicParam CreateMultiValueDynamicParam(string fieldName) {
            BaseDynamicParam dynamicParam = new MultiValueDynamicParam(fieldName);
            this.childDynamicParams.Add(new ChildDynamicParam(dynamicParam, fieldName));
            return dynamicParam;
        }
        */

        internal bool HasDirtyParams {
            get {
                foreach (var statementParamValue in this.statementParams.Values.ToArray()) {
                    if (statementParamValue is BaseDynamicParam dynamicParam && dynamicParam.Dirty) return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Executes the select statement of the DynamicQuery and returns a sequence of DataEvents starting an InitialBegin event, then an Insert event for each row, and then an InitialEnd event.
        /// </summary>
        /// <returns></returns>
        internal async Task<DataEvent[]> GetInitialDataEventsAsync() {
            logger.Debug($"GetInitialDataEventsAsync()");
            return await this.dynamicViewSet.Database.GetInitialDataEventsAsync(this.name, this.keyFieldNames, this.statement, this.statementParams);
        }

        internal void ResetDirtyParams() {
            logger.Trace($"ResetDirtyParams():Id={this.Id}");
            foreach (var childDynamicParam in this.childDynamicParams) {
                childDynamicParam.dynamicParam.ResetDirty();
            }
        }

        internal RecordDataEvent[] ProcessDataChange(DataEvent dataEvent, Dict[] preCommitImpactedRecords, Dict[] postCommitImpactedRecords) {
            logger.Trace($"ProcessDataChange():dataEvent={dataEvent},preCommitImpactedRecords.Length={preCommitImpactedRecords?.Length},postCommitImpactedRecords.Length={postCommitImpactedRecords?.Length}");
            if (!(dataEvent is KeyValueDataEvent keyValueDataEvent)) return null;

            StatementTableRef tableRef = this.statement.FindTableRefByTableName(keyValueDataEvent.name);
            if (tableRef == null) return null;

            List<RecordDataEvent> recordDataEvents = new List<RecordDataEvent>();
            switch (keyValueDataEvent.dataEventType) {
                case DataEventType.Insert:
                    if (postCommitImpactedRecords != null) {
                        foreach (var impactedRecord in postCommitImpactedRecords) {
                            object keyValue = BaseDatabase.GetKeyValue(this.keyFieldNames, impactedRecord);
                            recordDataEvents.Add(new RecordDataEvent(DataEventType.Insert, this.name, keyValue, impactedRecord));
                        }
                    }
                    break;
                case DataEventType.Update:
                    var preCommitKeyValues = preCommitImpactedRecords==null ? new object[] { } : preCommitImpactedRecords.Select(x => BaseDatabase.GetKeyValue(this.keyFieldNames, x)).ToArray();
                    var postCommitKeyValues = postCommitImpactedRecords==null ? new object[] { } : postCommitImpactedRecords.Select(x => BaseDatabase.GetKeyValue(this.keyFieldNames, x)).ToArray();

                    // Find updates and deletes
                    for (int i=0; i<preCommitKeyValues.Length; i++) {
                        int postCommitIndex = Array.IndexOf(postCommitKeyValues, preCommitKeyValues[i]);
                        if (postCommitIndex>=0) {
                            if (!preCommitImpactedRecords[i].IsSame(postCommitImpactedRecords[postCommitIndex])) {
                                recordDataEvents.Add(new RecordDataEvent(DataEventType.Update, this.name, preCommitKeyValues[i], postCommitImpactedRecords[postCommitIndex]));
                            }
                        }
                        else {
                            recordDataEvents.Add(new RecordDataEvent(DataEventType.Delete, this.name, preCommitKeyValues[i], preCommitImpactedRecords[i]));
                        }
                    }

                    // Find inserts
                    for (int i = 0; i < postCommitKeyValues.Length; i++) {
                        int preCommitIndex = Array.IndexOf(preCommitKeyValues, postCommitKeyValues[i]);
                        if (preCommitIndex==-1) {
                            recordDataEvents.Add(new RecordDataEvent(DataEventType.Insert, this.name, postCommitKeyValues[i], postCommitImpactedRecords[i]));
                        }
                    }
                    break;
                case DataEventType.Delete:
                    if (preCommitImpactedRecords != null) {
                        foreach (var impactedRecord in preCommitImpactedRecords) {
                            object keyValue = BaseDatabase.GetKeyValue(this.keyFieldNames, impactedRecord);
                            recordDataEvents.Add(new RecordDataEvent(DataEventType.Delete, this.name, keyValue, impactedRecord));
                        }
                    }
                    break;
            }
            logger.Trace($"ProcessDataChange():recordDataEvents={string.Join(",", recordDataEvents)}");
            return recordDataEvents.ToArray();
        }

        internal async Task<Dict[]> GetImpactedRecordsAsync(KeyValueDataEvent keyValueDataEvent) {
            StatementTableRef tableRef = this.statement.FindTableRefByTableName(keyValueDataEvent.name);
            if (tableRef == null) return null;
            logger.Trace($"GetImpactedRecordsAsync():name={name},tableRef={tableRef}");

            StringBuilder newAndCondition = new StringBuilder();
            Dict newWhereParams = new Dict();

            Dict primaryKeyValues = BaseDatabase.ParseKeyValue(keyValueDataEvent.keyValue, this.dynamicViewSet.Database.Tables[keyValueDataEvent.name].Indexes[0].FieldNames);
            foreach (var fieldName in this.dynamicViewSet.Database.Tables[keyValueDataEvent.name].Indexes[0].FieldNames) {
                logger.Trace($"GetImpactedRecordsAsync():fieldName={fieldName}");

                string prefix;
                if (this.statement.TableRefs.Length == 1) {
                    prefix = "";
                }
                else if (!string.IsNullOrEmpty(tableRef.tableAlias)) {
                    prefix = $"{tableRef.tableAlias}.";
                }
                else {
                    prefix = $"{tableRef.table.Name}.";
                }
                logger.Trace($"GetImpactedRecordsAsync():prefix={prefix}");

                var paramName = $"__{ fieldName}";
                var condition = $"{prefix}{fieldName}=@{paramName}";
                logger.Trace($"GetImpactedRecordsAsync():condition={condition}");

                if (newAndCondition.Length > 0) newAndCondition.Append(" AND ");
                newAndCondition.Append(condition);

                newWhereParams[paramName] = primaryKeyValues[fieldName];
            }
            logger.Trace($"GetImpactedRecordsAsync():newAndCondition={newAndCondition}");

            return await this.dynamicViewSet.Database.SelectRowsAsync(this.statement, this.statementParams, newAndCondition.ToString(), newWhereParams);
        }

        internal void UpdateChildDynamicParams(DataEvent[] dataEvents) {
            foreach (var childDynamicParam in this.childDynamicParams) {
                foreach (var dataEvent in dataEvents) {
                    childDynamicParam.UpdateFrom(this.keyFieldNames, dataEvent);
                }
            }
        }
    }

    public class ChildDynamicParam {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public readonly BaseDynamicParam dynamicParam;
        public readonly string fieldName;

        protected readonly Dictionary<object, object> valueByKeyValue = new Dictionary<object, object>();

        public ChildDynamicParam(BaseDynamicParam dynamicParam, string fieldName) {
            this.dynamicParam = dynamicParam;
            this.fieldName = fieldName;
        }

        public void UpdateFrom(string[] keyFieldNames, DataEvent dataEvent) {
            logger.Trace($"UpdateFrom():this.fieldName={this.fieldName},dataEvent={dataEvent}");
            bool changed = false;
            if (dataEvent.dataEventType == DataEventType.InitialBegin) {
                if (this.valueByKeyValue.Count > 0) {
                    changed = true;
                    this.valueByKeyValue.Clear();
                    this.dynamicParam.Clear();
                }
            }
            else if (dataEvent is RecordDataEvent recordDataEvent) {
                switch (dataEvent.dataEventType) {
                    case DataEventType.Initial:
                    case DataEventType.Insert:
                    case DataEventType.Update: {
                            object newValue = recordDataEvent.record[this.fieldName];
                            if (!this.valueByKeyValue.TryGetValue(recordDataEvent.keyValue, out object existingValue) || existingValue != newValue) {
                                changed = true;
                            }
                            if (changed) {
                                this.valueByKeyValue[recordDataEvent.keyValue] = newValue;
                            }
                            break;
                        }
                    case DataEventType.Delete: {
                            changed = this.valueByKeyValue.Remove(recordDataEvent.keyValue);
                            break;
                        }
                }
            }

            if (changed) {
                if (dynamicParam is MultiValueDynamicParam multiValueDynamicParam) {
                    multiValueDynamicParam.SetValues(this.valueByKeyValue.Values.Distinct().ToArray());
                }
            }
        }
    }
}
