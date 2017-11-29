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
using System.Collections.Generic;

using RedHttpServerNet45.Response;
using NLog;

using Butterfly.Channel;

namespace Butterfly.RedHttpServer {
    public class RedHttpServerChannelServer : ChannelServer {

        protected readonly global::RedHttpServerNet45.RedHttpServer server;

        public RedHttpServerChannelServer(global::RedHttpServerNet45.RedHttpServer server, int mustReceiveHeartbeatMillis = 5000) : base(mustReceiveHeartbeatMillis) {
            this.server = server;
        }

        protected override void DoStart() {
            HashSet<string> uniquePaths = new HashSet<string>();
            foreach (var listener in this.onNewChannelListeners) {
                if (!uniquePaths.Contains(listener.path)) uniquePaths.Add(listener.path);
            }
            foreach (var listener in this.onNewChannelAsyncListeners) {
                if (!uniquePaths.Contains(listener.path)) uniquePaths.Add(listener.path);
            }

            foreach (var path in uniquePaths) {
                logger.Debug($"DoStart():Websocket listening on path {path}");
                this.server.WebSocket($"{path}/:channelId", (req, wsd) => {
                    string channelId = req.Params["channelId"];
                    logger.Debug($"DoStart():Websocket created for path {path}, channelId {channelId}");
                    this.CreateChannel(channelId, path, () => new WebSocketDialogChannel(channelId, wsd));
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
                this.LastHeartbeatReceived = DateTime.Now;
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
