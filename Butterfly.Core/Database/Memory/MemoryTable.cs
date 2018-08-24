/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Data;

namespace Butterfly.Core.Database.Memory {

    public class MemoryTable : Table {
        public MemoryTable(DataTable dataTable, TableFieldDef[] fieldDefs, TableIndex[] indexes) : base(dataTable.TableName, fieldDefs, indexes) {
            this.DataTable = dataTable;
        }

        public DataTable DataTable {
            get;
            protected set;
        }
    }
}
