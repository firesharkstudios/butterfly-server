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

namespace Butterfly.Database {
    public class InsertStatement : Statement {

        public const string NAMES = "@@names";
        public const string VALUES = "@@values";

        protected readonly static Regex STATEMENT_REGEX = new Regex(@"INSERT INTO (?<fromClause>.*) \((?<namesClause>.*)\) VALUES \((?<valuesClause>.*)\)", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        protected readonly string fromClause;
        protected readonly string namesClause;
        protected readonly string valuesClause;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="database"></param>
        /// <param name="sourceSql">Can be a table name or full SQL. Full SQL can use @@names and @@values tokens to retrieve values from the record parameter.</param>
        /// <param name="sourceParams"></param>
        public InsertStatement(IDatabase database, string sql) {
            this.SetSql(sql, "INSERT INTO @@tableName (@@names) VALUES (@@values)");

            // Confirm the sql is valid
            Match match = STATEMENT_REGEX.Match(this.Sql);
            if (!match.Success) throw new Exception($"Invalid sql '{this.Sql}'");

            // Extract each clause
            this.fromClause = match.Groups["fromClause"].Value.Trim();
            this.namesClause = match.Groups["namesClause"].Value.Trim();
            this.valuesClause = match.Groups["valuesClause"].Value.Trim();

            // Parse the FROM clause
            this.TableRefs = TableRef.ParseTableRefs(database, this.fromClause);
        }

        public static List<string> ParseNamesClause(string namesClause) {
            return namesClause.Split(',').Select(x => x.Trim()).ToList();
        }

        public static List<string> ParseValuesClause(string valuesClause) {
            return valuesClause.Split(',').Select(x => x.Trim()).ToList();
        }

        public (string, Dict) GetExecutableSqlAndParams(Dict statementParamsDict) {
            string newNamesClause = this.namesClause;
            string newValuesClause = this.valuesClause;

            if (newNamesClause==NAMES && newValuesClause == VALUES) {
                newNamesClause = string.Join(",", statementParamsDict.Keys);

                var paramNames = statementParamsDict.Keys.Select(x => $"@{x}");
                newValuesClause = string.Join(",", paramNames);
            }
            else {
                List<string> names = ParseNamesClause(newNamesClause);
                List<string> values = ParseValuesClause(newValuesClause);
                foreach ((string key, object value) in statementParamsDict) {
                    if (!names.Contains(key)) {
                        names.Add(key);
                        values.Add($"@{key}");
                    }
                }
                newNamesClause = string.Join(",", names);
                newValuesClause = string.Join(",", values);
            }

            return ($"INSERT INTO {this.TableRefs[0].table.Name} ({newNamesClause}) VALUES ({newValuesClause})", statementParamsDict);
        }
    }
}
