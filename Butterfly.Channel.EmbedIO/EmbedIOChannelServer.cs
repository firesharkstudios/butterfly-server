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
using System.Threading.Tasks;

using NLog;
using Unosquare.Labs.EmbedIO.Modules;
using Unosquare.Net;
using Unosquare.Labs.EmbedIO;

using Butterfly.Util;

namespace Butterfly.Channel.EmbedIO {
    public class EmbedIOChannelServer : BaseChannelServer {
        public readonly WebServer webServer;

        public EmbedIOChannelServer(WebServer webServer, int mustReceiveHeartbeatMillis = 5000) : base(mustReceiveHeartbeatMillis) {
            this.webServer = webServer;
        }

        protected override void DoStart() {
            this.webServer.RegisterModule(new WebSocketsModule());
            foreach (var listener in this.onNewChannelListeners) {
                this.webServer.Module<WebSocketsModule>().RegisterWebSocketsServer(listener.pathFilter, new MyWebSocketsServer(this, (channelId, path, channel) => {
                    this.AddAndStartChannel(channelId, path, channel);
                }));
            }
        }
    }

    public class MyWebSocketsServer : WebSocketsServer {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly IChannelServer channelServer;

        public MyWebSocketsServer(IChannelServer channelServer, Action<string, string, IChannel> addAndStartChannel) {
            this.channelServer = channelServer;
        }

        public override string ServerName => "EmbedIO Channel Server";

        protected override void OnClientConnected(WebSocketContext context) {
            string path = GetPath(context);
            string channelId = GetChannelId(context);
            logger.Debug($"OnClientConnected():Websocket created for path {path}, channelId {channelId}");
            var channel = new EmbedIOChannel(channelId, context, message => {
                this.Send(context, message);
            });
        }

        protected override void OnClientDisconnected(WebSocketContext context) {
        }

        protected override void OnFrameReceived(WebSocketContext context, byte[] rxBuffer, WebSocketReceiveResult rxResult) {
        }

        protected override void OnMessageReceived(WebSocketContext context, byte[] rxBuffer, WebSocketReceiveResult rxResult) {
            string channelId = GetChannelId(context);
            var embedIOChannel = this.GetEmbedIOChannel(channelId);
            embedIOChannel.Heartbeat();
        }

        protected static string GetPath(WebSocketContext context) {
            return context.RequestUri.AbsolutePath;
        }

        protected static string GetChannelId(WebSocketContext context) {
            return context.RequestUri.ParseQuery().GetAs("id", "");
        }

        protected EmbedIOChannel GetEmbedIOChannel(string channelId) {
            return channelServer.GetChannel(channelId) as EmbedIOChannel;
        }
    }

    public class EmbedIOChannel : BaseChannel {

        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly WebSocketContext context;
        protected readonly Action<string> send;

        public EmbedIOChannel(string id, WebSocketContext context, Action<string> send) : base(id) {
            this.context = context;
            this.send = send;
        }

        protected override Task SendAsync(string text) {
            //logger.Debug($"Send():channelId={channelId},text={text}");
            this.send(text);
            return Task.FromResult(0);
        }

        protected override void DoDispose() {
            logger.Debug($"DoDispose():id={this.id}");
        }

    }
}
