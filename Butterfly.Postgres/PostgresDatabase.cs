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
using Butterfly.Core.Database;
using System.Collections.ObjectModel;
using System.Data.Common;

namespace Butterfly.Postgres {

    /// <inheritdoc/>
    public class PostgresDatabase : BaseDatabase {

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public PostgresDatabase(string connectionString) : base(connectionString) {
        }

        public override bool CanJoin => true;

        public override bool CanFieldAlias => true;

        protected override void LoadSchema() {
            string commandText = "SELECT table_name FROM information_schema.tables WHERE table_schema='public'";
            using (var connection = new NpgsqlConnection(this.ConnectionString)) {
                connection.Open();
                var command = new NpgsqlCommand(commandText, connection);
                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        string tableName = reader[0].ToString();
                        Table table = this.LoadTableSchema(tableName);
                        this.tableByName[table.Name] = table;
                    }
                }
            }
        }

        protected override Table LoadTableSchema(string tableName) {
            TableFieldDef[] fieldDefs = this.GetFieldDefs(tableName);
            TableIndex[] uniqueIndexes = this.GetUniqueIndexes(tableName);
            return new Table(tableName, fieldDefs, uniqueIndexes);
        }

        protected TableFieldDef[] GetFieldDefs(string tableName) {
            List<TableFieldDef> fields = new List<TableFieldDef>();
            string commandText = $"select column_name, data_type, character_maximum_length, is_nullable, column_default from INFORMATION_SCHEMA.COLUMNS where table_name = @tableName";
            using (var connection = new NpgsqlConnection(this.ConnectionString)) {
                connection.Open();
                var command = new NpgsqlCommand(commandText, connection);
                command.Parameters.AddWithValue("tableName", tableName);
                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        string name = reader[0].ToString();
                        string typeText = reader[1].ToString();
                        string maxLengthText = reader[2].ToString();
                        string allowNullText = reader[3].ToString();
                        string defaultText = reader[4].ToString();

                        if (!int.TryParse(maxLengthText, out int maxLength)) maxLength = -1;
                        bool allowNull = allowNullText.Equals("YES", StringComparison.OrdinalIgnoreCase);
                        bool isAutoIncrement = defaultText.StartsWith("NEXTVAL(", StringComparison.OrdinalIgnoreCase);
                        (Type type, _) = ConvertType(typeText);
                        fields.Add(new TableFieldDef(name, type, maxLength, allowNull, isAutoIncrement));
                    }
                }
            }
            return fields.ToArray();
        }

        protected TableIndex[] GetUniqueIndexes(string tableName) {
            List<TableIndex> uniqueIndexes = new List<TableIndex>();
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
                WHERE trel.relname=@tableName AND i.indisunique='True'
                GROUP BY tnsp.nspname, trel.relname, irel.relname, i.indisunique, i.indisprimary";
            using (var connection = new NpgsqlConnection(this.ConnectionString)) {
                connection.OpenAsync();
                var command = new NpgsqlCommand(commandText, connection);
                command.Parameters.AddWithValue("tableName", tableName);
                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        string indexName = reader[1].ToString();
                        string isUniqueText = reader[2].ToString();
                        string isPrimaryText = reader[3].ToString();
                        string[] fieldNames = (string[])reader[4];

                        TableIndexType tableIndexType;
                        if (isPrimaryText=="True") {
                            tableIndexType = TableIndexType.Primary;
                        }
                        else if (isUniqueText=="True") {
                            tableIndexType = TableIndexType.Unique;
                        }
                        else {
                            tableIndexType = TableIndexType.Other;
                        }
                        uniqueIndexes.Add(new TableIndex(tableIndexType, fieldNames));
                    }
                }
            }
            return uniqueIndexes.ToArray();
        }

        protected override BaseTransaction CreateTransaction() {
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
                        ReadOnlyCollection<DbColumn> columns = null;
                        while (await reader.ReadAsync()) {
                            if (columns == null) columns = reader.GetColumnSchema();
                            Dict row = new Dictionary<string, object>();
                            /*
                            for (int i = 0; i < statement.FieldRefs.Length; i++) {
                                row[statement.FieldRefs[i].fieldAlias] = ConvertValue(statement.FieldRefs[i].fieldAlias, reader[i]);
                            }
                            */
                            foreach (var column in columns) {
                                row[column.ColumnName] = ConvertValue(reader[column.ColumnName]);
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
