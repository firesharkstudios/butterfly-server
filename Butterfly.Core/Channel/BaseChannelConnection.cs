/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using Nito.AsyncEx;
using NLog;

using Butterfly.Core.Util;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Core.Channel {

    /// <inheritdoc/>
    /// <summary>
    /// Base class implementing <see cref="IChannelConnection"/>. New implementations will normally extend this class.
    /// </summary>
    public abstract class BaseChannelConnection : IChannelConnection {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly BaseSubscriptionApi subscriptionApi;
        //protected readonly RegisteredRoute registeredRoute;
        protected readonly DateTime created;

        protected readonly ConcurrentQueue<string> buffer = new ConcurrentQueue<string>();
        protected readonly AsyncMonitor monitor = new AsyncMonitor();

        protected object authToken;
        protected string id = null;

        /// <summary>
        /// Stores when the datetime of the last heartbeat (set via <ref>Heartbeat</ref>)
        /// </summary>
        protected DateTime lastHeartbeat = DateTime.Now;

        public BaseChannelConnection(BaseSubscriptionApi subscriptionApi) {
            this.subscriptionApi = subscriptionApi;
            this.created = DateTime.Now;
        }

        public object AuthToken => this.authToken;

        public string Id => this.id;

        public DateTime Created => this.created;

        //public RegisteredRoute RegisteredRoute => this.registeredRoute;

        /// <summary>
        /// When the last heartbeat was registered
        /// </summary>
        public DateTime LastHeartbeat => this.lastHeartbeat;

        /// <summary>
        /// Implementing classes should call this periodically to keep the channel alive (otherwise <ref>SubscriptionApi</ref> will remove the channel)
        /// </summary>
        internal void Heartbeat() {
            logger.Trace($"Heartbeat()");
            this.lastHeartbeat = DateTime.Now;
        }

        /// <summary>
        /// Queue an object to be sent over the channel to the client.  The queue is processed by a background thread when the Channel is started.
        /// </summary>
        /// <param name="channelKey">The value to be sent to the client (will be converted to JSON)</param>
        /// <param name="messageType">The value to be sent to the client (will be converted to JSON)</param>
        /// <param name="data">The value to be sent to the client (will be converted to JSON)</param>
        public void QueueMessage(string channelKey = null, string messageType = null, object data = null) {
            Dict payload = new Dict();
            if (!string.IsNullOrEmpty(channelKey)) payload["channelKey"] = channelKey;
            if (!string.IsNullOrEmpty(messageType)) payload["messageType"] = messageType;
            if (data!=null) payload["data"] = data;

            string text = JsonUtil.Serialize(payload);
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
            logger.Trace($"ReceiveMessage():text={text}");
            this.Heartbeat();
            if (text != "!") {
                int pos = text.IndexOf(':');
                if (pos > 0) {
                    string name = text.Substring(0, pos).Trim();
                    string value = text.Substring(pos + 1).Trim();
                    logger.Debug($"ReceiveMessage():name={name},value={value}");
                    if (name == HttpRequestHeader.Authorization.ToString()) {
                        this.UnsubscribeAll();
                        try {
                            var authenticationHeaderValue = string.IsNullOrWhiteSpace(value) ? null : AuthenticationHeaderValue.Parse(value);
                            await this.subscriptionApi.AuthenticateAsync(authenticationHeaderValue?.Scheme, authenticationHeaderValue?.Parameter, this);
                            this.QueueMessage(messageType: "AUTHENTICATED");
                        }
                        catch (Exception e) {
                            this.QueueMessage(messageType: "UNAUTHENTICATED", data: e.Message);
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
                    if (this.subscriptionApi.ChannelSubscriptionByKey.TryGetValue(channelKey, out ChannelSubscription registeredChannel)) {
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
            logger.Debug($"Dispose():id={this.Id}");
            this.started = false;

            foreach (var channel in this.channelByKey.Values) {
                try {
                    channel.Dispose();
                }
                catch (Exception e) {
                    logger.Error(e);
                }
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
