/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using System.Text.RegularExpressions;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Core.Database {
    /// <summary>
    /// Internal class used to parse DELETE statements
    /// </summary>
    public class DeleteStatement : BaseStatement {
        protected readonly static Regex STATEMENT_REGEX = new Regex(@"DELETE\s+FROM\s+(?<fromClause>\w+)\s+WHERE\s+(?<whereClause>.*)", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        public readonly string fromClause;
        public readonly string whereClause;

        public DeleteStatement(IDatabase database, string sql) {
            //this.SetSql(sql, "DELETE FROM @@tableName");
            this.Sql = sql;

            if (this.IsSqlTableName) {
                this.fromClause = this.Sql;
                this.whereClause = null;
            }
            else {
                // Confirm the sql is valid
                Match match = STATEMENT_REGEX.Match(this.Sql);
                if (!match.Success) throw new Exception($"Invalid sql '{this.Sql}'");

                // Extract each clause
                this.fromClause = match.Groups["fromClause"].Value.Trim();
                this.whereClause = match.Groups["whereClause"].Value.Trim();
            }

            // Parse the FROM clause
            this.StatementFromRefs = StatementFromRef.ParseFromRefs(database, this.fromClause);
        }

        public (TableIndex, StatementEqualsRef[]) GetWhereIndexAndWhereRefs(IDatabase database, Dict statementParams) {
            StatementEqualsRef[] equalsRefs;
            if (string.IsNullOrEmpty(this.whereClause)) {
                if (this.StatementFromRefs.Length > 1) throw new Exception("Cannot auto fill where clause with more than one table in DELETE statement");
                equalsRefs = statementParams.Select(x => new StatementEqualsRef(this.StatementFromRefs[0].table.Name, x.Key, x.Key)).ToArray();
            }
            else {
                equalsRefs = BaseStatement.DetermineEqualsRefs(database, this.whereClause);
            }

            var uniqueIndex = this.StatementFromRefs[0].table.FindUniqueIndex(equalsRefs);
            if (uniqueIndex == null) throw new Exception($"Could not find unique index building WHERE clause of DELETE statement");

            if (equalsRefs.Length > uniqueIndex.FieldNames.Length) throw new Exception($"Unused fields auto filling WHERE clause of DELETE statement ({string.Join(",", equalsRefs.Select(x => x.fieldName))})");
            return (uniqueIndex, equalsRefs);
        }

        public (string, Dict) GetExecutableSqlAndParams(Dict sourceParams, StatementEqualsRef[] whereRefs) {
            string newWhereClause;
            if (string.IsNullOrEmpty(this.whereClause)) {
                newWhereClause = string.Join(" AND ", whereRefs.Select(x => $"{x.fieldName}=@{x.paramName}"));
            }
            else {
                newWhereClause = this.whereClause;
            }

            string sql = $"DELETE FROM {this.fromClause} WHERE {newWhereClause}";
            return (sql, sourceParams);
        }
    }
}
