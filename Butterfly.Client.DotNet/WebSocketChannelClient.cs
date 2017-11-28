using System;

using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Butterfly.Database.Event;
using Butterfly.Util;

namespace Butterfly.Client.DotNet {

    public enum WebSocketChannelClientStatus {
        Connecting,
        Reconnecting,
        Connected
    }

    public class WebSocketChannelClient : IDisposable {

        protected readonly Uri url;
        protected readonly Action<DataEventTransaction> onDataEventTransaction;

        protected readonly int heartbeatEveryMillis;
        protected readonly int receiveBufferSize;

        protected readonly Action<WebSocketChannelClientStatus> onStatusChange;

        public WebSocketChannelClient(string url, Action<DataEventTransaction> onDataEventTransaction, Action<WebSocketChannelClientStatus> onStatusChange = null, int heartbeatEveryMillis = 3000, int receiveBufferSize = 4096) {
            this.url = new Uri(url);
            this.onDataEventTransaction = onDataEventTransaction;

            this.heartbeatEveryMillis = heartbeatEveryMillis;
            this.onStatusChange = onStatusChange;
        }

        public void Start() {
            Task receiveTask = Task.Run(this.RunAsync);
        }

        protected readonly CancellationTokenSource connectingCancellationSource = new CancellationTokenSource();
        protected CancellationTokenSource ioCancellationTokenSource = null;

        protected bool isFirst = true;
        protected async Task RunAsync() {
            byte[] buffer = new byte[this.receiveBufferSize];
            while (!this.connectingCancellationSource.IsCancellationRequested) {
                if (this.onStatusChange != null) this.onStatusChange(isFirst ? WebSocketChannelClientStatus.Connecting : WebSocketChannelClientStatus.Reconnecting);
                this.isFirst = false;

                ClientWebSocket clientWebSocket = new ClientWebSocket();
                try {
                    await clientWebSocket.ConnectAsync(this.url, this.connectingCancellationSource.Token);
                    if (this.onStatusChange != null) this.onStatusChange(WebSocketChannelClientStatus.Connected);
                }
                catch (Exception e) {
                    await Task.Delay(this.heartbeatEveryMillis);
                    continue;
                }

                this.ioCancellationTokenSource = new CancellationTokenSource();
                
                Task receivingTask = Task.Run(async() => {
                    while (!ioCancellationTokenSource.IsCancellationRequested) {
                        WebSocketReceiveResult result = null;
                        try {
                            result = await clientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), this.ioCancellationTokenSource.Token);
                        }
                        catch (Exception e) {
                            break;
                        }
                        var json = (new UTF8Encoding()).GetString(buffer);
                        var dataEventTransaction = JsonUtil.Deserialize<DataEventTransaction>(json);
                        if (this.onDataEventTransaction != null) this.onDataEventTransaction(dataEventTransaction);
                    }
                });

                Task sendingTask = Task.Run(async () => {
                    ArraySegment<byte> outputBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes("!"));
                    while (!this.ioCancellationTokenSource.IsCancellationRequested) {
                        try {
                            await clientWebSocket.SendAsync(outputBuffer, WebSocketMessageType.Text, true, this.ioCancellationTokenSource.Token);
                        }
                        catch (Exception e) {
                            break;
                        }
                        await Task.Delay(this.heartbeatEveryMillis);
                    }
                });
                Task.WaitAny(receivingTask, sendingTask);
                ioCancellationTokenSource.Cancel();
            }
        }

        public void Dispose() {
            this.connectingCancellationSource.Cancel();
            this.ioCancellationTokenSource.Cancel();
        }
    }
}
