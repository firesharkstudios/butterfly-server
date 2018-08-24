/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Data;

namespace Butterfly.Core.Database {
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
