/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using System.Text.RegularExpressions;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Core.Database {
    /// <summary>
    /// Internal class used to parse UPDATE statements
    /// </summary>
    public class UpdateStatement : BaseStatement {
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
            this.Sql = sql;

            if (this.IsSqlTableName) {
                this.fromClause = this.Sql;
                this.setClause = null;
                this.whereClause = null;
            }
            else {
                // Confirm the sql is valid
                Match match = STATEMENT_REGEX.Match(this.Sql);
                if (!match.Success) throw new Exception($"Invalid sql '{this.Sql}'");

                // Extract each clause
                this.fromClause = match.Groups[1].Value.Trim();
                this.setClause = match.Groups[2].Value.Trim();
                this.whereClause = match.Groups[3].Value.Trim();
            }

            // Parse the FROM clause
            this.StatementFromRefs = StatementFromRef.ParseFromRefs(database, this.fromClause);
        }

        public (TableIndex, StatementEqualsRef[], StatementEqualsRef[]) GetWhereIndexSetRefsAndWhereRefs(IDatabase database, Dict statementParams) {
            if (string.IsNullOrEmpty(this.setClause) && string.IsNullOrEmpty(this.whereClause)) {
                if (this.StatementFromRefs.Length > 1) throw new Exception("Cannot auto fill set clause and where clause with more than one table in update statement");

                var statementEqualsRefs = statementParams.Select(x => new StatementEqualsRef(this.StatementFromRefs[0].table.Name, x.Key, x.Key));
                var uniqueIndex = this.StatementFromRefs[0].table.FindUniqueIndex(statementEqualsRefs.ToArray());
                if (uniqueIndex==null) throw new Exception($"Could not find unique index building WHERE clause of UPDATE statement");

                var whereRefs = statementEqualsRefs.Where(x => uniqueIndex.FieldNames.Contains(x.fieldName)).ToArray();
                var setRefs = statementEqualsRefs.Where(x => !uniqueIndex.FieldNames.Contains(x.fieldName)).ToArray();

                return (uniqueIndex, setRefs, whereRefs);
            }
            else {
                var setRefs = BaseStatement.DetermineEqualsRefs(database, this.setClause);
                var whereRefs = BaseStatement.DetermineEqualsRefs(database, this.whereClause);
                var uniqueIndex = this.StatementFromRefs[0].table.FindUniqueIndex(whereRefs);

                return (uniqueIndex, setRefs, whereRefs);
            }
        }

        public (string, Dict) GetExecutableSqlAndParams(Dict sourceParams, StatementEqualsRef[] setRefs, StatementEqualsRef[] whereRefs) {
            //BaseStatement.ConfirmAllParamsUsed(this.Sql, sourceParams);
            string newSetClause;
            string newWhereClause;
            if (string.IsNullOrEmpty(this.setClause) && string.IsNullOrEmpty(this.whereClause)) {
                newSetClause = string.Join(",", setRefs.Select(x => $"{x.fieldName}=@{x.paramName}"));
                newWhereClause = string.Join(" AND ", whereRefs.Select(x => $"{x.fieldName}=@{x.paramName}"));
            }
            else {
                newSetClause = this.setClause;
                newWhereClause = this.whereClause;
            }

            string sql = $"UPDATE {this.fromClause} SET {newSetClause} WHERE {newWhereClause}";
            return (sql, sourceParams);
        }

    }

}
