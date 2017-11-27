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

namespace Butterfly.Database {

    public abstract class Table {

        public Table(string name, FieldDef[] fieldDefs, Index primaryIndex) {
            this.Name = name;
            this.FieldDefs = fieldDefs;
            this.PrimaryIndex = primaryIndex;
            this.AutoIncrementFieldName = this.FieldDefs.FirstOrDefault(x => x.isAutoIncrement)?.name;
        }

        public string Name {
            get;
            protected set;
        }

        public FieldDef[] FieldDefs {
            get;
            protected set;
        }

        public FieldDef FindFieldDef(string name) {
            return Array.Find(this.FieldDefs, x => x.name == name);
        }

        public Index PrimaryIndex {
            get;
            protected set;
        }

        public string AutoIncrementFieldName {
            get;
            protected set;
        }
        
        protected readonly Dictionary<string, Func<object>> getDefaultValueByFieldName = new Dictionary<string, Func<object>>();
        internal void SetDefaultValue(string fieldName, Func<object> getDefaultValue) {
            this.getDefaultValueByFieldName[fieldName] = getDefaultValue;
        }

        public Dictionary<string, Func<object>> GetDefaultValueByFieldName => this.getDefaultValueByFieldName;

    }
}
