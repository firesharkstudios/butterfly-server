/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

using NLog;

using Butterfly.Core.Util;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Core.Database.Memory {

    /// <inheritdoc/>
    public class MemoryTransaction : BaseTransaction {

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly HashSet<MemoryTable> changedTables = new HashSet<MemoryTable>();

        public MemoryTransaction(MemoryDatabase database) : base(database) {
        }

        public override void Begin() {
        }

        public override Task BeginAsync() {
            return Task.FromResult(0);
        }

        protected override void DoCommit() {
            foreach (var changedTable in this.changedTables) {
                changedTable.DataTable.AcceptChanges();
            }
        }

        protected override Task DoCommitAsync() {
            this.DoCommit();
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

        protected override bool DoCreate(CreateStatement statement) {
            var dataTable = new DataTable(statement.TableName);

            foreach (var fieldDef in statement.FieldDefs) {
                DataColumn dataColumn = new DataColumn(fieldDef.name, fieldDef.type) {
                    AutoIncrement = fieldDef.isAutoIncrement,
                    AllowDBNull = fieldDef.allowNull
                };
                dataTable.Columns.Add(dataColumn);
            }

            dataTable.PrimaryKey = Array.ConvertAll(statement.Indexes[0].FieldNames, x => dataTable.Columns[x]);

            Table table = new MemoryTable(dataTable, statement.FieldDefs, statement.Indexes);
            this.database.Tables.Add(table.Name, table);

            return true;
        }

        protected override Task<bool> DoCreateAsync(CreateStatement statement) {
            return Task.FromResult(this.DoCreate(statement));
        }

        protected override Task<Func<object>> DoInsertAsync(string executableSql, Dict executableParams, bool ignoreIfDuplicate) {
            InsertStatement executableStatement = new InsertStatement(this.database, executableSql);
            var memoryTable = executableStatement.StatementFromRefs[0].table as MemoryTable;

            var insertRefs = executableStatement.GetInsertRefs(executableParams);
            var fieldValues = BaseStatement.RemapStatementParamsToFieldValues(executableParams, insertRefs);

            var dataRow = memoryTable.DataTable.NewRow();
            foreach (var nameValuePair in fieldValues) {
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
            if (!(executableStatement.StatementFromRefs[0].table is MemoryTable memoryTable)) throw new Exception("Table is not a MemoryTable");

            (var whereIndex, var setRefs, var whereRefs) = executableStatement.GetWhereIndexSetRefsAndWhereRefs(this.database, executableParams);
            var fieldValues = BaseStatement.RemapStatementParamsToFieldValues(executableParams, setRefs);

            string evaluatedWhereClause = MemoryDatabase.EvaluateWhereClause(executableStatement.whereClause, executableParams, executableStatement.StatementFromRefs);
            var dataRows = memoryTable.DataTable.Select(evaluatedWhereClause);
            int count = 0;
            foreach (var dataRow in dataRows) {
                bool changed = false;
                foreach ((string name, object value) in fieldValues) {
                    //object value = executableParams[setRef.paramName];
                    if (dataRow[name] != value) {
                        dataRow[name] = value;
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
            var memoryTable = executableStatement.StatementFromRefs[0].table as MemoryTable;

            string evaluatedWhereClause = MemoryDatabase.EvaluateWhereClause(executableStatement.whereClause, executableParams, executableStatement.StatementFromRefs);
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
