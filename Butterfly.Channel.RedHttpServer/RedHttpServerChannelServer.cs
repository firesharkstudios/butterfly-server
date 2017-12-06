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

using Butterfly.Util;

namespace Butterfly.Channel.RedHttpServer {
    public class RedHttpServerChannelServer : BaseChannelServer {
        public readonly global::RedHttpServerNet45.RedHttpServer server;

        public RedHttpServerChannelServer(global::RedHttpServerNet45.RedHttpServer server, Func<string, string> authenticate = null, int mustReceiveHeartbeatMillis = 5000) : base(authenticate, mustReceiveHeartbeatMillis) {
            if (EnvironmentX.IsRunningOnMono()) throw new Exception("Unfortunately, RedHttpServer does not support WebSockets on Mono");
            this.server = server;
        }

        protected override void DoStart() {
            foreach (var listener in this.onNewChannelListeners) {
                logger.Info($"DoStart():Listening for WebSocket requests at {listener.path}");
                this.server.WebSocket(listener.path, (req, wsd) => {
                    this.AddUnauthenticatedChannel(new WebSocketDialogChannel(this, listener.path, wsd));
                });
            }
        }

    }

    public class WebSocketDialogChannel : BaseChannel {

        protected readonly WebSocketDialog webSocketDialog;

        public WebSocketDialogChannel(BaseChannelServer channelServer, string path, WebSocketDialog webSocketDialog) : base(channelServer, path) {
            this.webSocketDialog = webSocketDialog;

            this.webSocketDialog.OnTextReceived += (sender, eventArgs) => {
                try {
                    this.ReceiveMessage(eventArgs.Text);
                }
                catch (Exception e) {
                    logger.Trace(e);
                    webSocketDialog.Close();
                }
            };
        }

        protected override Task SendAsync(string text) {
            //logger.Trace($"Send():channelId={channelId},text={text}");
            return this.webSocketDialog.SendText(text);
        }

        protected override void DoDispose() {
            logger.Trace($"DoDispose():id={this.AuthId}");
            this.webSocketDialog.Close();
        }

    }
}
