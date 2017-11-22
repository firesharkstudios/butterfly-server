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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Npgsql;
using NLog;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Database.Postgres {

    public class PostgresDatabase : Database {

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public PostgresDatabase(string connectionString) : base(connectionString) {
        }

        public override async Task LoadSchemaAsync() {
            string commandText = "SELECT table_name FROM information_schema.tables WHERE table_schema='public'";
            using (var connection = new NpgsqlConnection(this.ConnectionString)) {
                await connection.OpenAsync();
                var command = new NpgsqlCommand(commandText, connection);
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
            FieldDef[] fieldDefs = await this.GetFieldDefsAsync(tableName);
            Index primaryIndex = await this.GetPrimaryIndexAsync(tableName);
            return new PostgresTable(this, tableName, fieldDefs, primaryIndex);
        }

        protected async Task<FieldDef[]> GetFieldDefsAsync(string tableName) {
            List<FieldDef> fields = new List<FieldDef>();
            string commandText = $"select column_name, data_type, character_maximum_length, is_nullable, column_default from INFORMATION_SCHEMA.COLUMNS where table_name = @tableName";
            using (var connection = new NpgsqlConnection(this.ConnectionString)) {
                await connection.OpenAsync();
                var command = new NpgsqlCommand(commandText, connection);
                command.Parameters.AddWithValue("tableName", tableName);
                using (var reader = await command.ExecuteReaderAsync()) {
                    while (await reader.ReadAsync()) {
                        string name = reader[0].ToString();
                        string typeText = reader[1].ToString();
                        string maxLengthText = reader[2].ToString();
                        string allowNullText = reader[3].ToString();
                        string defaultText = reader[4].ToString();

                        if (!int.TryParse(maxLengthText, out int maxLength)) maxLength = -1;
                        bool allowNull = allowNullText.Equals("YES", StringComparison.OrdinalIgnoreCase);
                        bool isAutoIncrement = defaultText.StartsWith("NEXTVAL(", StringComparison.OrdinalIgnoreCase);
                        (Type type, _) = ConvertType(typeText);
                        fields.Add(new FieldDef(name, type, maxLength, allowNull, isAutoIncrement));
                    }
                }
            }
            return fields.ToArray();
        }

        protected async Task<Index> GetPrimaryIndexAsync(string tableName) {
            Index primaryIndex = null;
            string commandText = @"SELECT
                  trel.relname AS table_name,
                  irel.relname AS index_name,
                  i.indisunique AS is_unique,
                  i.indisprimary AS is_primary,  
                  array_agg (a.attname ORDER BY c.ordinality) AS columns
                FROM pg_index AS i
                JOIN pg_class AS trel ON trel.oid = i.indrelid
                JOIN pg_namespace AS tnsp ON trel.relnamespace = tnsp.oid
                JOIN pg_class AS irel ON irel.oid = i.indexrelid
                CROSS JOIN LATERAL unnest (i.indkey) WITH ORDINALITY AS c (colnum, ordinality)
                JOIN pg_attribute AS a ON trel.oid = a.attrelid AND a.attnum = c.colnum
                WHERE trel.relname=@tableName AND i.indisprimary='True'
                GROUP BY tnsp.nspname, trel.relname, irel.relname, i.indisunique, i.indisprimary";
            using (var connection = new NpgsqlConnection(this.ConnectionString)) {
                await connection.OpenAsync();
                var command = new NpgsqlCommand(commandText, connection);
                command.Parameters.AddWithValue("tableName", tableName);
                using (var reader = await command.ExecuteReaderAsync()) {
                    while (await reader.ReadAsync()) {
                        string indexName = reader[1].ToString();
                        string isUniqueText = reader[2].ToString();
                        string isPrimaryText = reader[3].ToString();
                        string[] fieldNames = (string[])reader[4];

                        //string[] fieldNames = fieldNamesText.Replace("{", "").Replace("}", "").Split(',').Select(x => x.Trim()).ToArray();
                        primaryIndex = new Index(indexName, fieldNames);
                    }
                }
            }

            if (primaryIndex == null) throw new Exception($"Unable to determine primary index on table '{tableName}'");
            return primaryIndex;
        }

        protected override ITransaction CreateTransaction() {
            return new PostgresTransaction(this);
        }

        protected override async Task<Dict[]> DoSelectRowsAsync(string executableSql, Dict executableParams) {
            SelectStatement statement = new SelectStatement(this, executableSql);

            List<Dict> rows = new List<Dict>();
            try {
                using (var connection = new NpgsqlConnection(this.ConnectionString)) {
                    await connection.OpenAsync();
                    var command = new NpgsqlCommand(executableSql, connection);
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

        public static (Type, int) ConvertType(string text) {
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
