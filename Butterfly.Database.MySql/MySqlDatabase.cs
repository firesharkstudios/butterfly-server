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
using System.Threading.Tasks;

using MySql.Data.MySqlClient;
using NLog;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Database.MySql {

    /// <inheritdoc/>
    public class MySqlDatabase : BaseDatabase {

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public MySqlDatabase(string connectionString) : base(connectionString) {
        }

        protected override void LoadSchema() {
            try {
                string commandText = "show tables";
                using (MySqlDataReader reader = MySqlHelper.ExecuteReader(this.ConnectionString, commandText)) {
                    while (reader.Read()) {
                        string tableName = reader[0].ToString();
                        Table table = this.LoadTableSchema(tableName);
                        this.tableByName[tableName] = table;
                    }
                }
            }
            catch (MySqlException e) {
                if (e.Message.StartsWith("Unable to connect")) {
                    throw new UnableToConnectDatabaseException(e.Message);
                }
                else {
                    throw;
                }
            }
        }

        protected override Table LoadTableSchema(string tableName) {
            TableFieldDef[] fieldDefs = this.GetFieldDefs(tableName);
            TableIndex primaryIndex = this.GetPrimaryIndex(tableName);
            return new Table(tableName, fieldDefs, primaryIndex);
        }

        protected TableFieldDef[] GetFieldDefs(string tableName) {
            List<TableFieldDef> fields = new List<TableFieldDef>();
            string commandText = $"DESCRIBE {tableName}";
            using (MySqlDataReader reader = MySqlHelper.ExecuteReader(this.ConnectionString, commandText)) {
                while (reader.Read()) {
                    string name = reader[0].ToString();
                    string typeText = reader[1].ToString();
                    string allowNullText = reader[2].ToString();
                    string key = reader[3].ToString();
                    string extra = reader[5].ToString();

                    (Type type, int maxLength) = BaseDatabase.ConvertMySqlType(typeText);
                    bool allowNull = allowNullText.Equals("YES", StringComparison.OrdinalIgnoreCase);
                    bool isAutoIncrement = !string.IsNullOrEmpty(extra) && extra.Contains("auto_increment");
                    fields.Add(new TableFieldDef(name, type, maxLength, allowNull, isAutoIncrement));
                }
            }
            return fields.ToArray();
        }

        protected TableIndex GetPrimaryIndex(string tableName) {
            List<TableIndex> uniqueIndexes = new List<TableIndex>();
            string commandText = $"SHOW INDEX FROM {tableName}";
            string lastIndexName = null;
            List<string> lastFieldNames = new List<string>();
            using (MySqlDataReader reader = MySqlHelper.ExecuteReader(this.ConnectionString, commandText)) {
                while (reader.Read()) {
                    bool unique = int.Parse(reader[1].ToString()) == 0;
                    if (unique) {
                        string indexName = reader[2].ToString();
                        string columnName = reader[4].ToString();

                        if (indexName != lastIndexName) {
                            if (lastFieldNames.Count > 0) {
                                uniqueIndexes.Add(new TableIndex(lastIndexName, lastFieldNames.ToArray()));
                            }
                            lastIndexName = indexName;
                            lastFieldNames.Clear();
                        }
                        lastFieldNames.Add(columnName);
                    }
                }
                if (lastFieldNames.Count > 0) {
                    TableIndex uniqueIndex = new TableIndex(lastIndexName, lastFieldNames.ToArray());
                    uniqueIndexes.Add(uniqueIndex);
                }
            }

            //this.Indexes = uniqueIndexes.ToArray();
            TableIndex primaryIndex = Array.Find(uniqueIndexes.ToArray(), x => x.Name == "PRIMARY");
            if (primaryIndex == null) throw new Exception($"Unable to determine primary index on table '{tableName}'");
            return primaryIndex;
        }



        protected override BaseTransaction CreateTransaction() {
            return new MySqlTransaction(this);
        }

        protected override async Task<Dict[]> DoSelectRowsAsync(string executableSql, Dict executableParams) {
            SelectStatement statement = new SelectStatement(this, executableSql);
            MySqlParameter[] mySqlParams = executableParams.Select(keyValuePair => new MySqlParameter(keyValuePair.Key, keyValuePair.Value)).ToArray();

            List<Dict> rows = new List<Dict>();
            try {
                using (MySqlDataReader reader = await MySqlHelper.ExecuteReaderAsync(this.ConnectionString, executableSql, mySqlParams)) {
                    while (reader.Read()) {
                        Dict row = new Dictionary<string, object>();
                        for (int i = 0; i < statement.FieldRefs.Length; i++) {
                            row[statement.FieldRefs[i].fieldAlias] = ConvertValue(statement.FieldRefs[i].fieldAlias, reader[i]);
                        }
                        rows.Add(row);
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
    }
}
