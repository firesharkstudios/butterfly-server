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
    public class DynamicSelect {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly DynamicSelectGroup dynamicQuerySet;
        protected readonly SelectStatement selectStatement;
        protected readonly Dict selectParams;
        protected readonly string name;
        protected readonly string[] keyFieldNames;

        protected readonly List<DynamicParamDependency> dynamicParamDependencies = new List<DynamicParamDependency>();

        public DynamicSelect(DynamicSelectGroup dynamicQuerySet, string sql, dynamic parameters = null, string name = null, string[] keyFieldNames = null) {
            this.Id = Guid.NewGuid().ToString();
            this.dynamicQuerySet = dynamicQuerySet; 
            this.selectStatement = new SelectStatement(dynamicQuerySet.Database, sql);
            this.selectParams = this.selectStatement.ConvertParamsToDict(parameters);
            this.name = string.IsNullOrEmpty(name) ? string.Join("_", this.selectStatement.TableRefs.Select(x => x.table.Name)) : name;

            if (keyFieldNames == null) {
                if (this.selectStatement.TableRefs.Length != 1) throw new System.Exception("Must specify key field names if the DynamicSelect contains multiple table references");
                this.keyFieldNames = dynamicQuerySet.Database.Tables[this.selectStatement.TableRefs[0].table.Name].PrimaryIndex.FieldNames;
            }
            else {
                this.keyFieldNames = keyFieldNames;
            }
        }

        public string Id {
            get;
            protected set;
        }

        public DynamicParam CreateMultiValueDynamicParam(string fieldName) {
            DynamicParam dynamicParam = new MultiValueDynamicParam(fieldName);
            this.dynamicParamDependencies.Add(new DynamicParamDependency(dynamicParam, fieldName));
            return dynamicParam;
        }

        public bool HasDirtyParams {
            get {
                foreach (var whereParameter in this.selectParams) {
                    if (whereParameter.Value is DynamicParam dynamicParam && dynamicParam.Dirty) return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Executes the select statement of the DynamicQuery and returns a sequence of DataEvents starting an InitialBegin event, then an Insert event for each row, and then an InitialEnd event.
        /// </summary>
        /// <returns></returns>
        public async Task<DataEvent[]> GetInitialDataEventsAsync() {
            return await this.dynamicQuerySet.Database.GetInitialDataEventsAsync(this.name, this.keyFieldNames, this.selectStatement, this.selectParams);
        }

        public void ResetDirtyOnDynamicParams() {
            foreach (var dynamicParamDependency in this.dynamicParamDependencies) {
                dynamicParamDependency.ResetDirty();
            }
        }

        public ChangeDataEvent[] ProcessDataChange(DataEvent dataEvent, Dict[] preCommitImpactedRecords, Dict[] postCommitImpactedRecords) {
            logger.Debug($"ProcessDataChange():dataEvent={dataEvent},preCommitImpactedRecords.Length={preCommitImpactedRecords?.Length},postCommitImpactedRecords.Length={postCommitImpactedRecords?.Length}");
            TableRef tableRef = this.selectStatement.FindTableRefByTableName(dataEvent.name);
            if (tableRef == null) return null;

            if (!(dataEvent is ChangeDataEvent dataChange)) return null;

            List<ChangeDataEvent> newDataChanges = new List<ChangeDataEvent>();
            switch (dataChange.dataEventType) {
                case DataEventType.Insert:
                    foreach (var impactedRecord in postCommitImpactedRecords) {
                        newDataChanges.Add(new ChangeDataEvent(DataEventType.Insert, this.name, impactedRecord));
                    }
                    break;
                case DataEventType.Update:
                    var preCommitKeyValues = preCommitImpactedRecords.Select(x => Database.GetKeyValue(this.keyFieldNames, x)).ToArray();
                    var postCommitKeyValues = postCommitImpactedRecords.Select(x => Database.GetKeyValue(this.keyFieldNames, x)).ToArray();

                    // Find updates and deletes
                    for (int i=0; i<preCommitKeyValues.Length; i++) {
                        int postCommitIndex = Array.IndexOf(postCommitKeyValues, preCommitKeyValues[i]);
                        if (postCommitIndex>=0) {
                            if (!preCommitImpactedRecords[i].IsSame(postCommitImpactedRecords[postCommitIndex])) {
                                newDataChanges.Add(new ChangeDataEvent(DataEventType.Update, this.name, postCommitImpactedRecords[postCommitIndex]));
                            }
                        }
                        else {
                            newDataChanges.Add(new ChangeDataEvent(DataEventType.Delete, this.name, preCommitImpactedRecords[i]));
                        }
                    }

                    // Find inserts
                    for (int i = 0; i < postCommitKeyValues.Length; i++) {
                        int preCommitIndex = Array.IndexOf(preCommitKeyValues, postCommitKeyValues[i]);
                        if (preCommitIndex==-1) {
                            newDataChanges.Add(new ChangeDataEvent(DataEventType.Insert, this.name, postCommitImpactedRecords[i]));
                        }
                    }
                    break;
                case DataEventType.Delete:
                    foreach (var impactedRecord in preCommitImpactedRecords) {
                        newDataChanges.Add(new ChangeDataEvent(DataEventType.Delete, this.name, impactedRecord));
                    }
                    break;
            }
            return newDataChanges.ToArray();
        }

        public async Task<Dict[]> GetImpactedRecordsAsync(ChangeDataEvent dataChange) {
            TableRef tableRef = this.selectStatement.FindTableRefByTableName(dataChange.name);
            if (tableRef == null) return null;

            StringBuilder newAndCondition = new StringBuilder();
            Dict newWhereParams = new Dict();
            foreach (var fieldName in this.dynamicQuerySet.Database.Tables[dataChange.name].PrimaryIndex.FieldNames) {
                string prefix;
                if (this.selectStatement.TableRefs.Length == 1) {
                    prefix = "";
                }
                else if (!string.IsNullOrEmpty(tableRef.tableAlias)) {
                    prefix = $"{tableRef.tableAlias}.";
                }
                else {
                    prefix = $"{tableRef.table.Name}.";
                }

                string paramName = $"__{ fieldName}";
                if (newAndCondition.Length > 0) newAndCondition.Append(" AND ");
                newAndCondition.Append($"{prefix}{fieldName}=@{paramName}");

                newWhereParams[paramName] = dataChange.record[fieldName];
            }
            return await this.dynamicQuerySet.Database.SelectRowsAsync(this.selectStatement, this.selectParams, newAndCondition.ToString(), newWhereParams);
        }

        public void UpdateDynamicParams(DataEvent[] dataEvents) {
            foreach (var dataEvent in dataEvents) {
                this.UpdateDynamicParams(dataEvent);
            }
        }

        public void UpdateDynamicParams(DataEvent dataEvent) {
            foreach (var dynamicParamDependency in this.dynamicParamDependencies) {
                dynamicParamDependency.UpdateFrom(this.keyFieldNames, dataEvent);
            }
        }

        protected bool IsMatch(Dict record) {
            return true;
        }
    }

    public class DynamicKeyEnter {
        public readonly Dict keyValueByTable = new Dict();
    }

    public class DynamicParamDependency {
        public readonly DynamicParam dynamicParam;
        public readonly string fieldName;

        protected readonly Dictionary<object, object> valueByKeyValue = new Dictionary<object, object>();

        public DynamicParamDependency(DynamicParam dynamicParam, string fieldName) {
            this.dynamicParam = dynamicParam;
            this.fieldName = fieldName;
        }

        public void ResetDirty() {
            this.dynamicParam.ResetDirty();
        }

        public void UpdateFrom(string[] keyFieldNames, DataEvent dataEvent) {
            bool changed = false;
            if (dataEvent.dataEventType == DataEventType.InitialBegin) {
                if (this.valueByKeyValue.Count > 0) {
                    changed = true;
                    this.valueByKeyValue.Clear();
                    this.dynamicParam.Clear();
                }
            }
            else if (dataEvent is ChangeDataEvent dataChange) {
                switch (dataEvent.dataEventType) {
                    case DataEventType.Initial:
                    case DataEventType.Insert:
                    case DataEventType.Update: {
                            object keyValue = Database.GetKeyValue(keyFieldNames, dataChange.record);
                            object newValue = dataChange.record[this.fieldName];
                            if (!this.valueByKeyValue.TryGetValue(keyValue, out object existingValue) || existingValue != newValue) {
                                changed = true;
                            }
                            if (changed) {
                                this.valueByKeyValue[keyValue] = newValue;
                            }
                            break;
                        }
                    case DataEventType.Delete: {
                            object keyValue = Database.GetKeyValue(keyFieldNames, dataChange.record);
                            changed = this.valueByKeyValue.Remove(keyValue);
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
