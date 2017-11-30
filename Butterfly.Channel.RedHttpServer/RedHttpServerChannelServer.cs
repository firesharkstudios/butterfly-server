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

using RedHttpServerNet45.Response;
using NLog;

using Butterfly.Util;

namespace Butterfly.Channel.RedHttpServer {
    public class RedHttpServerChannelServer : ChannelServer {

        protected readonly global::RedHttpServerNet45.RedHttpServer server;

        public RedHttpServerChannelServer(global::RedHttpServerNet45.RedHttpServer server, int mustReceiveHeartbeatMillis = 5000) : base(mustReceiveHeartbeatMillis) {
            if (EnvironmentX.IsRunningOnMono()) throw new Exception("Unfortunately, RedHttpServer does not support WebSockets on Mono");
            this.server = server;
        }

        protected override void DoStart() {
            foreach (var listener in this.onNewChannelListeners) {
                logger.Debug($"DoStart():Websocket listening on path {listener.path}");
                this.server.WebSocket(listener.path, (req, wsd) => {
                    string channelId = req.Queries["id"];
                    logger.Debug($"DoStart():Websocket created for path {listener.path}, channelId {channelId}");
                    this.AddAndStartChannel(channelId, listener.path, new WebSocketDialogChannel(channelId, wsd));
                });
            }
        }
    }

    public class WebSocketDialogChannel : BaseChannel {

        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly WebSocketDialog webSocketDialog;

        public WebSocketDialogChannel(string id, WebSocketDialog webSocketDialog) : base(id) {
            logger.Debug($"WebSocketDialogChannel():id={id}");
            this.webSocketDialog = webSocketDialog;

            this.webSocketDialog.OnTextReceived += (sender, eventArgs) => {
                logger.Debug($"WebSocketDialogChannel():New heartbeat...");
                this.Heartbeat();
            };
        }

        protected override Task SendAsync(string text) {
            //logger.Debug($"Send():channelId={channelId},text={text}");
            return this.webSocketDialog.SendText(text);
        }

        protected override void DoDispose() {
            logger.Debug($"DoDispose():id={this.id}");
            this.webSocketDialog.Close();
        }

    }
}
