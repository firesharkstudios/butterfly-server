/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Butterfly.Core.Util;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Core.Database {
    /// <summary>
    /// Internal class used to parse INSERT statements
    /// </summary>
    public class InsertStatement : BaseStatement {

        //public const string NAMES = "@@names";
        //public const string VALUES = "@@values";

        protected readonly static Regex STATEMENT_REGEX = new Regex(@"INSERT INTO (?<fromClause>.*) \((?<namesClause>.*)\) VALUES \((?<valuesClause>.*)\)", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        protected readonly string fromClause;
        protected readonly string namesClause;
        protected readonly string valuesClause;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="database"></param>
        /// <param name="sql">Can be a table name or full SQL. Full SQL can use @@names and @@values tokens to retrieve values from the record parameter.</param>
        public InsertStatement(IDatabase database, string sql) {
            this.Sql = sql;
            //this.SetSql(sql, "INSERT INTO @@tableName (@@names) VALUES (@@values)");

            if (this.IsSqlTableName) {
                this.fromClause = this.Sql;
                this.namesClause = null;
                this.valuesClause = null;
            }
            else {
                // Confirm the sql is valid
                Match match = STATEMENT_REGEX.Match(this.Sql);
                if (!match.Success) throw new Exception($"Invalid sql '{this.Sql}'");

                // Extract each clause
                this.fromClause = match.Groups["fromClause"].Value.Trim();
                this.namesClause = match.Groups["namesClause"].Value.Trim();
                this.valuesClause = match.Groups["valuesClause"].Value.Trim();
            }

            // Parse the FROM clause
            this.StatementFromRefs = StatementFromRef.ParseFromRefs(database, this.fromClause);
        }

        public static List<string> ParseNamesClause(string namesClause) {
            return namesClause.Split(',').Select(x => x.Trim()).ToList();
        }

        public static List<string> ParseValuesClause(string valuesClause) {
            return valuesClause.Split(',').Select(x => x.Trim()).ToList();
        }

        public (string, Dict) GetExecutableSqlAndParams(Dict statementParams, Dict defaultValues) {
            Dict executableParams = new Dict();
            executableParams.UpdateFrom(defaultValues);
            executableParams.UpdateFrom(statementParams);
            
            (List<string> names, List<string> values) = this.GetNamesAndValues(executableParams, defaultValues);

            string newNamesClause = string.Join(",", names);
            string newValuesClause = string.Join(",", values);

            string executableSql = $"INSERT INTO {this.StatementFromRefs[0].table.Name} ({newNamesClause}) VALUES ({newValuesClause})";
            BaseStatement.ConfirmAllParamsUsed(executableSql, executableParams);

            return (executableSql, executableParams);
        }

        public StatementEqualsRef[] GetInsertRefs(Dict executableParams) {
            (IList<string> names, IList<string> values) = this.GetNamesAndValues(executableParams);

            List<StatementEqualsRef> result = new List<StatementEqualsRef>();
            for (int i = 0; i < names.Count; i++) {
                result.Add(new StatementEqualsRef(this.StatementFromRefs[0].table.Name, names[i], values[i].Substring(1)));
            }
            return result.ToArray();
        }

        protected (List<string>, List<string>) GetNamesAndValues(Dict executableParams, Dict defaultValues = null) {
            List<string> names;
            List<string> values;
            if (string.IsNullOrEmpty(this.namesClause) && string.IsNullOrEmpty(this.valuesClause)) {
                names = executableParams.Keys.ToList();
                values = executableParams.Keys.Select(x => $"@{x}").ToList();
            }
            else {
                names = ParseNamesClause(this.namesClause);
                values = ParseValuesClause(this.valuesClause);
                if (defaultValues!=null) {
                    foreach ((string key, object value) in defaultValues) {
                        if (!names.Contains(key)) {
                            names.Add(key);
                            values.Add($"@{key}");
                        }
                    }
                }
            }
            return (names, values);
        }

    }
}
