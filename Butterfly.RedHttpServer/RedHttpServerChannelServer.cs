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

using System.Threading.Tasks;

using Butterfly.Channel;

using RedHttpServerNet45.Response;
using NLog;
using System.Collections.Generic;

namespace Butterfly.RedHttpServer {
    public class RedHttpServerChannelServer : ChannelServer {

        protected readonly global::RedHttpServerNet45.RedHttpServer server;

        public RedHttpServerChannelServer(global::RedHttpServerNet45.RedHttpServer server) {
            this.server = server;
        }

        public override void Start() {
            HashSet<string> uniquePaths = new HashSet<string>();
            foreach (var listener in this.onNewChannelListeners) {
                if (!uniquePaths.Contains(listener.path)) uniquePaths.Add(listener.path);
            }
            foreach (var listener in this.onNewChannelAsyncListeners) {
                if (!uniquePaths.Contains(listener.path)) uniquePaths.Add(listener.path);
            }

            foreach (var path in uniquePaths) {
                this.server.WebSocket($"{path}/:channelId", (req, wsd) => {
                    string channelId = req.Params["channelId"];
                    logger.Debug($"RedHttpServerChannelManager():Websocket created for path {path}, channelId {channelId}");
                    this.CreateChannelTransport(channelId, () => new WebSocketDialogChannelTransport(channelId, wsd));
                    this.CreateChannel(path, channelId);
                });
            }
        }

        public override void Stop() {
        }
    }

    public class WebSocketDialogChannelTransport : IChannelTransport {

        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly string channelId;
        protected readonly WebSocketDialog webSocketDialog;

        public WebSocketDialogChannelTransport(string channelId, WebSocketDialog webSocketDialog) {
            this.channelId = channelId;
            this.webSocketDialog = webSocketDialog;
        }

        public Task Send(string text) {
            //logger.Debug($"Send():channelId={channelId},text={text}");
            return this.webSocketDialog.SendText(text);
        }

        public void Dispose() {
            this.webSocketDialog.Close();
        }
    }
}
