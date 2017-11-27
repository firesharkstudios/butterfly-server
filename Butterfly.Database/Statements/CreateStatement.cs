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
        protected readonly static Regex STATEMENT_REGEX = new Regex(@"CREATE\s+TABLE\s+(?<tableName>\w+)\s*\(\s*(?<createTableDefs>[\s\S]*)\s*\)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        protected readonly static Regex FIELD_REGEX = new Regex(@"^\s*(?<fieldName>\w+)\s+(?<fieldType>[\w\(\)]+)(?<not>\s*NOT)?(?<null>\s*NULL)?(?<autoIncrement>\s*AUTO_INCREMENT)?\s*[,$]", RegexOptions.IgnoreCase);
        protected readonly static Regex PRIMARY_KEY_REGEX = new Regex(@"^\s*PRIMARY\s+KEY\s*\((?<fields>.*)\)\s*(?:,|$)", RegexOptions.IgnoreCase);
        protected readonly static Regex INDEX_REGEX = new Regex(@"^\s*(?<unique>UNIQUE\s+)?INDEX\s*(?<name>\w+)\s*\((?<fields>.*)\)\s*(?:,|$)", RegexOptions.IgnoreCase);

        public CreateStatement(string sql) {
            Match match = STATEMENT_REGEX.Match(sql.Trim());
            if (!match.Success) throw new Exception($"Invalid sql '{this.Sql}'");

            this.Sql = sql;

            // Extract field names
            this.TableName = match.Groups["tableName"].Value;

            string createTableDefs = match.Groups["createTableDefs"].Value.Replace("\r", " ").Replace("\n", " ");

            List<FieldDef> fieldDefs = new List<FieldDef>();
            int lastIndex = 0;
            while (lastIndex<createTableDefs.Length) {
                string substring = createTableDefs.Substring(lastIndex);
                Match primaryKeyMatch = PRIMARY_KEY_REGEX.Match(substring);
                if (primaryKeyMatch.Success) {
                    string[] primaryKeyFieldNames = primaryKeyMatch.Groups["fields"].Value.Split(',').Select(x => x.Trim()).ToArray();
                    this.PrimaryIndex = new Index("Primary", primaryKeyFieldNames);
                    lastIndex += primaryKeyMatch.Length;
                }
                else {
                    Match indexMatch = INDEX_REGEX.Match(substring);
                    if (indexMatch.Success) {
                        lastIndex += indexMatch.Length;
                    }
                    else {
                        Match fieldMatch = FIELD_REGEX.Match(substring);
                        if (fieldMatch.Success) {
                            string fieldName = fieldMatch.Groups["fieldName"].Value;
                            string fieldTypeText = fieldMatch.Groups["fieldType"].Value;
                            (Type fieldType, int maxLength) = Database.ConvertMySqlType(fieldTypeText);
                            bool notNull = fieldMatch.Groups["not"].Success && fieldMatch.Groups["null"].Success;
                            bool autoIncrement = fieldMatch.Groups["autoIncrement"].Success;
                            fieldDefs.Add(new FieldDef(fieldName, fieldType, maxLength, !notNull, autoIncrement));
                            lastIndex += fieldMatch.Length;
                        }
                        else {
                            throw new Exception($"Could not parse '{substring}' in {this.Sql}");
                        }
                    }
                }
            }
            this.FieldDefs = fieldDefs.ToArray();
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
