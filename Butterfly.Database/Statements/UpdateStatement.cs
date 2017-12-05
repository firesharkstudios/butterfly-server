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

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Database {

    public class UpdateStatement : Statement {
        protected readonly static Regex STATEMENT_REGEX = new Regex(@"UPDATE\s+(\w+)\s+SET\s+(.+)\s+WHERE\s+(.*)", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        public readonly string fromClause;
        public readonly string setClause;
        public readonly string whereClause;

        /// <summary>
        /// Must only update a single row at a time. Must have a where clause that specifies the primary key fields. Where clause can only use ANDed equal conditions (like chat_id=2 AND chat_type='X")";
        /// </summary>
        /// <param name="database"></param>
        /// <param name="sql"></param>
        public UpdateStatement(IDatabase database, string sql) {
            this.SetSql(sql, "UPDATE @@tableName SET @@nameValues");

            // Confirm the sql is valid
            Match match = STATEMENT_REGEX.Match(this.Sql);
            if (!match.Success) throw new Exception($"Invalid sql '{this.Sql}'");

            // Extract each clause
            this.fromClause = match.Groups[1].Value.Trim();
            this.setClause = match.Groups[2].Value.Trim();
            this.whereClause = match.Groups[3].Value.Trim();

            // Parse the FROM clause
            this.TableRefs = TableRef.ParseTableRefs(database, this.fromClause);

            // Parse the SET clause
            this.SetRefs = DetermineEqualsRefs(database, setClause);
            this.WhereRefs = DetermineEqualsRefs(database, whereClause);
        }

        public EqualsRef[] SetRefs {
            get;
            protected set;
        }

        public EqualsRef[] WhereRefs {
            get;
            protected set;
        }

        public (string, Dict) GetExecutableSqlAndParams(Dict sourceParams) {
            Statement.ConfirmAllParamsUsed(this.Sql, sourceParams);
            return (this.Sql, sourceParams);
        }

    }

}
