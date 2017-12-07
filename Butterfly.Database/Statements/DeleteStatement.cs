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
using System.Text.RegularExpressions;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Database {
    /// <summary>
    /// Internal class used to parse DELETE statements
    /// </summary>
    public class DeleteStatement : BaseStatement {
        protected readonly static Regex STATEMENT_REGEX = new Regex(@"DELETE\s+FROM\s+(?<fromClause>\w+)\s+WHERE\s+(?<whereClause>.*)", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        public readonly string fromClause;
        public readonly string whereClause;

        public DeleteStatement(IDatabase database, string sql) {
            this.SetSql(sql, "DELETE FROM @@tableName");

            // Confirm the sql is valid
            Match match = STATEMENT_REGEX.Match(this.Sql);
            if (!match.Success) throw new Exception($"Invalid sql '{this.Sql}'");

            // Extract each clause
            this.fromClause = match.Groups["fromClause"].Value.Trim();
            this.whereClause = match.Groups["whereClause"].Value.Trim();

            // Parse the FROM clause
            this.TableRefs = StatementTableRef.ParseTableRefs(database, this.fromClause);
            this.WhereRefs = DetermineEqualsRefs(database, whereClause);
        }

        public StatementEqualsRef[] WhereRefs {
            get;
            protected set;
        }

        public (string, Dict) GetExecutableSqlAndParams(Dict sourceParams) {
            BaseStatement.ConfirmAllParamsUsed(this.Sql, sourceParams);
            return (this.Sql, sourceParams);
        }
    }
}
