/* Copyright (C) Fireshark Studios, LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 * Written by Kent Johnson <kent@fireshark.com>, November 2017
 */
 
 using Butterfly.Database;

namespace Fireshark.Butterfly.Dynamic {
    public abstract class DynamicParam : IDynamicParam {
        public readonly string name;

        public DynamicParam(string name) {
            this.name = name;
        }

        public bool Dirty {
            get;
            protected set;
        }

        public void ResetDirty() {
            this.Dirty = false;
        }

        public abstract void Clear();

        public abstract object GetValue();

    }
}
