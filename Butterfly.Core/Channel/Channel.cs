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

        protected readonly IChannelServerConnection connection;
        protected readonly string channelKey;
        protected readonly Dict vars;

        public Channel(IChannelServerConnection connection, string channelKey, Dict vars) {
            this.connection = connection;
            this.channelKey = channelKey;
            this.vars = vars;
        }

        public IChannelServerConnection Connection => this.connection;

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
