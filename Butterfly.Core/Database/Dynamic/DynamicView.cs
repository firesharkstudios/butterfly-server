/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NLog;

using Butterfly.Core.Database.Event;
using Butterfly.Core.Util;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Core.Database.Dynamic {
    /// <summary>
    /// Represents a specific view (SELECT statement) that should be executed
    /// to return the initial data as a sequence of <see cref="DataEvent"/> instances
    /// and should publish <see cref="DataEvent"/> instances when any data in the
    /// view changes
    /// </summary>
    public class DynamicView {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly DynamicViewSet dynamicViewSet;
        protected readonly SelectStatement selectStatement;
        protected readonly Dict varsDict;
        protected readonly string name;
        protected readonly string[] keyFieldNames;
        protected readonly Dictionary<string, StatementFromRef> dynamicStatementFromRefByTableName = new Dictionary<string, StatementFromRef>();

        protected readonly List<ChildDynamicParam> childDynamicParams = new List<ChildDynamicParam>();

        public DynamicView(DynamicViewSet dynamicQuerySet, string sql, dynamic vars = null, string name = null, string[] keyFieldNames = null, string[] dynamicTableAliases = null) {
            this.Id = Guid.NewGuid().ToString();
            this.dynamicViewSet = dynamicQuerySet; 
            this.selectStatement = new SelectStatement(dynamicQuerySet.Database, sql);
            this.varsDict = this.selectStatement.ConvertParamsToDict(vars);

            if (string.IsNullOrEmpty(name)) {
                //if (this.statement.TableRefs.Length != 1) throw new System.Exception("Must specify name if the DynamicView contains multiple table references");
                if (this.selectStatement.StatementFromRefs.Length > 1) {
                    logger.Debug($"DynamicView():Using '{this.selectStatement.StatementFromRefs[0].table.Name}' for the name of the dynamic view even though the SQL contained tables '{string.Join(",", this.selectStatement.StatementFromRefs.Select(x => x.table.Name))}'");
                }
                this.name = this.selectStatement.StatementFromRefs[0].table.Name;
            }
            else {
                this.name = name;
            }

            if (keyFieldNames == null) {
                //if (this.statement.TableRefs.Length != 1) throw new System.Exception("Must specify key field names if the DynamicView contains multiple table references");
                if (this.selectStatement.StatementFromRefs.Length > 1) {
                    logger.Debug($"DynamicView():Using the key field names of the primary key of table '{this.selectStatement.StatementFromRefs[0].table.Name}' for the dynamic view name even though the SQL contained tables '{string.Join(",", this.selectStatement.StatementFromRefs.Select(x => x.table.Name))}'");
                }
                this.keyFieldNames = this.selectStatement.StatementFromRefs[0].table.Indexes[0].FieldNames;
            }
            else {
                this.keyFieldNames = keyFieldNames;
            }

            if (dynamicTableAliases==null) {
                foreach (var statementFromRef in this.selectStatement.StatementFromRefs) {
                    if (!this.dynamicStatementFromRefByTableName.ContainsKey(statementFromRef.table.Name)) {
                        this.dynamicStatementFromRefByTableName[statementFromRef.table.Name] = statementFromRef;
                    }
                }
            }
            else {
                foreach (var tableAlias in dynamicTableAliases) {
                    var statementFromRef = Array.Find(this.selectStatement.StatementFromRefs, x => x.tableAlias == tableAlias || (x.tableAlias == null && x.table.Name == tableAlias));
                    if (statementFromRef == null) throw new Exception($"Dynamic table alias {tableAlias} not found");
                    if (this.dynamicStatementFromRefByTableName.ContainsKey(statementFromRef.table.Name)) throw new Exception($"Table {statementFromRef.table.Name} can only have one dynamic alias");
                    this.dynamicStatementFromRefByTableName[statementFromRef.table.Name] = statementFromRef;
                }
            }
        }

        public string Id {
            get;
            protected set;
        }

        public string Name => this.name;

        public bool TryGetDynamicStatementFromRef(string tableName, out StatementFromRef dynamicStatementFromRef) => this.dynamicStatementFromRefByTableName.TryGetValue(tableName, out dynamicStatementFromRef);

        /*
        public BaseDynamicParam CreateMultiValueDynamicParam(string fieldName) {
            BaseDynamicParam dynamicParam = new MultiValueDynamicParam(fieldName);
            this.childDynamicParams.Add(new ChildDynamicParam(dynamicParam, fieldName));
            return dynamicParam;
        }
        */

        internal bool HasDirtyParams {
            get {
                foreach (var statementParamValue in this.varsDict.Values.ToArray()) {
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
            return await this.dynamicViewSet.Database.GetInitialDataEventsAsync(this.name, this.keyFieldNames, this.selectStatement, this.varsDict);
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

            if (!this.selectStatement.HasTableInFrom(keyValueDataEvent.name)) return null;

            List<RecordDataEvent> recordDataEvents = new List<RecordDataEvent>();
            /*
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
                */
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
            /*
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
            */
            logger.Trace($"ProcessDataChange():recordDataEvents={string.Join(",", recordDataEvents)}");
            return recordDataEvents.ToArray();
        }

        /*
         * Given this SQL...
         * 
         *  SELECT e.name, d.name
         *  FROM employee e
         *      LEFT JOIN department d ON e.department_id=d.id
         *  WHERE e.name LIKE 'm%'
         *  
         * An event on employee would be checked by running...
         * 
         *  SELECT e.name, d.name
         *  FROM employee e
         *      LEFT JOIN department d ON e.department_id=d.id
         *  WHERE e.name LIKE 'm%' AND e.id='123'
         *  
         * An event on department would be checked by running...
         * 
         *  SELECT e.name, d.name
         *  FROM employee e
         *      LEFT JOIN department d ON e.department_id=d.id AND d.id='456'
         *  WHERE e.name LIKE 'm%'
         *  
         */
        internal async Task<Dict[]> GetImpactedRecordsAsync(KeyValueDataEvent keyValueDataEvent) {
            if (!this.dynamicStatementFromRefByTableName.TryGetValue(keyValueDataEvent.name, out StatementFromRef selectedStatementFromRef)) return null;
            logger.Trace($"GetImpactedRecordsAsync():name={name}");

            StringBuilder newFromClause = new StringBuilder();
            string newWhereClause = this.selectStatement.whereClause;
            Dict newVarsDict = new Dict(this.varsDict);

            foreach (StatementFromRef statementFromRef in this.selectStatement.StatementFromRefs) {
                StringBuilder sb = new StringBuilder();
                if (statementFromRef == selectedStatementFromRef) {
                    Dict primaryKeyValues = BaseDatabase.ParseKeyValue(keyValueDataEvent.keyValue, this.dynamicViewSet.Database.TableByName[keyValueDataEvent.name].Indexes[0].FieldNames);

                    foreach (var fieldName in this.dynamicViewSet.Database.TableByName[keyValueDataEvent.name].Indexes[0].FieldNames) {
                        logger.Trace($"GetImpactedRecordsAsync():tableName={statementFromRef.table.Name},fieldName={fieldName}");

                        string prefix;
                        if (this.selectStatement.StatementFromRefs.Length == 1) {
                            prefix = "";
                        }
                        else if (!string.IsNullOrEmpty(statementFromRef.tableAlias)) {
                            prefix = $"{statementFromRef.tableAlias}.";
                        }
                        else {
                            prefix = $"{statementFromRef.table.Name}.";
                        }
                        logger.Trace($"GetImpactedRecordsAsync():prefix={prefix}");

                        var paramName = $"__{fieldName}";
                        var condition = $"{prefix}{fieldName}=@{paramName}";
                        logger.Trace($"GetImpactedRecordsAsync():condition={condition}");

                        if (sb.Length > 0) sb.Append(" AND ");
                        sb.Append(condition);

                        newVarsDict[paramName] = primaryKeyValues[fieldName];
                    }
                    logger.Trace($"GetImpactedRecordsAsync():newAndCondition={newWhereClause}");
                }

                // JoinType = None only happens on first table reference
                if (statementFromRef.joinType == JoinType.None) {
                    newFromClause.Append(statementFromRef.table.Name);
                    if (!string.IsNullOrEmpty(statementFromRef.tableAlias)) {
                        newFromClause.Append($" {statementFromRef.tableAlias}");
                    }

                    if (sb.Length > 0) {
                        if (string.IsNullOrEmpty(newWhereClause)) newWhereClause = sb.ToString();
                        else newWhereClause = $"({newWhereClause}) AND ({sb.ToString()})";
                    }
                }
                else {
                    newFromClause.Append($" {statementFromRef.joinType.ToString().ToUpper()} JOIN");
                    newFromClause.Append($" {statementFromRef.table.Name}");
                    if (!string.IsNullOrEmpty(statementFromRef.tableAlias)) {
                        newFromClause.Append($" {statementFromRef.tableAlias}");
                    }
                    newFromClause.Append($" ON");

                    if (sb.Length > 0) {
                        newFromClause.Append($" ({statementFromRef.joinCondition}) AND ({sb.ToString()})");
                    }
                    else {
                        newFromClause.Append($" {statementFromRef.joinCondition}");
                    }
                }
            }

            SelectStatement newStatement = new SelectStatement(this.dynamicViewSet.Database, this.selectStatement.selectClause, newFromClause.ToString(), newWhereClause.ToString(), null);

            return await this.dynamicViewSet.Database.SelectRowsAsync(newStatement, newVarsDict);
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
