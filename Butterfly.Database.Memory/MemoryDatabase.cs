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
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using NLog;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Database.Memory {

    /// <inheritdoc/>
    public class MemoryDatabase : BaseDatabase {

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public MemoryDatabase() : base(null) {
        }

        protected override void LoadSchema() {
        }

        protected override Table LoadTableSchema(string tableName) {
            return null;
        }

        protected override BaseTransaction CreateTransaction() {
            return new MemoryTransaction(this);
        }

        protected override Task<Dict[]> DoSelectRowsAsync(string executableSql, Dict executableParams) {
            SelectStatement executableStatement = new SelectStatement(this, executableSql);
            if (executableStatement.TableRefs.Length > 1) throw new Exception("MemoryTable does not support joins");
            if (!(executableStatement.TableRefs[0].table is MemoryTable memoryTable)) throw new Exception("Table is not a MemoryTable");

            string evaluatedWhereClause = EvaluateWhereClause(executableStatement.whereClause, executableParams, executableStatement.TableRefs);
            DataRow[] dataRows = memoryTable.DataTable.Select(evaluatedWhereClause, null, DataViewRowState.OriginalRows);
            List<Dict> rows = new List<Dict>();
            foreach (var dataRow in dataRows) {
                Dict row = new Dict();
                foreach (var fieldRef in executableStatement.FieldRefs) {
                    row[fieldRef.fieldAlias] = dataRow[fieldRef.fieldAlias, DataRowVersion.Original];
                }
                rows.Add(row);
            }
            return Task.FromResult(rows.ToArray());
        }

        protected static readonly Regex SIMPLE_REPLACE = new Regex(@"(?<tableAliasWithDot>\w+\.)?(?<fieldName>\w+)\s*(?<op>=|<>|!=|>|<)\s*(?<param>\@\w+)");
        protected static readonly Regex IN_REPLACE = new Regex(@"(?<tableAliasWithDot>\w+\.)?(?<fieldName>\w+)\s+(?<op>IN|NOT\s+IN)\s+\((?<param>[^\)]+)\)", RegexOptions.IgnoreCase);

        public override bool CanJoin => false;

        public static string EvaluateWhereClause(string whereClause, Dict sqlParams, StatementTableRef[] tableRefs) {
            string newWhereClause = whereClause;
            newWhereClause = EvaluateWhereClauseReplace(newWhereClause, SIMPLE_REPLACE, sqlParams, tableRefs, op => {
                if (op == "!=") return "<>";
                else return op;
            });
            newWhereClause = EvaluateWhereClauseReplace(newWhereClause, IN_REPLACE, sqlParams, tableRefs);
            return newWhereClause;
        }

        protected static string EvaluateWhereClauseReplace(string sql, Regex regex, Dict sqlParams, StatementTableRef[] tableRefs, Func<string, string> remapOp = null) {
            StringBuilder sb = new StringBuilder();
            int lastIndex = 0;
            foreach (Match match in regex.Matches(sql)) {
                sb.Append(sql.Substring(lastIndex, match.Groups["op"].Index - lastIndex));
                string op = match.Groups["op"].Value;
                sb.Append(remapOp !=null ? remapOp(op) : op);
                lastIndex = match.Groups["op"].Index + match.Groups["op"].Length;

                sb.Append(sql.Substring(lastIndex, match.Groups["param"].Index - lastIndex));

                // Get table ref
                string tableAlias = match.Groups["tableAliasWithDot"].Value.Replace(".", "");
                StatementTableRef tableRef;
                if (string.IsNullOrEmpty(tableAlias)) {
                    if (tableRefs.Length > 1) throw new Exception("SELECT statements with more than one table reference must use table aliases for all where clause fields");
                    tableRef = tableRefs[0];
                }
                else {
                    tableRef = Array.Find(tableRefs, x => x.tableAlias==tableAlias);
                }

                // Get field defs
                string fieldName = match.Groups["fieldName"].Value;
                if (fieldName.Equals("NOT", StringComparison.OrdinalIgnoreCase)) {
                    lastIndex = match.Groups["param"].Index;
                }
                else {
                    TableFieldDef fieldDef = tableRef.table.FindFieldDef(fieldName);

                    // Get evaluated value
                    var paramNames = match.Groups["param"].Value.Split(',').Select(x => x.Replace("@", "").Trim());
                    bool isFirst = true;
                    foreach (var paramName in paramNames) {
                        object replacementValue = sqlParams[paramName];
                        string evaluatedValue;
                        if (fieldDef.type == typeof(string)) {
                            evaluatedValue = $"'{replacementValue}'";
                        }
                        else if (fieldDef.type == typeof(DateTime)) {
                            evaluatedValue = $"#{replacementValue}#";
                        }
                        else {
                            evaluatedValue = $"{replacementValue}";
                        }
                        if (isFirst) isFirst = false;
                        else sb.Append(',');
                        sb.Append(evaluatedValue);
                    }
                    lastIndex = match.Groups["param"].Index + match.Groups["param"].Length;
                }
            }
            sb.Append(sql.Substring(lastIndex));
            return sb.ToString();
        }
    }
}
