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

using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core.Database.Dynamic {
    /// <summary>
    /// A <see cref="IDynamicParam"/> that may contain multiple values (like an array)
    /// </summary>
    public class MultiValueDynamicParam : BaseDynamicParam {
        protected readonly List<object> values = new List<object>();

        public MultiValueDynamicParam(string name) : base(name) {
        }

        public override void Clear() {
            if (this.values.Count > 0) {
                logger.Trace($"Clear()");
                this.SetDirty();
                this.values.Clear();
            }
        }

        public override object GetValue() {
            return this.values;
        }

        public void SetValues(ICollection<object> value) {
            var differences = value.Except(this.values);
            if (differences.Count() > 0) {
                logger.Trace($"Values.set():{this.name}={string.Join(",", value)}");
                this.SetDirty();
                this.values.Clear();
                this.values.AddRange(value);
            }
        }

    }
}
