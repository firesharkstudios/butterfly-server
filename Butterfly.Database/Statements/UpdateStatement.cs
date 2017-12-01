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
        /// <param name="sourceSql"></param>
        /// <param name="sourceParams"></param>
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
            this.SetRefs = DetermineSetRefs(database, setClause);
        }

        // x.id=@id
        protected readonly static Regex FIRST_SET_REF_REGEX = new Regex(@"^(?<tableAliasWithDot>\w+\.)?(?<fieldName>\w+)\s*=\s*\@(?<paramName>\w+)");

        // AND x.type=@type
        protected readonly static Regex NEXT_SET_REF_REGEX = new Regex(@"\s+,\s+(?<tableAliasWithDot>\w+\.)?(?<fieldName>\w+)\s*=\s*\@(?<paramName>\w+)");

        public static SqlEqualsRef[] DetermineSetRefs(IDatabase database, string whereClause) {
            var firstMatch = FIRST_SET_REF_REGEX.Match(whereClause);
            if (!firstMatch.Success) throw new Exception($"Invalid where clause '{whereClause}'");

            List<SqlEqualsRef> setRefs = new List<SqlEqualsRef>();
            setRefs.Add(new SqlEqualsRef(firstMatch.Groups["tableAliasWithDot"].Value.Trim(), firstMatch.Groups["fieldName"].Value.Trim(), firstMatch.Groups["paramName"].Value.Trim()));

            var nextMatches = NEXT_SET_REF_REGEX.Matches(whereClause);
            foreach (Match nextMatch in nextMatches) {
                setRefs.Add(new SqlEqualsRef(nextMatch.Groups["tableAliasWithDot"].Value.Trim(), nextMatch.Groups["fieldName"].Value.Trim(), nextMatch.Groups["paramName"].Value.Trim()));
            }
            return setRefs.ToArray();
        }

        public SqlEqualsRef[] SetRefs {
            get;
            protected set;
        }

        public (string, Dict) GetExecutableSqlAndParams(Dict sourceParams) {
            return (this.Sql, sourceParams);
        }
    }

}
