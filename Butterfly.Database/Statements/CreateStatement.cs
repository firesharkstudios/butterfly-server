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

namespace Butterfly.Database {
    public class CreateStatement : Statement {
        protected readonly static Regex STATEMENT_REGEX = new Regex(@"CREATE\s+TABLE\s+(?<tableName>\w+)\s*\(\s*(?<fieldList>[\s\S]*)\s+PRIMARY\s+KEY\s*\((?<primaryKey>.*)\)\s*\)", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        public CreateStatement(string sql) {
            Match match = STATEMENT_REGEX.Match(sql.Trim());
            if (!match.Success) throw new Exception($"Invalid sql '{this.Sql}'");

            this.Sql = sql;

            // Extract field names
            this.TableName = match.Groups["tableName"].Value;

            // Extract field definitions
            List<FieldDef> fieldDefs = new List<FieldDef>();
            string fieldList = match.Groups["fieldList"].Value;
            var fieldListItems = fieldList.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x));
            foreach (var fieldListItem in fieldListItems) {
                string[] fieldParts = fieldListItem.Split(' ');
                string fieldName = fieldParts[0];
                (Type fieldType, int maxLength) = Database.ConvertMySqlType(fieldParts[1]);
                bool notNull = fieldListItem.ToUpper().Contains("NOT NULL");
                bool autoIncrement = fieldListItem.EndsWith("AUTO_INCREMENT", StringComparison.OrdinalIgnoreCase);
                fieldDefs.Add(new FieldDef(fieldName, fieldType, maxLength, !notNull, autoIncrement));
            }
            this.FieldDefs = fieldDefs.ToArray();

            // Extract primary key field names
            string[] primaryKeyFieldNames = match.Groups["primaryKey"].Value.Split(',').Select(x => x.Trim()).ToArray();
            this.PrimaryIndex = new Index("Primary", primaryKeyFieldNames);
        }

        public string TableName {
            get;
            protected set;
        }

        public FieldDef[] FieldDefs {
            get;
            protected set;
        }
        
        public Index PrimaryIndex {
            get;
            protected set;
        }
    }
}
