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

using NLog;

namespace Butterfly.Database.Dynamic {
    public abstract class DynamicParam : IDynamicParam {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private bool dirty = false;

        public readonly string name;


        public DynamicParam(string name) {
            this.name = name;
        }

        public bool Dirty => this.dirty;

        protected void SetDirty() {
            //logger.Debug($"SetDirty():name={name}");
            this.dirty = true;
        }

        public void ResetDirty() {
            //logger.Debug($"ResetDirty():name={name}");
            this.dirty = false;
        }

        public abstract void Clear();

        public abstract object GetValue();

    }
}
