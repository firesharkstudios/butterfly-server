/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using NLog;

using Butterfly.Core.Database;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.SQLite {

    /// <inheritdoc/>
    public class SQLiteDatabase : BaseDatabase {

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public override bool CanJoin => true;

        public override bool CanFieldAlias => true;

        public SQLiteDatabase(string connectionString) : base(connectionString) {
        }

        protected override async Task LoadSchemaAsync() {
            string commandText = "SELECT name FROM sqlite_master WHERE type='table';";
            using (var connection = new SqliteConnection(this.ConnectionString)) {
                connection.Open();
                var command = new SqliteCommand(commandText, connection);
                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        string tableName = reader[0].ToString();
                        Table table = await this.LoadTableSchemaAsync(tableName);
                        this.tableByName[table.Name] = table;
                    }
                }
            }
        }

        protected override async Task<Table> LoadTableSchemaAsync(string tableName) {
            TableFieldDef[] fieldDefs = await this.GetFieldDefs(tableName);
            TableIndex[] indexes = await this.GetIndexes(tableName, fieldDefs);
            return new Table(tableName, fieldDefs, indexes);
        }

        protected async Task<TableFieldDef[]> GetFieldDefs(string tableName) {
            List<TableFieldDef> fields = new List<TableFieldDef>();
            string commandText = $"SELECT * FROM {tableName} WHERE 1 = 2";
            using (var connection = new SqliteConnection(this.ConnectionString)) {
                await connection.OpenAsync();
                var command = new SqliteCommand(commandText, connection);
                DataTable dataTable = new DataTable();
                using (var reader = command.ExecuteReader()) {
                    dataTable.Load(reader);
                }
                foreach (DataColumn dataColumn in dataTable.Columns) {
                    bool isAutoIncrement = (dataColumn.DataType == typeof(long) || dataColumn.DataType == typeof(int)) && dataTable.PrimaryKey.Length==1 && dataTable.PrimaryKey[0].ColumnName == dataColumn.ColumnName;
                    TableFieldDef fieldDef = new TableFieldDef(dataColumn.ColumnName, dataColumn.DataType, dataColumn.MaxLength, dataColumn.AllowDBNull, isAutoIncrement);
                    fields.Add(fieldDef);
                }
            }
            return fields.ToArray();
        }

        protected async Task<TableIndex[]> GetIndexes(string tableName, TableFieldDef[] fieldDefs) {
            List<TableIndex> tableIndexes = new List<TableIndex>();
            string commandText = $"PRAGMA index_list({tableName});";
            using (var connection = new SqliteConnection(this.ConnectionString)) {
                await connection.OpenAsync();
                var command = new SqliteCommand(commandText, connection);
                using (var reader = command.ExecuteReader()) {
                    while (await reader.ReadAsync()) {
                        string indexName = null;
                        TableIndexType indexType = TableIndexType.Other;
                        for (int i = 0; i < reader.FieldCount; i++) {
                            var name = reader.GetName(i);
                            var value = ConvertValue(reader[i])?.ToString();
                            if (name == "name") indexName = value;
                            else if (name == "unique" && value == "1") indexType = TableIndexType.Unique;
                            else if (name == "origin" && value == "pk") indexType = TableIndexType.Primary;
                        }
                        if (!string.IsNullOrEmpty(indexName)) {
                            var fieldNames = await GetIndexFieldNames(indexName);
                            var tableIndex = new TableIndex(indexType, fieldNames);
                            tableIndexes.Add(tableIndex);
                        }
                    }
                }
            }

            // Not sure why auto increment fields don't have an index created in PRAGMA results
            foreach (var autoIncrementFieldDef in fieldDefs.Where(x => x.isAutoIncrement)) {
                tableIndexes.Add(new TableIndex(TableIndexType.Primary, new string[] { autoIncrementFieldDef.name }));
            }

            return tableIndexes.ToArray();
        }

        protected async Task<string[]> GetIndexFieldNames(string indexName) {
            List<string> fieldNames = new List<string>();
            string commandText = $"PRAGMA index_info({indexName});";
            using (var connection = new SqliteConnection(this.ConnectionString)) {
                await connection.OpenAsync();
                var command = new SqliteCommand(commandText, connection);
                using (var reader = command.ExecuteReader()) {
                    while (await reader.ReadAsync()) {
                        var fieldName = ConvertValue(reader[2])?.ToString();
                        fieldNames.Add(fieldName);
                    }
                }
                return fieldNames.ToArray();
            }
        }

        protected override BaseTransaction CreateTransaction() {
            return new SQLiteTransaction(this);
        }

        protected override async Task<Dict[]> DoSelectRowsAsync(string executableSql, Dict executableParams) {
            SelectStatement statement = new SelectStatement(this, executableSql);

            List<Dict> rows = new List<Dict>();
            try {
                using (var connection = new SqliteConnection(this.ConnectionString)) {
                    await connection.OpenAsync();
                    var command = new SqliteCommand(executableSql, connection);
                    foreach (var keyValuePair in executableParams) {
                        command.Parameters.AddWithValue(keyValuePair.Key, keyValuePair.Value);
                    }
                    using (var reader = await command.ExecuteReaderAsync()) {
                        while (await reader.ReadAsync()) {
                            Dict row = new Dictionary<string, object>();
                            for (int i=0; i<reader.FieldCount; i++) {
                                var name = reader.GetName(i);
                                var value = ConvertValue(reader[i]);
                                row[name] = value;
                            }
                            rows.Add(row);
                        }
                    }
                }
            }
            catch (Exception e) {
                logger.Error(e, $"Error executing {statement.Sql}...");
                throw;
            }

            return rows.ToArray();
        }

        protected override Task<Dict[]> DoQueryRowsAsync(string storedProcedureName, Dict vars = null) {
            throw new NotImplementedException();
        }

        protected static object ConvertValue(object value) {
            if (value == null || value == DBNull.Value) {
                return null;
            }
            else {
                return value;
            }
        }

    }
}
