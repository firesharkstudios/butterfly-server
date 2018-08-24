/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

namespace Butterfly.Core.Database.Dynamic {
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
