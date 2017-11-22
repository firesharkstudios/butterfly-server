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
using System.Data;
using System.Threading.Tasks;

using NLog;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Database.Memory {
    public class MemoryTransaction : BaseTransaction {

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly HashSet<MemoryTable> changedTables = new HashSet<MemoryTable>();

        public MemoryTransaction(MemoryDatabase database) : base(database) {
        }

        public override Task BeginAsync() {
            return Task.FromResult(0);
        }

        protected override Task DoCommit() {
            foreach (var changedTable in this.changedTables) {
                changedTable.DataTable.AcceptChanges();
            }
            return Task.FromResult(0);
        }

        protected override void DoRollback() {
            foreach (var changedTable in this.changedTables) {
                changedTable.DataTable.RejectChanges();
            }
        }

        public override void Dispose() {
            this.changedTables.Clear();
        }

        protected override Task<bool> DoCreateAsync(CreateStatement statement) {
            var dataTable = new DataTable(statement.TableName);

            foreach (var fieldDef in statement.FieldDefs) {
                DataColumn dataColumn = new DataColumn(fieldDef.name, fieldDef.type);
                dataColumn.AutoIncrement = fieldDef.isAutoIncrement;
                dataTable.Columns.Add(dataColumn);
            }

            dataTable.PrimaryKey = Array.ConvertAll(statement.PrimaryIndex.FieldNames, x => dataTable.Columns[x]);

            Table table = new MemoryTable(dataTable, statement.FieldDefs, statement.PrimaryIndex);
            this.database.Tables.Add(table.Name, table);

            return Task.FromResult(true);
        }

        protected override Task<Func<object>> DoInsertAsync(string executableSql, Dict executableParams, bool ignoreIfDuplicate) {
            InsertStatement executableStatement = new InsertStatement(this.database, executableSql);
            var memoryTable = executableStatement.TableRefs[0].table as MemoryTable;

            var dataRow = memoryTable.DataTable.NewRow();
            foreach (var nameValuePair in executableParams) {
                dataRow[nameValuePair.Key] = nameValuePair.Value;
            }
            memoryTable.DataTable.Rows.Add(dataRow);

            this.changedTables.Add(memoryTable);

            if (memoryTable.AutoIncrementFieldName==null) {
                return Task.FromResult<Func<object>>(null);
            }
            else {
                return Task.FromResult<Func<object>>(() => dataRow[memoryTable.AutoIncrementFieldName]);
            }
        }

        protected override Task<int> DoUpdateAsync(string executableSql, Dict executableParams) {
            UpdateStatement executableStatement = new UpdateStatement(this.database, executableSql);
            if (!(executableStatement.TableRefs[0].table is MemoryTable memoryTable)) throw new Exception("Table is not a MemoryTable");

            string evaluatedWhereClause = MemoryDatabase.EvaluateWhereClause(executableStatement.whereClause, executableParams, executableStatement.TableRefs);
            var dataRows = memoryTable.DataTable.Select(evaluatedWhereClause);
            int count = 0;
            foreach (var dataRow in dataRows) {
                bool changed = false;
                foreach (var setRef in executableStatement.SetRefs) {
                    object value = executableParams[setRef.paramName];
                    if (dataRow[setRef.fieldName] != value) {
                        dataRow[setRef.fieldName] = value;
                        changed = true;
                    }
                }
                if (changed) count++;
            }
            this.changedTables.Add(memoryTable);
            return Task.FromResult(count);
        }

        protected override Task<int> DoDeleteAsync(string executableSql, Dict executableParams) {
            DeleteStatement executableStatement = new DeleteStatement(this.database, executableSql);
            var memoryTable = executableStatement.TableRefs[0].table as MemoryTable;

            string evaluatedWhereClause = MemoryDatabase.EvaluateWhereClause(executableStatement.whereClause, executableParams, executableStatement.TableRefs);
            var dataRows = memoryTable.DataTable.Select(evaluatedWhereClause);
            foreach (var dataRow in dataRows) {
                dataRow.Delete();
            }
            this.changedTables.Add(memoryTable);
            return Task.FromResult(dataRows.Length);
        }

        protected override Task DoTruncateAsync(string tableName) {
            if (!this.database.Tables.TryGetValue(tableName, out Table table)) throw new Exception($"Invalid table name '{tableName}'");
            if (!(table is MemoryTable memoryTable)) throw new Exception($"Invalid table type {table.GetType()}");            
            memoryTable.DataTable.Clear();
            return Task.FromResult(0);
        }

    }
}
