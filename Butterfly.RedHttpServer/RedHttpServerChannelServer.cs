/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Butterfly.Core.Channel;
using Butterfly.Core.Util;

namespace Butterfly.RedHttpServer {

    /// <inheritdoc/>
    public class RedHttpServerChannelServer : BaseChannelServer {
        public readonly global::RedHttpServerNet45.RedHttpServer server;
        public readonly string path;

        public RedHttpServerChannelServer(RedHttpServer server, int mustReceiveHeartbeatMillis = 5000, string path = "/ws", Func<string, string, object> getAuthToken = null, Func<string, string, Task<object>> getAuthTokenAsync = null, Func<object, string> getConnectionId = null, Func<object, Task<string>> getConnectionIdAsync = null) : base(mustReceiveHeartbeatMillis, getAuthToken, getAuthTokenAsync, getConnectionId, getConnectionIdAsync) {
            if (EnvironmentX.IsRunningOnMono()) throw new Exception("Unfortunately, RedHttpServer does not support WebSockets on Mono");
            this.server = server;
            this.path = path;
        }

        protected override void DoStart() {
            logger.Info($"DoStart():Listening for WebSocket requests at {this.path}");
            this.server.WebSocket(this.path, (req, wsd) => {
                this.AddUnauthenticatedConnection(new WebSocketDialogChannel(this, wsd));
            });
        }

    }

    public class WebSocketDialogChannel : BaseChannelServerConnection {

        protected readonly WebSocketDialog webSocketDialog;

        public WebSocketDialogChannel(BaseChannelServer channelServer, WebSocketDialog webSocketDialog) : base(channelServer) {
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
