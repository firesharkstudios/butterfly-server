using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using NLog;
using System.Net;

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
        protected readonly Action<string> onMessage;

        protected readonly int heartbeatEveryMillis;
        protected readonly int receiveBufferSize;

        protected readonly Action<WebSocketChannelClientStatus> onStatusChange;

        public WebSocketChannelClient(string url, string authorization, Action<string> onMessage, Action<WebSocketChannelClientStatus> onStatusChange = null, int heartbeatEveryMillis = 3000, int receiveBufferSize = 4096) {
            this.url = new Uri(url);
            this.authorization = authorization;
            this.onMessage = onMessage;
            this.onStatusChange = onStatusChange;

            this.heartbeatEveryMillis = heartbeatEveryMillis;
            this.receiveBufferSize = receiveBufferSize;
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

                Task receivingTask = Task.Run(async() => {
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
                            if (!string.IsNullOrEmpty(message) && this.onMessage != null) this.onMessage(message);
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
                await Task.WhenAny(receivingTask, heartbeatTask);
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
}
