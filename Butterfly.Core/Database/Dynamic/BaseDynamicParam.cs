/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using NLog;

namespace Butterfly.Core.Database.Dynamic {
    /// <summary>
    /// Base class for implementing dynamic params (see <see cref="IDynamicParam"/>)
    /// </summary>
    public abstract class BaseDynamicParam : IDynamicParam {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private bool dirty = false;

        public readonly string name;


        public BaseDynamicParam(string name) {
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
