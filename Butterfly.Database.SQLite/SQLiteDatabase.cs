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
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using NLog;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Database.SQLite {

    /// <inheritdoc/>
    public class SQLiteDatabase : BaseDatabase {

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public SQLiteDatabase(string fileName) : base($"Data Source={fileName};Version=3;") {
            if (!File.Exists(fileName)) {
                SQLiteConnection.CreateFile(fileName);
            }
        }

        protected override async Task LoadSchemaAsync() {
            string commandText = "SELECT name FROM sqlite_master WHERE type='table';";
            using (var connection = new SQLiteConnection(this.ConnectionString)) {
                await connection.OpenAsync();
                var command = new SQLiteCommand(commandText, connection);
                using (var reader = await command.ExecuteReaderAsync()) {
                    while (await reader.ReadAsync()) {
                        string tableName = reader[0].ToString();
                        Table table = await this.LoadTableSchemaAsync(tableName);
                        this.tableByName[table.Name] = table;
                    }
                }
            }
        }

        protected override async Task<Table> LoadTableSchemaAsync(string tableName) {
            TableFieldDef[] fieldDefs = await this.GetFieldDefsAsync(tableName);
            TableIndex primaryIndex = await this.GetPrimaryIndexAsync(tableName);
            return new Table(tableName, fieldDefs, primaryIndex);
        }

        protected async Task<TableFieldDef[]> GetFieldDefsAsync(string tableName) {
            List<TableFieldDef> fields = new List<TableFieldDef>();
            string commandText = $"SELECT * FROM {tableName} WHERE 1 = 2";
            using (var connection = new SQLiteConnection(this.ConnectionString)) {
                await connection.OpenAsync();
                var command = new SQLiteCommand(commandText, connection);
                DataTable dataTable = new DataTable();
                using (var reader = await command.ExecuteReaderAsync()) {
                    dataTable.Load(reader);
                }
                foreach (DataColumn dataColumn in dataTable.Columns) {
                    bool isAutoIncrement = dataColumn.DataType == typeof(long) && dataTable.PrimaryKey.Length==1 && dataTable.PrimaryKey[0].ColumnName == dataColumn.ColumnName;
                    TableFieldDef fieldDef = new TableFieldDef(dataColumn.ColumnName, dataColumn.DataType, dataColumn.MaxLength, dataColumn.AllowDBNull, isAutoIncrement);
                    fields.Add(fieldDef);
                }
            }
            return fields.ToArray();
        }

        protected async Task<TableIndex> GetPrimaryIndexAsync(string tableName) {
            string commandText = $"SELECT * FROM {tableName} WHERE 1 = 2";
            using (var connection = new SQLiteConnection(this.ConnectionString)) {
                await connection.OpenAsync();
                var command = new SQLiteCommand(commandText, connection);
                DataTable dataTable = new DataTable();
                using (var reader = await command.ExecuteReaderAsync()) {
                    dataTable.Load(reader);
                }
                return new TableIndex("PrimaryKey", dataTable.PrimaryKey.Select(x => x.ColumnName).ToArray());
            }
        }

        protected override ITransaction CreateTransaction() {
            return new SQLiteTransaction(this);
        }

        protected override async Task<Dict[]> DoSelectRowsAsync(string executableSql, Dict executableParams) {
            SelectStatement statement = new SelectStatement(this, executableSql);

            List<Dict> rows = new List<Dict>();
            try {
                using (var connection = new SQLiteConnection(this.ConnectionString)) {
                    await connection.OpenAsync();
                    var command = new SQLiteCommand(executableSql, connection);
                    foreach (var keyValuePair in executableParams) {
                        command.Parameters.AddWithValue(keyValuePair.Key, keyValuePair.Value);
                    }
                    using (var reader = await command.ExecuteReaderAsync()) {
                        while (await reader.ReadAsync()) {
                            Dict row = new Dictionary<string, object>();
                            for (int i = 0; i < statement.FieldRefs.Length; i++) {
                                row[statement.FieldRefs[i].fieldAlias] = ConvertValue(statement.FieldRefs[i].fieldAlias, reader[i]);
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

        protected static object ConvertValue(string fieldName, object value) {
            if (value == null || value == DBNull.Value) {
                return null;
            }
            else {
                return value;
            }
        }

        public static (Type, int) ConvertPostgresType(string text) {
            Match match = PARSE_TYPE.Match(text);
            if (!match.Success) throw new Exception($"Could not parse SQL type '{text}'");

            string typeText = match.Groups["type"].Value;

            Type type;
            if (typeText.StartsWith("CHARACTER", StringComparison.OrdinalIgnoreCase)) {
                type = typeof(string);
            }
            else if (typeText.Equals("INTEGER", StringComparison.OrdinalIgnoreCase)) {
                type = typeof(long);
            }
            else if (typeText.Equals("BIGINT", StringComparison.OrdinalIgnoreCase)) {
                type = typeof(long);
            }
            else if (typeText.Equals("REAL", StringComparison.OrdinalIgnoreCase)) {
                type = typeof(float);
            }
            else if (typeText.Equals("DOUBLE PRECISION", StringComparison.OrdinalIgnoreCase)) {
                type = typeof(double);
            }
            else if (typeText.StartsWith("TIMESTAMP", StringComparison.OrdinalIgnoreCase)) {
                type = typeof(DateTime);
            }
            else {
                throw new Exception($"Unknown field type '{text}'");
            }

            string maxLengthText = match.Groups["maxLengthWithParens"].Value.Replace("(", "").Replace(")", "");
            if (!int.TryParse(maxLengthText, out int maxLength)) maxLength = -1;

            return (type, maxLength);
        }

    }
}
