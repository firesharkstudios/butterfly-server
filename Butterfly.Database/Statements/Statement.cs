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
using System.Text.RegularExpressions;

using Butterfly.Util;

using Dict = System.Collections.Generic.Dictionary<string, object>;
using System.Data;

namespace Butterfly.Database {
    public abstract class Statement {
        protected void SetSql(string sql, string defaultStatement) {
            this.Sql = sql.Contains(" ") ? sql : defaultStatement.Replace("@@tableName", sql);
        }

        public string Sql {
            get;
            protected set;
        }

        public TableRef[] TableRefs {
            get;
            protected set;
        }

        public TableRef FindTableRefByTableAlias(string tableAlias) {
            return Array.Find(this.TableRefs, x => x.tableAlias == tableAlias);
        }

        public TableRef FindTableRefByTableName(string tableName) {
            return Array.Find(this.TableRefs, x => x.table.Name == tableName);
        }

        public void ConfirmAllParamsUsed(Dict statementParams) {
            foreach (var key in statementParams.Keys) {
                string paramName = $"@{key}";
                if (!this.Sql.Contains(paramName)) throw new Exception($"Unused parameter '{key}'");
            }
        }

        public Dict ConvertParamsToDict(dynamic statementParams, bool allowKeyValueAsSourceParams = false) {
            // If statementParams is null, return empty dictionary
            if (statementParams == null) {
                return new Dict();
            }

            // If statementParams is already a dictionary, return the dictionary
            else if (statementParams is Dict d) {
                return d;
            }

            // If statementParams is a single string, assume it is a primary key value
            else if (statementParams is string keyValue) {
                if (!allowKeyValueAsSourceParams) throw new Exception("Statement doesn't allow passing single key value as source params");
                if (this.TableRefs.Length != 1) throw new Exception("Statement must have exactly one table to pass single string value as where condition");

                Dict result = new Dict();
                string[] keyValueParts = keyValue.Split(';');
                string[] keyFieldNames = this.TableRefs[0].table.PrimaryIndex.FieldNames;
                for (int i = 0; i < keyFieldNames.Length; i++) {
                    result[keyFieldNames[i]] = keyValueParts[i];
                }
                return result;
            }

            // Otherwise, convert statementParams to a dictionary
            else {
                return DynamicX.ToDictionary(statementParams);
            }
        }

        /*
        public static Dict ConvertParamsToFields(Dict statementParams) {

        }
        */
    }

    /*
     * "user u" would be...
     *   tableName=user
     *   tableAlias=u
     */
    public class TableRef {
        public readonly Table table;
        public readonly string tableAlias;

        public TableRef(Table table) : this(table, table.Name) {
        }

        public TableRef(Table table, string tableAlias) {
            this.table = table;
            this.tableAlias = string.IsNullOrWhiteSpace(tableAlias) ? null : tableAlias;
        }

        // chat_participant cp
        protected readonly static Regex FIRST_TABLE_REGEX = new Regex(@"^(\w+)(\s+\w+)?");

        // INNER JOIN user u ON cp.user_id=U.id
        protected readonly static Regex JOIN_REGEX = new Regex(@"\s+INNER\s+JOIN\s+(\w+)(\s+\w+)?\s+ON\s+(\w+)\.(\w+)=(\w+)\.(\w+)");

        public static TableRef[] ParseTableRefs(IDatabase database, string fromClause) {
            var match = FIRST_TABLE_REGEX.Match(fromClause);
            if (!match.Success) throw new Exception($"Invalid from clause '{fromClause}'");

            List<TableRef> tableRefs = new List<TableRef>();
            string firstTableName = match.Groups[1].Value.Trim();
            if (!database.Tables.TryGetValue(firstTableName, out Table firstTable)) throw new Exception($"Invalid table name '{firstTableName}'");
            tableRefs.Add(new TableRef(firstTable, match.Groups[2].Value.Trim()));

            var joinMatches = JOIN_REGEX.Matches(fromClause);
            foreach (Match joinMatch in joinMatches) {
                string joinTableName = joinMatch.Groups[1].Value.Trim();
                if (!database.Tables.TryGetValue(joinTableName, out Table joinTable)) throw new Exception($"Invalid table name '{joinTableName}'");
                tableRefs.Add(new TableRef(joinTable, joinMatch.Groups[2].Value.Trim()));
            }
            return tableRefs.ToArray();
        }
    }

    public class FieldDef {
        public readonly string name;
        public readonly Type type;
        public readonly int maxLength;
        public readonly bool allowNull;
        public readonly bool isAutoIncrement;

        public FieldDef(string name, Type type, int maxLength, bool allowNull, bool isAutoIncrement) {
            this.name = name;
            this.type = type;
            this.maxLength = maxLength;
            this.allowNull = allowNull;
            this.isAutoIncrement = isAutoIncrement;
        }

        public static FieldDef FromDataColumn(DataColumn dataColumn) {
            return new FieldDef(dataColumn.ColumnName, dataColumn.DataType, dataColumn.MaxLength, dataColumn.AllowDBNull, dataColumn.AutoIncrement);
        }
    }

    public class SqlEqualsRef {
        public readonly string tableName;
        public readonly string fieldName;
        public readonly string paramName;

        public SqlEqualsRef(string tableName, string fieldName, string paramName) {
            this.tableName = string.IsNullOrEmpty(tableName) ? null : tableName;
            this.fieldName = fieldName;
            this.paramName = paramName;
        }

        public static Dict GetMatchingParams(SqlEqualsRef[] equalsRefs, Dict sourceParams) {
            Dict result = new Dict();
            foreach (var keyValuePair in sourceParams) {
                SqlEqualsRef whereRef = Array.Find(equalsRefs, x => x.paramName == keyValuePair.Key);
                if (whereRef != null) {
                    result[whereRef.fieldName] = keyValuePair.Value;
                }
            }
            return result;
        }
    }
}
