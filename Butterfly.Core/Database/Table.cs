/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core.Database {

    /// <summary>
    /// Represents a table in an <see cref="IDatabase"/>
    /// </summary>
    public class Table {
        public Table(string name, TableFieldDef[] fieldDefs, TableIndex[] indexes) {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Name cannot be empty");
            if (fieldDefs==null || fieldDefs.Length==0) throw new ArgumentException($"FieldDefs cannot be empty for {name}");
            if (indexes==null || indexes.Length==0) throw new ArgumentException($"Indexes cannot be empty for {name}");

            this.Name = name;
            this.FieldDefs = fieldDefs;
            this.Indexes = indexes.OrderBy(x => x.IndexType).ToArray();
            this.AutoIncrementFieldName = this.FieldDefs.FirstOrDefault(x => x.isAutoIncrement)?.name;
        }

        public string Name {
            get;
            protected set;
        }

        public TableFieldDef[] FieldDefs {
            get;
            protected set;
        }

        public TableFieldDef FindFieldDef(string name) {
            return Array.Find(this.FieldDefs, x => x.name == name);
        }

        public TableIndex[] Indexes {
            get;
            protected set;
        }

        public string AutoIncrementFieldName {
            get;
            protected set;
        }
        
        protected readonly Dictionary<string, Func<string, object>> getDefaultValueByFieldName = new Dictionary<string, Func<string, object>>();
        internal void SetDefaultValue(string fieldName, Func<string, object> getDefaultValue) {
            this.getDefaultValueByFieldName[fieldName] = getDefaultValue;
        }
        public Dictionary<string, Func<string, object>> GetDefaultValueByFieldName => this.getDefaultValueByFieldName;

        protected readonly Dictionary<string, Func<string, object>> getOverrideValueByFieldName = new Dictionary<string, Func<string, object>>();
        internal void SetOverrideValue(string fieldName, Func<string, object> getOverrideValue) {
            this.getOverrideValueByFieldName[fieldName] = getOverrideValue;
        }
        public Dictionary<string, Func<string, object>> GetOverrideValueByFieldName => this.getOverrideValueByFieldName;

        internal TableIndex FindUniqueIndex(StatementEqualsRef[] setRefs) {
            var uniqueIndexes = this.Indexes.Where(x => x.IndexType != TableIndexType.Other && x.FieldNames.Length > 0);
            foreach (var uniqueIndex in uniqueIndexes) {
                bool hasAllFieldNames = uniqueIndex.FieldNames.All(x => Array.Find(setRefs, y => y.fieldName == x) != null);
                if (hasAllFieldNames) return uniqueIndex;
            }
            return null;
        }
    }
}
