/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

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
