using NLog;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Butterfly.Client.DotNet {

    public enum WebSocketChannelClientStatus {
        Connecting,
        Reconnecting,
        Connected
    }

    public class WebSocketChannelClient : IDisposable {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly Uri url;
        protected readonly Action<string> onMessage;

        protected readonly int heartbeatEveryMillis;
        protected readonly int receiveBufferSize;

        protected readonly Action<WebSocketChannelClientStatus> onStatusChange;

        public WebSocketChannelClient(string url, Action<string> onMessage, Action<WebSocketChannelClientStatus> onStatusChange = null, int heartbeatEveryMillis = 3000, int receiveBufferSize = 4096) {
            this.url = new Uri(url);
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

                ClientWebSocket clientWebSocket = new ClientWebSocket();
                try {
                    await clientWebSocket.ConnectAsync(this.url, this.connectingCancellationSource.Token);
                    if (this.onStatusChange != null) this.onStatusChange(WebSocketChannelClientStatus.Connected);
                }
                catch (Exception e) {
                    logger.Error(e);
                    await Task.Delay(this.heartbeatEveryMillis);
                    continue;
                }

                this.ioCancellationTokenSource = new CancellationTokenSource();
                
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

                Task sendingTask = Task.Run(async () => {
                    ArraySegment<byte> outputBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes("!"));
                    try {
                        while (!this.ioCancellationTokenSource.IsCancellationRequested) {
                            logger.Debug($"RunAsync():this.ioCancellationTokenSource.IsCancellationRequested={this.ioCancellationTokenSource.IsCancellationRequested}");
                            try {
                                await clientWebSocket.SendAsync(outputBuffer, WebSocketMessageType.Text, true, this.ioCancellationTokenSource.Token);
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
                await Task.WhenAny(receivingTask, sendingTask);
                ioCancellationTokenSource.Cancel();
            }
        }

        public void Dispose() {
            logger.Debug("Dispose()");
            this.connectingCancellationSource.Cancel();
            this.ioCancellationTokenSource.Cancel();
        }
    }
}
