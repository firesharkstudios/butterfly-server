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
using System.Text.RegularExpressions;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Database {
    /// <summary>
    /// Internal class used to parse SELECT statements
    /// </summary>
    public class SelectStatement : BaseStatement {
        protected readonly static Regex STATEMENT_REGEX = new Regex(@"^SELECT\s+(.+?)\s+FROM\s+(.+?)(?:\s+WHERE\s+(.+?))?(?:\s+ORDER\s+BY\s+(.+?))?(?:\s+LIMIT\s+(.+?))?$", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        public readonly string selectClause;
        public readonly string fromClause;
        public readonly string whereClause;
        public readonly string orderByClause;
        public readonly int limit;

        public SelectStatement(IDatabase database, string sql, int overrideLimit = -1) {
            this.Sql = sql;

            // Confirm the sql is valid
            if (this.IsSqlTableName) {
                this.selectClause = null;
                this.fromClause = this.Sql;
                this.whereClause = null;
                this.orderByClause = null;
            }
            else {
                Match match = STATEMENT_REGEX.Match(this.Sql);
                if (!match.Success) throw new Exception($"Invalid sql '{this.Sql}'");

                // Extract each clause
                this.selectClause = match.Groups[1].Value.Trim();
                this.fromClause = match.Groups[2].Value.Trim();
                this.whereClause = match.Groups[3].Value.Trim();
                this.orderByClause = match.Groups[4].Value.Trim();

                if (overrideLimit == -1) {
                    string limitText = match.Groups[5].Value.Trim();
                    this.limit = string.IsNullOrEmpty(limitText) ? -1 : int.Parse(limitText);
                }
                else {
                    this.limit = overrideLimit;
                }
            }

            // Parse the FROM clause
            this.TableRefs = StatementTableRef.ParseTableRefs(database, this.fromClause);

            if (this.selectClause == "*" || string.IsNullOrEmpty(this.selectClause)) {
                if (this.TableRefs.Length != 1) throw new Exception("Select statement must have exactly one table to use * to select field names");
                this.FieldRefs = this.TableRefs[0].table.FieldDefs.Select(x => new StatementFieldRef(x.name)).ToArray();
            }
            else if (this.selectClause.EndsWith(".*")) {
                string[] parts = this.selectClause.Split('.');
                var tableRef = this.TableRefs.First(x => parts[0] == x.table.Name || parts[0] == x.tableAlias);
                if (tableRef==null) throw new Exception("Could not find table matching {this.selectClause}");
                this.FieldRefs = tableRef.table.FieldDefs.Select(x => new StatementFieldRef(x.name)).ToArray();
            }
            else {
                this.FieldRefs = StatementFieldRef.ParseFieldRefs(selectClause);
            }
        }

        public SelectStatement(SelectStatement sourceSelectStatement, string overrideWhereClause, bool ignoreOrderBy) {
            this.TableRefs = sourceSelectStatement.TableRefs;

            this.selectClause = sourceSelectStatement.selectClause;
            this.fromClause = sourceSelectStatement.fromClause;
            this.whereClause = overrideWhereClause;
            this.orderByClause = ignoreOrderBy ? null : sourceSelectStatement.orderByClause;

            StringBuilder sb = new StringBuilder();
            sb.Append($"SELECT {this.selectClause} FROM {this.fromClause} WHERE {this.whereClause}");
            if (!string.IsNullOrEmpty(this.orderByClause)) {
                sb.Append($" ORDER BY {this.orderByClause}");
            }
            this.Sql = sb.ToString();

            this.FieldRefs = sourceSelectStatement.FieldRefs;
        }

        public (string, Dict) GetExecutableSqlAndParams(Dict sourceParams) {
            string newSelectClause = string.IsNullOrEmpty(this.selectClause) ? "*" : this.selectClause;
            string newWhereClause = string.IsNullOrEmpty(this.whereClause) && sourceParams.Count > 0 ? string.Join(" AND ", sourceParams.Keys.Select(x => $"{x}=@{x}")) : this.whereClause;

            Dict dataParams = new Dict();
            foreach (var sourceParam in sourceParams) {
                // Get value
                object value;
                if (sourceParam.Value is IDynamicParam dynamicParam) {
                    value = dynamicParam.GetValue();
                }
                else {
                    value = sourceParam.Value;
                }

                Regex conditionRegex = new Regex($"(\\w+\\.)?(\\w+)\\s*(!=|=)\\s*\\@{sourceParam.Key}");
                // Convert where conditions like "id=@id" to "id IN (@id0, @id1, @id2)" if value is collection
                if (value is null || value.Equals(DBNull.Value)) {
                    newWhereClause = ReplaceAll(newWhereClause, conditionRegex, match => {
                        string tableAliasWithDot = match.Groups[1].Value.Trim();
                        string fieldName = match.Groups[2].Value.Trim();
                        string condition = match.Groups[3].Value.Trim();
                        string text = condition == "=" ? "IS NULL" : "IS NOT NULL";
                        return $"{tableAliasWithDot}{fieldName} {text}";
                    });
                }
                // Convert where conditions like "id=@id" to "id IN (@id0, @id1, @id2)" if value is collection
                else if (value is ICollection<object> collection) {
                    if (collection.Count == 0) {
                        newWhereClause = ReplaceAll(newWhereClause, conditionRegex, match => {
                            string condition = match.Groups[3].Value;
                            return condition == "=" ? "1=2" : "1=1";
                        });
                    }
                    else if (collection.Count == 1) {
                        dataParams.Add(sourceParam.Key, collection.First());
                    }
                    else {
                        int i = 0;
                        foreach (var item in collection) {
                            dataParams.Add($"{sourceParam.Key}{i}", item);
                            i++;
                        }

                        string rangeText = string.Join(",", collection.Select((x, index) => $"@{sourceParam.Key}{index}"));
                        newWhereClause = ReplaceAll(newWhereClause, conditionRegex, match => {
                            string tableAliasWithDot = match.Groups[1].Value.Trim();
                            string fieldName = match.Groups[2].Value.Trim();
                            string condition = match.Groups[3].Value.Trim();
                            string inText = condition == "=" ? "IN" : "NOT IN";
                            return $"{tableAliasWithDot}{fieldName} {inText} ({rangeText})";
                        });
                    }
                }
                else {
                    dataParams.Add(sourceParam.Key, value);
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.Append($"SELECT {newSelectClause} FROM {this.fromClause}");
            if (!string.IsNullOrEmpty(newWhereClause)) {
                sb.Append($" WHERE {newWhereClause}");
            }
            if (!string.IsNullOrEmpty(this.orderByClause)) {
                sb.Append($" ORDER BY {this.orderByClause}");
            }
            if (this.limit>0) {
                sb.Append($" LIMIT {this.limit}");
            }
            return (sb.ToString(), dataParams);
        }

        protected static string ReplaceAll(string sql, Regex regex, Func<Match, string> getReplacement) {
            StringBuilder sb = new StringBuilder();
            int lastIndex = 0;
            var matches = regex.Matches(sql);
            foreach (Match match in matches) {
                string replacement = getReplacement(match);
                sb.Append(sql.Substring(lastIndex, match.Index - lastIndex));
                sb.Append(replacement);
                lastIndex = match.Index + match.Length;
            }
            sb.Append(sql.Substring(lastIndex));
            return sb.ToString();
        }

        public StatementFieldRef[] FieldRefs {
            get;
            protected set;
        }
    }

    /// <summary>
    /// Internal class representing a SQL field reference like "table_alias.field_name field_alias"
    /// </summary>
    public class StatementFieldRef {
        public readonly string tableAlias;
        public readonly string fieldName;
        public readonly string fieldAlias;

        public StatementFieldRef(string fieldName) : this(null, fieldName, null) {
        }

        public StatementFieldRef(string tableAlias, string fieldName, string fieldAlias) {
            this.tableAlias = string.IsNullOrWhiteSpace(tableAlias) ? null : tableAlias;
            this.fieldName = fieldName;
            this.fieldAlias = string.IsNullOrWhiteSpace(fieldAlias) ? fieldName : fieldAlias;
        }

        protected readonly static Regex REGEX = new Regex(@"^(\w+\.)?(\w+)(\s+\w+)?");
        public static StatementFieldRef[] ParseFieldRefs(string selectClause) {
            List<StatementFieldRef> fieldRefs = new List<StatementFieldRef>();
            string[] selectClauseParts = selectClause.Split(',').Select(x => x.Trim()).ToArray();
            foreach (var selectClausePart in selectClauseParts) {
                Match match = REGEX.Match(selectClausePart);
                if (!match.Success) throw new Exception("Invalid field reference in SELECT clause '{selectClausePart}'");
                string tableAlias = match.Groups[1].Value.Trim();
                if (tableAlias.EndsWith(".")) tableAlias = tableAlias.Substring(0, tableAlias.Length - 1);
                string fieldName = match.Groups[2].Value.Trim();
                string fieldAlias = match.Groups[3].Value.Trim();
                fieldRefs.Add(new StatementFieldRef(tableAlias, fieldName, fieldAlias));
            }
            return fieldRefs.ToArray();
        }
    }

}
