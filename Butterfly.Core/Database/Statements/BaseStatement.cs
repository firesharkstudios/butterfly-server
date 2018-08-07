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

using Butterfly.Core.Util;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Core.Database {
    /// <summary>
    /// Base class for parsing SQL statements
    /// </summary>
    public abstract class BaseStatement {
        protected bool IsSqlTableName {
            get {
                return !this.Sql.Contains(" ");
            }
        }

        public string Sql {
            get;
            protected set;
        }

        private StatementFromRef[] statementFromRefs;
        public StatementFromRef[] StatementFromRefs {
            get {
                return this.statementFromRefs;
            }
            protected set {
                this.statementFromRefs = value;
                foreach (var statementFromRef in value) {
                    this.fromTableNames.Add(statementFromRef.table.Name);
                }
            }
        }

        public StatementFromRef FindStatementFromRefByTableAlias(string tableAlias) {
            return Array.Find(this.StatementFromRefs, x => x.tableAlias == tableAlias);
        }

        protected readonly HashSet<string> fromTableNames = new HashSet<string>();
        public bool HasTableInFrom(string tableName) {
            return this.fromTableNames.Contains(tableName);
        }

        public static void ConfirmAllParamsUsed(string sql, Dict statementParams) {
            foreach (var key in statementParams.Keys) {
                string paramName = $"@{key}";
                if (!sql.Contains(paramName)) throw new Exception($"Unused parameter '{key}'");
            }
        }

        public Dict ConvertParamsToDict(dynamic statementParams, bool allowKeyValueAsSourceParams = true) {
            // If statementParams is null, return empty dictionary
            if (statementParams == null) {
                return new Dict();
            }

            // If statementParams is already a dictionary, return the dictionary
            else if (statementParams is Dict d) {
                return new Dict(d);
            }

            // If statementParams is a single string, assume it is a primary key value
            else if (statementParams is string keyValue) {
                if (!allowKeyValueAsSourceParams) throw new Exception("Statement doesn't allow passing single key value as source params");
                if (this.StatementFromRefs.Length != 1) throw new Exception("Statement must have exactly one table to pass single string value as where condition");
                return BaseDatabase.ParseKeyValue(keyValue, this.StatementFromRefs[0].table.Indexes[0].FieldNames);
            }

            // Otherwise, convert statementParams to a dictionary
            else {
                return DynamicX.ToDictionary(statementParams);
            }
        }

        // x.id=@id
        protected readonly static Regex EQUALS_REF_REGEX = new Regex(@"^(?<tableAliasWithDot>\w+\.)?(?<fieldName>\w+)\s*=\s*\@(?<paramName>\w+)");

        internal static StatementEqualsRef[] DetermineEqualsRefs(IDatabase database, string clause) {
            List<StatementEqualsRef> setRefs = new List<StatementEqualsRef>();
            var matches = EQUALS_REF_REGEX.Matches(clause);
            foreach (Match nextMatch in matches) {
                setRefs.Add(new StatementEqualsRef(nextMatch.Groups["tableAliasWithDot"].Value.Trim(), nextMatch.Groups["fieldName"].Value.Trim(), nextMatch.Groups["paramName"].Value.Trim()));
            }
            return setRefs.ToArray();
        }

        public static Dict RemapStatementParamsToFieldValues(Dict statementParamsDict, StatementEqualsRef[] equalsRefs) {
            Dict result = new Dict();
            foreach ((string statementParamName, object statementParamValue) in statementParamsDict) {
                StatementEqualsRef equalsRef = Array.Find(equalsRefs, x => x.paramName == statementParamName);
                if (equalsRef != null) {
                    result[equalsRef.fieldName] = statementParamValue;
                }
            }
            return result;
        }
    }

    public enum JoinType {
        None,
        Inner,
        Left,
        Right
    }

    /// <summary>
    /// Internal class representing a SQL table reference like "table_name table_alias"
    /// </summary>
    public class StatementFromRef {
        public readonly JoinType joinType;
        public readonly Table table;
        public readonly string tableAlias;
        public readonly string joinCondition;

        /*
        public StatementFromRef(Table table) : this(table, table.Name) {
        }
        */

        public StatementFromRef(JoinType joinType, Table table, string tableAlias, string joinCondition) {
            this.joinType = joinType;
            this.table = table;
            this.tableAlias = string.IsNullOrWhiteSpace(tableAlias) ? null : tableAlias;
            this.joinCondition = joinCondition;
        }

        // chat_participant cp
        protected readonly static Regex FIRST_TABLE_REGEX = new Regex(@"^(?<tableName>\w+)(?<tableAlias>\s+\w+)?");

        // INNER JOIN user u ON cp.user_id=U.id
        protected readonly static Regex JOIN_REGEX = new Regex(@"\s+(?<joinType>INNER|LEFT|RIGHT)\s+JOIN\s+(?<tableName>\w+)(?<tableAlias>\s+\w+)?\s+ON\s+", RegexOptions.IgnoreCase);

        public static StatementFromRef[] ParseFromRefs(IDatabase database, string fromClause) {
            var match = FIRST_TABLE_REGEX.Match(fromClause);
            if (!match.Success) throw new Exception($"Invalid from clause '{fromClause}'");

            List<StatementFromRef> statementFromRefs = new List<StatementFromRef>();
            string firstTableName = match.Groups["tableName"].Value.Trim();
            if (!database.Tables.TryGetValue(firstTableName, out Table firstTable)) throw new Exception($"Invalid table name '{firstTableName}'");
            statementFromRefs.Add(new StatementFromRef(JoinType.None, firstTable, match.Groups["tableAlias"].Value.Trim(), null));

            var joinMatches = JOIN_REGEX.Matches(fromClause, match.Index + match.Length);
            for (int i=0; i<joinMatches.Count; i++) {
                if (Enum.TryParse(joinMatches[i].Groups["joinType"].Value.Trim(), true, out JoinType joinType)) {
                    if (joinType == JoinType.Right) throw new Exception("Right joins not supported");

                    string joinTableName = joinMatches[i].Groups["tableName"].Value.Trim();
                    if (!database.Tables.TryGetValue(joinTableName, out Table joinTable)) throw new Exception($"Invalid table name '{joinTableName}'");

                    string joinTableAlias = joinMatches[i].Groups["tableAlias"].Value.Trim();

                    int beginIndex = joinMatches[i].Index + joinMatches[i].Length;
                    int endIndex = i < joinMatches.Count - 1 ? joinMatches[i + 1].Index : fromClause.Length;

                    string joinCondition = fromClause.Substring(beginIndex, endIndex - beginIndex);

                    statementFromRefs.Add(new StatementFromRef(joinType, joinTable, joinTableAlias, joinCondition));
                }
            }
            return statementFromRefs.ToArray();
        }

        public override string ToString() {
            return $"{{table={this.table.Name},alias={this.tableAlias}}}";
        }
    }

    /// <summary>
    /// Internal class representing a SQL equality reference like "table_alias.field_name=@param_name"
    /// </summary>
    public class StatementEqualsRef {
        public readonly string tableName;
        public readonly string fieldName;
        public readonly string paramName;

        public StatementEqualsRef(string tableName, string fieldName, string paramName) {
            this.tableName = string.IsNullOrEmpty(tableName) ? null : tableName;
            this.fieldName = fieldName;
            this.paramName = paramName;
        }

        public static Dict GetMatchingParams(StatementEqualsRef[] equalsRefs, Dict sourceParams) {
            Dict result = new Dict();
            foreach (var keyValuePair in sourceParams) {
                StatementEqualsRef whereRef = Array.Find(equalsRefs, x => x.paramName == keyValuePair.Key);
                if (whereRef != null) {
                    result[whereRef.fieldName] = keyValuePair.Value;
                }
            }
            return result;
        }
    }
}
