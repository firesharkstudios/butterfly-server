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
using System.Threading.Tasks;

using NLog;
using Unosquare.Labs.EmbedIO.Modules;
using Unosquare.Net;
using Unosquare.Labs.EmbedIO;

using Butterfly.Core.Channel;
using Butterfly.Core.Util;

namespace Butterfly.EmbedIO {

    /// <inheritdoc/>
    public class EmbedIOChannelServer : BaseChannelServer {
        public readonly WebServer webServer;
        public readonly string path;

        public EmbedIOChannelServer(WebServer webServer, int mustReceiveHeartbeatMillis = 5000, string path = "/ws", Func<string, string, object> getAuthToken = null, Func<string, string, Task<object>> getAuthTokenAsync = null, Func<object, string> getId = null, Func<object, Task<string>> getIdAsync = null) : base(mustReceiveHeartbeatMillis, getAuthToken, getAuthTokenAsync, getId, getIdAsync) {
            this.webServer = webServer;
            this.path = path;
        }

        protected override void DoStart() {
            this.webServer.RegisterModule(new WebSocketsModule());
            logger.Info($"Listening for WebSocket requests at {this.path}");
            this.webServer.Module<WebSocketsModule>().RegisterWebSocketsServer(this.path, new MyWebSocketsServer(this, (webRequest, channel) => {
                this.AddUnauthenticatedConnection(channel);
                return true;
            }));
        }
    }

    public class MyWebSocketsServer : WebSocketsServer {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly BaseChannelServer channelServer;
        protected readonly Func<IWebRequest, IChannelServerConnection, bool> onNewChannel;
        protected readonly ConcurrentDictionary<WebSocketContext, EmbedIOChannelServerConnection> channelByWebSocketContext = new ConcurrentDictionary<WebSocketContext, EmbedIOChannelServerConnection>();

        public MyWebSocketsServer(BaseChannelServer channelServer, Func<IWebRequest, IChannelServerConnection, bool> onNewChannel) {
            this.channelServer = channelServer;
            this.onNewChannel = onNewChannel;
        }

        public override string ServerName => "EmbedIO Channel Server";

        protected override void OnClientConnected(WebSocketContext context) {
            var webRequest = new EmbedIOWebSocketWebRequest(context);
            logger.Trace($"OnClientConnected():Websocket created for path {webRequest.RequestUrl.AbsolutePath}");
            var channel = new EmbedIOChannelServerConnection(this.channelServer, message => {
                this.Send(context, message);
            }, context);
            bool valid = this.onNewChannel(webRequest, channel);
            if (valid) {
                this.channelByWebSocketContext[context] = channel;
            }
        }

        protected override void OnClientDisconnected(WebSocketContext context) {
            this.channelByWebSocketContext.TryRemove(context, out EmbedIOChannelServerConnection dummyContext);
        }

        protected override void OnFrameReceived(WebSocketContext context, byte[] rxBuffer, WebSocketReceiveResult rxResult) {
        }

        protected override void OnMessageReceived(WebSocketContext context, byte[] rxBuffer, WebSocketReceiveResult rxResult) {
            var text = System.Text.Encoding.UTF8.GetString(rxBuffer);
            logger.Debug($"OnMessageReceived():text={text}");
            if (this.channelByWebSocketContext.TryGetValue(context, out EmbedIOChannelServerConnection embedIOChannel)) {
                try {
                    embedIOChannel.ReceiveMessageAsync(text).Wait();
                }
                catch (Exception e) {
                    logger.Trace(e);
                    embedIOChannel.Dispose();
                }
            }
        }

        protected EmbedIOChannelServerConnection GetEmbedIOChannel(string channelId) {
            return channelServer.GetConnection(channelId) as EmbedIOChannelServerConnection;
        }
    }

    public class EmbedIOChannelServerConnection : BaseChannelServerConnection {
        protected readonly Action<string> send;
        protected readonly WebSocketContext context;

        public EmbedIOChannelServerConnection(BaseChannelServer channelServer, Action<string> send, WebSocketContext context) : base(channelServer) {
            this.send = send;
            this.context = context;
        }

        protected override Task SendAsync(string text) {
            //logger.Trace($"Send():channelId={channelId},text={text}");
            this.send(text);
            return Task.FromResult(0);
        }

        protected override void DoDispose() {
            //logger.Trace($"DoDispose():id={this.Id}");
            this.context.WebSocket.CloseAsync();
        }

    }

    public class EmbedIOWebSocketWebRequest : IWebRequest {

        protected readonly WebSocketContext context;

        public EmbedIOWebSocketWebRequest(WebSocketContext context) {
            this.context = context;
        }

        public Uri RequestUrl => context.RequestUri;

        public Dictionary<string, string> Headers => context.Headers.ToDictionary();

        public Dictionary<string, string> PathParams => throw new NotImplementedException();

        public Dictionary<string, string> QueryParams => this.RequestUrl.ParseQuery();

    }
}
