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

namespace Butterfly.Database.Dynamic {
    /// <summary>
    /// A <see cref="IDynamicParam"/> that may only contain a single value
    /// </summary>
    public class SingleValueDynamicParam : BaseDynamicParam {
        protected object value = null;

        public SingleValueDynamicParam(string name) : base(name) {
        }

        public override void Clear() {
            if (this.value!=null) {
                this.SetDirty();
                this.value = null;
            }
        }

        public override object GetValue() {
            return this.value;
        }

        public void SetValue(object value) {
            if (this.value != value) {
                logger.Debug($"Values.set():{this.name}={value}");
                this.SetDirty();
                this.value = value;
            }
        }

    }
}
