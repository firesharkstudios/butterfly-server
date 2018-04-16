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
using System.Collections.Specialized;
using System.Collections.Generic;

namespace Butterfly.Channel.RedHttpServer {

    /// <inheritdoc/>
    public class RedHttpServerChannelServer : BaseChannelServer {
        public readonly global::RedHttpServerNet45.RedHttpServer server;

        public RedHttpServerChannelServer(global::RedHttpServerNet45.RedHttpServer server, int mustReceiveHeartbeatMillis = 5000) : base(mustReceiveHeartbeatMillis) {
            if (EnvironmentX.IsRunningOnMono()) throw new Exception("Unfortunately, RedHttpServer does not support WebSockets on Mono");
            this.server = server;
        }

        protected override void DoStart() {
            foreach ((string routePath, RegisteredRoute registeredRoute) in this.registeredRouteByPath) {
                logger.Info($"DoStart():Listening for WebSocket requests at {routePath}");
                this.server.WebSocket(routePath, (req, wsd) => {
                    this.AddUnauthenticatedConnection(new WebSocketDialogChannel(this, registeredRoute, wsd));
                });
            }
        }

    }

    public class WebSocketDialogChannel : BaseChannelServerConnection {

        protected readonly WebSocketDialog webSocketDialog;

        public WebSocketDialogChannel(BaseChannelServer channelServer, RegisteredRoute registeredRoute, WebSocketDialog webSocketDialog) : base(channelServer, registeredRoute) {
            this.webSocketDialog = webSocketDialog;

            this.webSocketDialog.OnTextReceived += (sender, eventArgs) => {
                try {
                    this.ReceiveMessageAsync(eventArgs.Text).Wait();
                }
                catch (Exception e) {
                    logger.Trace(e);
                    this.Dispose();
                    webSocketDialog.Close();
                }
            };
        }

        protected override Task SendAsync(string text) {
            //logger.Trace($"Send():channelId={channelId},text={text}");
            return this.webSocketDialog.SendText(text);
        }

        protected override void DoDispose() {
            //logger.Trace($"DoDispose():id={this.Id}");
            this.webSocketDialog.Close();
        }

    }

    public class WebSocketDialogWebRequest : IWebRequest {

        protected readonly WebSocketDialog webSocketDialog;

        public WebSocketDialogWebRequest(WebSocketDialog webSocketDialog) {
            this.webSocketDialog = webSocketDialog;
        }
        public Uri RequestUrl => this.webSocketDialog.UnderlyingContext.RequestUri;

        public Dictionary<string, string> Headers => this.webSocketDialog.UnderlyingContext.Headers.ToDictionary();

        public Dictionary<string, string> PathParams => throw new NotImplementedException();

        public Dictionary<string, string> QueryParams => this.RequestUrl.ParseQuery();

    }
}
