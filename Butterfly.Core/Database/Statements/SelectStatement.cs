/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Butterfly.Core.Util;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Core.Database {
    /// <summary>
    /// Internal class used to parse SELECT statements
    /// </summary>
    public class SelectStatement : BaseStatement {
        //protected readonly static Regex STATEMENT_REGEX = new Regex(@"^SELECT\s+(.+?)\s+FROM\s+(.+?)(?:\s+WHERE\s+(.+?))?(?:\s+ORDER\s+BY\s+(.+?))?$", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        protected readonly static StructuredParser STRUCTURED_PARSER = new StructuredParser().AddToken("SELECT", new Regex(@"\s*SELECT\s+"), true).AddToken("FROM", new Regex(@"\s*FROM\s+"), true).AddToken("WHERE", new Regex(@"\s*WHERE\s+"), false).AddToken("ORDER BY", new Regex(@"\s*ORDER BY\s+"), false);

        public readonly string selectClause;
        public readonly string fromClause;
        public readonly string whereClause;
        public readonly string orderByClause;
        public readonly int limit;

        public SelectStatement(IDatabase database, string sql, int limit = -1) {
            this.Sql = sql;

            // Confirm the sql is valid
            if (this.IsSqlTableName) {
                this.selectClause = null;
                this.fromClause = this.Sql;
                this.whereClause = null;
                this.orderByClause = null;
            }
            else {
                var result = STRUCTURED_PARSER.Parse(this.Sql);

                // Extract each clause
                this.selectClause = result.GetAs("SELECT", (string)null)?.Trim();
                this.fromClause = result.GetAs("FROM", (string)null)?.Trim();
                this.whereClause = result.GetAs("WHERE", (string)null)?.Trim();
                this.orderByClause = result.GetAs("ORDER BY", (string)null)?.Trim();
            }
            this.limit = limit;

            this.Compile(database);
        }

        public SelectStatement(IDatabase database, string selectClause, string fromClause, string whereClause, string orderByClause, int limit = -1) {
            this.selectClause = selectClause;
            this.fromClause = fromClause;
            this.whereClause = whereClause;
            this.orderByClause = orderByClause;
            this.limit = limit;

            this.Compile(database);
        }

        protected void Compile(IDatabase database) {
            this.StatementFromRefs = StatementFromRef.ParseFromRefs(database, this.fromClause);
        }

        public SelectStatement(SelectStatement sourceSelectStatement, string overrideWhereClause, bool ignoreOrderBy) {
            this.StatementFromRefs = sourceSelectStatement.StatementFromRefs;

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
        }

        public (string, Dict) GetExecutableSqlAndParams(Dict sourceParams) {
            string newSelectClause = string.IsNullOrEmpty(this.selectClause) ? "*" : this.selectClause;

            Dict sourceParamsNotInFromClause = sourceParams.Where(x => !this.fromClause.Contains($"@{x.Key}")).ToDictionary(x => x.Key, x => x.Value);
            string newWhereClause = string.IsNullOrEmpty(this.whereClause) && sourceParamsNotInFromClause.Count > 0 ? string.Join(" AND ", sourceParamsNotInFromClause.Keys.Select(x => $"{x}=@{x}")) : this.whereClause;

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
            /*
            if (this.limit>0) {
                sb.Append($" LIMIT {this.limit}");
            }
            */
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
    }

    /// <summary>
    /// Internal class representing a SQL field reference like "table_alias.field_name field_alias"
    /// </summary>
    public class StatementFieldRef {
        public readonly string tableAlias;
        public readonly string fieldExpression;
        public readonly string fieldAlias;

        public StatementFieldRef(string fieldName) : this(null, fieldName, null) {
        }

        public StatementFieldRef(string tableAlias, string fieldName, string fieldAlias) {
            this.tableAlias = string.IsNullOrWhiteSpace(tableAlias) ? null : tableAlias;
            this.fieldExpression = fieldName;
            this.fieldAlias = string.IsNullOrWhiteSpace(fieldAlias) ? fieldName : fieldAlias;
        }

        protected readonly static Regex REGEX_WILDCARD = new Regex(@"^(\w+\.)?(\w+|\*)(\s+\w+)?");
        protected readonly static Regex REGEX = new Regex(@"^(\w+\.)?(\w+)(\s+\w+)?");
        public static StatementFieldRef[] ParseFieldRefs(string selectClause, bool allowWildcard) {
            List<StatementFieldRef> fieldRefs = new List<StatementFieldRef>();
            string[] selectClauseParts = selectClause.SmartSplit(delimiter: ',', openBracket: '(', closeBracket: ')').Select(x => x.Trim()).ToArray();
            foreach (var selectClausePart in selectClauseParts) {
                Match match = (allowWildcard ? REGEX_WILDCARD : REGEX).Match(selectClausePart);
                if (!match.Success) throw new Exception($"Invalid field reference in SELECT clause '{selectClausePart}'");
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
