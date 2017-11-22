/* Copyright (C) Fireshark Studios, LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 * Written by Kent Johnson <kent@fireshark.com>, November 2017
 */

 using System.Collections.Generic;
using System.Linq;

using NLog;

namespace Fireshark.Butterfly.Dynamic {
    public class MultiValueDynamicParam : DynamicParam {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly List<object> values = new List<object>();

        public MultiValueDynamicParam(string name) : base(name) {
        }

        public override void Clear() {
            if (this.values.Count > 0) {
                this.Dirty = true;
                this.values.Clear();
            }
        }

        public override object GetValue() {
            return this.values;
        }

        public void SetValues(ICollection<object> value) {
            var differences = value.Except(this.values);
            if (differences.Count() > 0) {
                logger.Debug($"Values.set():{this.name}={string.Join(",", value)}");
                this.Dirty = true;
                this.values.Clear();
                this.values.AddRange(value);
            }
        }

    }
}
