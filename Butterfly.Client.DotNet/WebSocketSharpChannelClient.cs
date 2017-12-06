using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using NLog;
using WebSocketSharp;

namespace Butterfly.Client.DotNet {

    public enum WebSocketSharpChannelClientStatus {
        Connecting,
        Reconnecting,
        Connected
    }

    public class WebSocketSharpChannelClient : IDisposable {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly string url;
        protected readonly string authorization;
        protected readonly Action<string> onMessage;

        protected readonly int heartbeatEveryMillis;
        protected readonly int receiveBufferSize;

        protected readonly Action<WebSocketChannelClientStatus> onStatusChange;

        protected WebSocket webSocket = null;

        public WebSocketSharpChannelClient(string url, string authorization, Action<string> onMessage, Action<WebSocketChannelClientStatus> onStatusChange = null, int heartbeatEveryMillis = 3000, int receiveBufferSize = 4096) {
            this.url = url;
            this.authorization = authorization;
            this.onMessage = onMessage;

            this.heartbeatEveryMillis = heartbeatEveryMillis;
            this.onStatusChange = onStatusChange;
        }

        protected readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        public void Start() {
            Task runTask = Task.Run(this.RunAsync);
        }

        protected bool isFirst = true;
        protected async Task RunAsync() {
            while (!this.cancellationTokenSource.IsCancellationRequested) {
                if (this.webSocket==null) {
                    if (this.onStatusChange != null) this.onStatusChange(this.isFirst ? WebSocketChannelClientStatus.Connecting : WebSocketChannelClientStatus.Reconnecting);
                    this.isFirst = false;

                    this.webSocket = new WebSocket(
                        this.url,
                        cancellationToken: this.cancellationTokenSource.Token,
                        onOpen: async() => {
                            if (this.onStatusChange != null) this.onStatusChange(WebSocketChannelClientStatus.Connected);
                            await this.webSocket.Send($"{HttpRequestHeader.Authorization.ToString()}: {this.authorization}");
                        },
                        onMessage: async (ea) => {
                            if (this.onMessage != null) {
                                string message = await ea.Text.ReadToEndAsync();
                                this.onMessage(message);
                            }
                        },
                        onError: ea => {
                            this.webSocket = null;
                            return Task.FromResult(0);
                        }
                    );
                }

                if (this.webSocket != null) {
                    await this.webSocket.Send("!");
                }

                await Task.Delay(this.heartbeatEveryMillis, this.cancellationTokenSource.Token);
            }
        }

        public void Dispose() {
            this.cancellationTokenSource.Cancel();
        }

    }
}
