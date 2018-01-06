using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Butterfly.Channel.EmbedIO;
using Butterfly.Client.DotNet;
using Butterfly.Util;

namespace Butterfly.Channel.Test {
    [TestClass]
    public class ChannelUnitTest {
        [TestMethod]
        public async Task RedHttpServerChannel() {
            var redHttpServer = new RedHttpServerNet45.RedHttpServer(8080);
            using (var channelServer = new Butterfly.Channel.RedHttpServer.RedHttpServerChannelServer(redHttpServer, mustReceiveHeartbeatMillis: 2000)) {
                await this.TestChannel(channelServer, () => {
                    redHttpServer.Start();
                });
            }
        }

        [TestMethod]
        public async Task EmbedIOChannel() {
            using (var webServer = new Unosquare.Labs.EmbedIO.WebServer("http://localhost:8080/", Unosquare.Labs.EmbedIO.Constants.RoutingStrategy.Regex))
            using (var channelServer = new EmbedIOChannelServer(webServer, mustReceiveHeartbeatMillis: 2000)) {
                await this.TestChannel(channelServer, () => {
                    webServer.RunAsync();
                });
            }
        }

        public async Task TestChannel(IChannelServer channelServer, Action start) {
            // Listen for new channels at /test to be created
            IChannel newChannel = null;
            TestDisposable testDisposable = new TestDisposable();
            channelServer.OnNewChannel("/test", (authType, authValue, channel) => {
                newChannel = channel;
                channel.Attach(testDisposable);
                return authValue;
            });
            channelServer.Start();
            if (start != null) start();

            var testAuthToken = "123";

            // Test creating a channel from the client
            List<string> messageCollector = new List<string>();
            var channelClient = new WebSocketChannelClient("ws://localhost:8080/test", testAuthToken, json => {
                var message = JsonUtil.Deserialize<string>(json);
                messageCollector.Add(message);
            }, heartbeatEveryMillis: 1000);
            channelClient.Start();
            await Task.Delay(1000);
            Assert.IsNotNull(newChannel);
            Assert.AreEqual(1, channelServer.ChannelCount);
            Assert.IsNotNull(channelServer.GetChannel(testAuthToken));

            // Test if sending a message on the server is received on the client
            channelServer.Queue(testAuthToken, "Hello");
            await Task.Delay(200);
            Assert.AreEqual(1, messageCollector.Count);
            Assert.AreEqual("Hello", messageCollector[0]);

            // Test if heartbeats keep the channel alive properly
            await Task.Delay(3000);
            Assert.AreEqual(1, channelServer.ChannelCount);

            // Test if channel is disposed if it is removed from server
            channelClient.Dispose();
            await Task.Delay(3000);
            Assert.AreEqual(0, channelServer.ChannelCount);

            // Test if the disposable returned by OnNewChannel() was disposed as well
            Assert.AreEqual(true, testDisposable.disposed);
        }
    }

    public class TestDisposable : IDisposable {
        public bool disposed = false;

        public void Dispose() {
            this.disposed = true;
        }
    }
}
