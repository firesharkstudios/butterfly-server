/* Copyright (C) Fireshark Studios, LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 * Written by Kent Johnson <kent@fireshark.com>, November 2017
 */

 using NLog;

namespace Fireshark.Butterfly.Dynamic {
    public class SingleValueDynamicParam : DynamicParam {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected object value = null;

        public SingleValueDynamicParam(string name) : base(name) {
        }

        public override void Clear() {
            if (this.value!=null) {
                this.Dirty = true;
                this.value = null;
            }
        }

        public override object GetValue() {
            return this.value;
        }

        public void SetValue(object value) {
            if (this.value != value) {
                logger.Debug($"Values.set():{this.name}={value}");
                this.Dirty = true;
                this.value = value;
            }
        }

    }
}
