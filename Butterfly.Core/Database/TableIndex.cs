/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

namespace Butterfly.Core.Database {

    public enum TableIndexType {
        Primary,
        Unique,
        Other
    }

    /// <summary>
    /// Defines an index for a <see cref="Table"/>
    /// </summary>
    public class TableIndex {
        public TableIndex(TableIndexType indexType, string[] fieldNames) {
            this.IndexType = indexType;
            this.FieldNames = fieldNames;
        }

        public TableIndexType IndexType {
            get;
            protected set;
        }

        public string[] FieldNames {
            get;
            protected set;
        }
    }

}
