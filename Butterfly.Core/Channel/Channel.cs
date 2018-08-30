/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;

using NLog;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Core.Channel {
    /// <summary>
    /// Represents a channel that clients can subscribe to
    /// </summary>
    public class Channel : IDisposable {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly IChannelConnection connection;
        protected readonly string channelKey;
        protected readonly Dict vars;

        public Channel(IChannelConnection connection, string channelKey, Dict vars) {
            this.connection = connection;
            this.channelKey = channelKey;
            this.vars = vars;
        }

        public IChannelConnection Connection => this.connection;

        public string ChannelKey => this.channelKey;

        public Dict Vars => this.vars;

        protected readonly List<IDisposable> disposables = new List<IDisposable>();

        public void Attach(IDisposable disposable) {
            this.disposables.Add(disposable);
        }

        public void Queue(object data) {
            this.Queue("DATA-EVENT-TRANSACTION", data);
        }

        public void Queue(string messageType, object data) {
            this.connection.QueueMessage(channelKey: this.channelKey, messageType: messageType, data: data);
        }

        public void Dispose() {
            logger.Debug($"Dispose():ChannelKey={this.ChannelKey}");
            foreach (var disposable in this.disposables) {
                try {
                    disposable.Dispose();
                }
                catch (Exception e) {
                    logger.Error(e);
                }
            }
            this.disposables.Clear();
        }

    }
}
