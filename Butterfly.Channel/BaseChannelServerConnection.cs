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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using Nito.AsyncEx;
using NLog;

using Butterfly.Util;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Channel {

    /// <inheritdoc/>
    /// <summary>
    /// Base class implementing <see cref="IChannelServerConnection"/>. New implementations will normally extend this class.
    /// </summary>
    public abstract class BaseChannelServerConnection : IChannelServerConnection {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly BaseChannelServer channelServer;
        protected readonly RegisteredRoute registeredRoute;
        protected readonly DateTime created;

        protected readonly ConcurrentQueue<string> buffer = new ConcurrentQueue<string>();
        protected readonly AsyncMonitor monitor = new AsyncMonitor();

        protected object authToken;
        protected string id = null;

        /// <summary>
        /// Stores when the datetime of the last heartbeat (set via <ref>Heartbeat</ref>)
        /// </summary>
        protected DateTime lastHeartbeat = DateTime.Now;

        public BaseChannelServerConnection(BaseChannelServer channelServer, RegisteredRoute registeredRoute) {
            this.channelServer = channelServer;
            this.registeredRoute = registeredRoute;
            this.created = DateTime.Now;
        }

        public object AuthToken => this.authToken;

        public string Id => this.id;

        public DateTime Created => this.created;

        public RegisteredRoute RegisteredRoute => this.registeredRoute;

        /// <summary>
        /// When the last heartbeat was registered
        /// </summary>
        public DateTime LastHeartbeat => this.lastHeartbeat;

        /// <summary>
        /// Implementing classes should call this periodically to keep the channel alive (otherwise <ref>ChannelServer</ref> will remove the channel)
        /// </summary>
        internal void Heartbeat() {
            logger.Trace($"Heartbeat()");
            this.lastHeartbeat = DateTime.Now;
        }

        /// <summary>
        /// Queue an object to be sent over the channel to the client.  The queue is processed by a background thread when the Channel is started.
        /// </summary>
        /// <param name="messageType">The value to be sent to the client (will be converted to JSON)</param>
        /// <param name="channelKey">The value to be sent to the client (will be converted to JSON)</param>
        /// <param name="value">The value to be sent to the client (will be converted to JSON)</param>
        public void QueueChannelMessage(string messageType, string channelKey = null, object value = null) {
            if (messageType.Contains(":")) throw new Exception($"Message type {messageType} may not contain a colon");
            if (channelKey!=null && channelKey.Contains(":")) throw new Exception($"Channel key {channelKey} may not contain a colon");

            string text;
            if (channelKey == null) {
                text = $"{messageType}";
            }
            else if (value == null) {
                text = $"{messageType}:{channelKey}";
            }
            else {
                string json = JsonUtil.Serialize(value);
                text = $"{messageType}:{channelKey}:{json}";
            }
            this.buffer.Enqueue(text);
            this.monitor.PulseAll();
        }

        protected bool started = false;
        public void Start(object authToken, string id) {
            this.authToken = authToken;
            this.id = id;
            this.started = true;
            Task.Run(() => this.RunAsync());
        }

        protected readonly Dictionary<string, Channel> channelByKey = new Dictionary<string, Channel>();

        protected async Task RunAsync() {
            try {
                while (this.started) {
                    if (this.buffer.TryDequeue(out string result)) {
                        await this.SendAsync(result);
                    }
                    else {
                        using (await this.monitor.EnterAsync()) {
                            await this.monitor.WaitAsync();
                        }
                    }
                }
            }
            finally {
                foreach (var channel in channelByKey.Values) {
                    channel.Dispose();
                }
            }
        }

        /// <summary>
        /// Implementing classes must override this to actually send the text to the client
        /// </summary>
        protected abstract Task SendAsync(string text);

        public async Task ReceiveMessageAsync(string text) {
            if (text == "!") {
                this.Heartbeat();
            }
            else {
                int pos = text.IndexOf(':');
                if (pos > 0) {
                    string name = text.Substring(0, pos).Trim();
                    string value = text.Substring(pos + 1).Trim();
                    logger.Debug($"ReceiveMessage():name={name},value={value}");
                    if (name == HttpRequestHeader.Authorization.ToString()) {
                        this.UnsubscribeAll();
                        if (!string.IsNullOrEmpty(value)) {
                            var authenticationHeaderValue = AuthenticationHeaderValue.Parse(value);
                            await this.channelServer.AuthenticateAsync(authenticationHeaderValue.Scheme, authenticationHeaderValue.Parameter, this);
                        }
                    }
                    else if (name == "Subscribe") {
                        try {
                            Dict[] subscriptions = JsonUtil.Deserialize<Dict[]>(value);
                            await this.SubscribeAsync(subscriptions);
                        }
                        catch (Exception e) {
                            logger.Error(e);
                        }
                    }
                    else if (name == "Unsubscribe") {
                        try {
                            string[] channelKeys;
                            if (value == "*") {
                                this.UnsubscribeAll();
                            }
                            else {
                                string channelKey = JsonUtil.Deserialize<string>(value);
                                channelKeys = new string[] { channelKey };
                                this.Unsubscribe(channelKeys);
                            }
                        }
                        catch (Exception e) {
                            logger.Error(e);
                        }
                    }
                    else {
                        logger.Warn($"ReceiveMessage():Unknown message '{name}'");
                    }
                }
            }
        }

        protected Dict EMPTY_DICT = new Dict();

        protected async Task SubscribeAsync(ICollection<Dict> subscriptions) {
            logger.Debug($"SubscribeAsync()");

            if (subscriptions != null) {
                foreach (var subscription in subscriptions) {
                    var channelKey = subscription.GetAs("channelKey", (string)null);
                    var vars = subscription.GetAs("vars", (Dict)null) ?? EMPTY_DICT;
                    logger.Debug($"SubscribeAsync():channelKey={channelKey},vars={vars}");

                    if (this.channelByKey.TryGetValue(channelKey, out Channel existingChannel)) {
                        existingChannel.Dispose();
                        this.channelByKey.Remove(channelKey);
                    }

                    logger.Debug($"SubscribeAsync():Creating new channel {channelKey}");
                    var channel = new Channel(this, channelKey, vars);
                    if (this.registeredRoute.RegisteredChannelByKey.TryGetValue(channelKey, out RegisteredChannel registeredChannel)) {
                        try {
                            var disposable = registeredChannel.handle != null ? registeredChannel.handle(vars, channel) : await registeredChannel.handleAsync(vars, channel);
                            if (disposable != null) {
                                channel.Attach(disposable);
                            }
                            this.channelByKey.Add(channelKey, channel);
                        }
                        catch (Exception e) {
                            channel.Queue("ERROR", e.Message);
                            channel.Dispose();
                            logger.Error(e);
                        }
                    }
                    else {
                        logger.Debug($"SubscribeAsync():Unknown subscription channel key '{channelKey}'");
                    }
                }
            }
        }

        protected void UnsubscribeAll() {
            this.Unsubscribe(this.channelByKey.Keys.ToArray());
        }

        protected void Unsubscribe(ICollection<string> channelKeys) {
            try {
                logger.Debug($"UnsubscribeAsync()");
                foreach (var channelKey in channelKeys) {
                    if (this.channelByKey.TryGetValue(channelKey, out Channel existingChannel)) {
                        logger.Debug($"SubscribeAsync():Removing channel key '{channelKey}'");
                        existingChannel.Dispose();
                        this.channelByKey.Remove(channelKey);
                    }
                }
            }
            catch (Exception e) {
                logger.Error(e);
            }
        }

        /// <summary>
        /// Implements the IDispose interface
        /// </summary>
        public void Dispose() {
            this.started = false;

            foreach (var channel in this.channelByKey.Values) {
                channel.Dispose();
            }
            this.channelByKey.Clear();

            this.monitor.PulseAll();

            this.DoDispose();
        }

        /// <summary>
        /// Implementing classes may optionally override this to cleanup resources as appropriate
        /// </summary>
        protected virtual void DoDispose() {
        }

    }
}
