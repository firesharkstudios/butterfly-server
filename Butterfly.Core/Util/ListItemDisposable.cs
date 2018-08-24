/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using NLog;
using System;
using System.Collections.Generic;

namespace Butterfly.Core.Util {
    public class ListItemDisposable<T> : IDisposable {

        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly List<T> list;
        protected readonly T item;

        public ListItemDisposable(List<T> list, T item) {
            logger.Debug("ListItemDisposable()");
            this.list = list;
            this.item = item;
            this.list.Add(item);
        }

        public void Dispose() {
            logger.Debug("Dispose()");
            this.list.Remove(this.item);
        }
    }
}
