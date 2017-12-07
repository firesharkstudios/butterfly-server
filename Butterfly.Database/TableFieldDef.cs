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
