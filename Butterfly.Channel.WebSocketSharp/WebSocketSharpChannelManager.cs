using System;
using System.Collections.Specialized;
using System.Collections.Concurrent;
using System.Threading.Tasks;

using WebSocketSharp.Server;
using WebSocketSharp;
using System.Net;

namespace Butterfly.Channel.WebSocketSharp {
    public class WebSocketSharpChannelManager : BaseChannelManager {

        protected readonly WebSocketServer webSocketServer;
        protected readonly ConcurrentDictionary<string, MyWebSocketBehavior> webSocketBehaviorById = new ConcurrentDictionary<string, MyWebSocketBehavior>();

        public WebSocketSharpChannelManager(string ipAddress, int port) {
            IPAddress ip = IPAddress.Parse(ipAddress);
            this.webSocketServer = new WebSocketServer(ip, port);
            this.webSocketServer.AddWebSocketService<MyWebSocketBehavior>("/channel", () => new MyWebSocketBehavior(this));
        }

        public Channel CreateChannel(string path) {
            string id = Guid.NewGuid().ToString();
            var webSocketChannel = new WebSocketChannel(this, id);
            webSocketChannel.Start();
            return webSocketChannel;
        }

        public void AddWebSocketBehavior(MyWebSocketBehavior webSocketBehavior) {
            this.webSocketBehaviorById.TryAdd(webSocketBehavior.ChannelId, webSocketBehavior);
        }

        public MyWebSocketBehavior GetWebSocketBehavior(string id) {
            if (this.webSocketBehaviorById.TryGetValue(id, out MyWebSocketBehavior result)) {
                return result;
            }
            else {
                return null;
            }
        }

        public void RemoveWebSocketBehavior(string id) {
            this.webSocketBehaviorById.TryRemove(id, out MyWebSocketBehavior removedWebSocketBehavior);
        }
    }

    public class WebSocketChannel : Channel {

        protected readonly WebSocketSharpChannelManager webSocketSharpChannelManager;

        public WebSocketChannel(WebSocketSharpChannelManager webSocketSharpChannelManager, string id) : base(id) {
            this.webSocketSharpChannelManager = webSocketSharpChannelManager;
        }

        protected bool started = false;
        public void Start() {
            this.started = true;
            Task.Run(this.Run);
        }

        public void Stop() {
            this.started = false;
            this.monitor.PulseAll();
        }

        protected async Task Run() {
            while (this.started) {
                if (this.buffer.TryDequeue(out string result)) {
                    MyWebSocketBehavior webSocketBehavior = this.webSocketSharpChannelManager.GetWebSocketBehavior(this.Id);
                    await webSocketBehavior.SendMessage(result);
                }
                else {
                    using (await this.monitor.EnterAsync()) {
                    }
                }
            }
        }
    }

    public class MyWebSocketBehavior : WebSocketBehavior {

        protected readonly WebSocketSharpChannelManager webSocketSharpChannelManager;
        public MyWebSocketBehavior(WebSocketSharpChannelManager webSocketSharpChannelManager) {
            this.webSocketSharpChannelManager = webSocketSharpChannelManager;
        }

        public string ChannelId {
            get;
            protected set;
        }

        protected override async Task OnOpen() {
            NameValueCollection queryString = await this.Context.GetQueryString();
            this.ChannelId = queryString["id"];
            this.webSocketSharpChannelManager.AddWebSocketBehavior(this);
        }

        public async Task SendMessage(string text) {
            await this.Send(text);
        }

        protected override Task OnClose(CloseEventArgs e) {
            this.webSocketSharpChannelManager.RemoveWebSocketBehavior(this.ChannelId);
            return Task.FromResult(0);
        }

    }
}
