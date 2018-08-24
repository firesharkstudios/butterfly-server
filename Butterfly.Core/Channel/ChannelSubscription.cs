/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Threading.Tasks;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Core.Channel {
    /// <summary>
    /// Represents a subscription to a specific channel
    /// </summary>
    public class ChannelSubscription {
        public readonly string channelKey;
        public readonly Func<Dict, Channel, IDisposable> handle;
        public readonly Func<Dict, Channel, Task<IDisposable>> handleAsync;

        public ChannelSubscription(string channelKey, Func<Dict, Channel, IDisposable> handle) {
            this.channelKey = channelKey;
            this.handle = handle;
            this.handleAsync = null;
        }

        public ChannelSubscription(string key, Func<Dict, Channel, Task<IDisposable>> handleAsync) {
            this.channelKey = key;
            this.handle = null;
            this.handleAsync = handleAsync;
        }
    }
}
