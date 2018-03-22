using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using NLog;
using System.Net;
using System.Collections.Generic;

using Dict = System.Collections.Generic.Dictionary<string, object>;
using System.Linq;
using Butterfly.Util;

namespace Butterfly.Client.DotNet {

    public enum WebSocketChannelClientStatus {
        Connecting,
        Reconnecting,
        Connected
    }

    public class WebSocketChannelClient : IDisposable {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly Uri url;
        protected readonly string authorization;

        protected readonly int heartbeatEveryMillis;
        protected readonly int sendSubscriptionsCheckEveryMillis;
        protected readonly int receiveBufferSize;

        protected readonly Action<WebSocketChannelClientStatus> onStatusChange;

        protected readonly Dictionary<string, Subscription> subscriptionByChannelKey = new Dictionary<string, Subscription>();

        protected bool sendSubscriptions = false;

        public WebSocketChannelClient(string url, string authorization, Action<WebSocketChannelClientStatus> onStatusChange = null, int heartbeatEveryMillis = 3000, int sendSubscriptionsCheckEveryMillis = 100, int receiveBufferSize = 4096) {
            this.url = new Uri(url);
            this.authorization = authorization;
            this.onStatusChange = onStatusChange;

            this.heartbeatEveryMillis = heartbeatEveryMillis;
            this.sendSubscriptionsCheckEveryMillis = sendSubscriptionsCheckEveryMillis;
            this.receiveBufferSize = receiveBufferSize;
        }

        public void Subscribe(Action<string> onMessage, string channelKey = "default", Dict vars = null) {
            this.subscriptionByChannelKey[channelKey] = new Subscription(vars, onMessage);
            this.sendSubscriptions = true;
        }

        public void Unsubscribe(string channelKey = "default") {
            if (this.subscriptionByChannelKey.Remove(channelKey)) {
                this.sendSubscriptions = true;
            }
        }

        public void Start() {
            Task runTask = Task.Run(this.RunAsync);
        }

        protected readonly CancellationTokenSource connectingCancellationSource = new CancellationTokenSource();
        protected CancellationTokenSource ioCancellationTokenSource = null;

        protected bool isFirst = true;
        protected async Task RunAsync() {
            while (!this.connectingCancellationSource.IsCancellationRequested) {
                if (this.onStatusChange != null) this.onStatusChange(isFirst ? WebSocketChannelClientStatus.Connecting : WebSocketChannelClientStatus.Reconnecting);
                this.isFirst = false;

                this.ioCancellationTokenSource = new CancellationTokenSource();

                ClientWebSocket clientWebSocket = new ClientWebSocket();
                try {
                    await clientWebSocket.ConnectAsync(this.url, this.connectingCancellationSource.Token);
                    await this.SendText(clientWebSocket, $"{HttpRequestHeader.Authorization.ToString()}: {this.authorization}", this.ioCancellationTokenSource.Token);
                    if (this.onStatusChange != null) this.onStatusChange(WebSocketChannelClientStatus.Connected);
                }
                catch (Exception e) {
                    logger.Error(e);
                    await Task.Delay(this.heartbeatEveryMillis);
                    continue;
                }

                Task receivingTask = Task.Run(async () => {
                    try {
                        while (!ioCancellationTokenSource.IsCancellationRequested) {
                            string message = null;
                            try {
                                ArraySegment<Byte> buffer = new ArraySegment<byte>(new Byte[this.receiveBufferSize]);
                                WebSocketReceiveResult result = null;
                                using (var memoryStream = new MemoryStream()) {
                                    do {
                                        result = await clientWebSocket.ReceiveAsync(buffer, CancellationToken.None);
                                        memoryStream.Write(buffer.Array, buffer.Offset, result.Count);
                                    } while (!result.EndOfMessage);

                                    memoryStream.Seek(0, SeekOrigin.Begin);

                                    if (result.MessageType == WebSocketMessageType.Text) {
                                        using (var reader = new StreamReader(memoryStream, Encoding.UTF8)) {
                                            message = reader.ReadToEnd();
                                        }
                                    }
                                }
                            }
                            catch (Exception e) {
                                logger.Error(e);
                                break;
                            }

                            logger.Debug($"message={message}");
                            if (string.IsNullOrEmpty(message)) {
                            }
                            else if (message == "$AUTHENTICATED") {
                                // Change status to authenticated
                            }
                            else {
                                int pos = message.IndexOf(':');
                                var channelKey = message.Substring(0, pos).Trim();
                                if (this.subscriptionByChannelKey.TryGetValue(channelKey, out Subscription subscription)) {
                                    var payload = message.Substring(pos + 1).Trim();
                                    subscription.onMessage(payload);
                                }
                            }
                        }
                    }
                    catch (TaskCanceledException) {
                    }
                });

                Task heartbeatTask = Task.Run(async () => {
                    try {
                        while (!this.ioCancellationTokenSource.IsCancellationRequested) {
                            logger.Debug($"RunAsync():this.ioCancellationTokenSource.IsCancellationRequested={this.ioCancellationTokenSource.IsCancellationRequested}");
                            try {
                                await this.SendText(clientWebSocket, "!", this.ioCancellationTokenSource.Token);
                            }
                            catch (Exception e) {
                                logger.Error(e);
                                break;
                            }
                            await Task.Delay(this.heartbeatEveryMillis, this.ioCancellationTokenSource.Token);
                        }
                    }
                    catch (TaskCanceledException) {
                    }
                });

                Task subscriptionsTask = Task.Run(async () => {
                    try {
                        while (!this.ioCancellationTokenSource.IsCancellationRequested) {
                            logger.Debug($"RunAsync():this.ioCancellationTokenSource.IsCancellationRequested={this.ioCancellationTokenSource.IsCancellationRequested}");
                            if (this.sendSubscriptions) {
                                try {
                                    var payload = subscriptionByChannelKey.Select(x => new {
                                        channelKey = x.Key,
                                        x.Value.vars
                                    });
                                    var json = JsonUtil.Serialize(payload);
                                    logger.Debug($"RunAsync():json={json}");
                                    await this.SendText(clientWebSocket, $"Subscribe: {json}", this.ioCancellationTokenSource.Token);
                                }
                                catch (Exception e) {
                                    logger.Error(e);
                                    break;
                                }
                                this.sendSubscriptions = false;
                            }
                            await Task.Delay(this.sendSubscriptionsCheckEveryMillis, this.ioCancellationTokenSource.Token);
                        }
                    }
                    catch (TaskCanceledException) {
                    }
                });
                await Task.WhenAny(receivingTask, heartbeatTask, subscriptionsTask);
                ioCancellationTokenSource.Cancel();
            }
        }

        protected async Task SendText(ClientWebSocket clientWebSocket, string text, CancellationToken cancellationToken) {
            ArraySegment<byte> outputBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(text));
            await clientWebSocket.SendAsync(outputBuffer, WebSocketMessageType.Text, true, cancellationToken);
        }

        public void Dispose() {
            logger.Debug("Dispose()");
            this.connectingCancellationSource.Cancel();
            this.ioCancellationTokenSource.Cancel();
        }
    }

    public class Subscription {
        public readonly Dict vars;
        public readonly Action<string> onMessage;

        public Subscription(Dict vars, Action<string> onMessage) {
            this.vars = vars;
            this.onMessage = onMessage;
        }
    }
}
