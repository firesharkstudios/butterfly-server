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
using System.Data;

namespace Butterfly.Database {
    /// <summary>
    /// Defines a field definition for a <see cref="Table"/>
    /// </summary>
    public class TableFieldDef {
        public readonly string name;
        public readonly Type type;
        public readonly int maxLength;
        public readonly bool allowNull;
        public readonly bool isAutoIncrement;

        public TableFieldDef(string name, Type type, int maxLength, bool allowNull, bool isAutoIncrement) {
            this.name = name;
            this.type = type;
            this.maxLength = maxLength;
            this.allowNull = allowNull;
            this.isAutoIncrement = isAutoIncrement;
        }

        public static TableFieldDef FromDataColumn(DataColumn dataColumn) {
            return new TableFieldDef(dataColumn.ColumnName, dataColumn.DataType, dataColumn.MaxLength, dataColumn.AllowDBNull, dataColumn.AutoIncrement);
        }
    }
}
