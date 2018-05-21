using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Butterfly.Core.Util;
using Butterfly.Client.DotNet;

namespace Butterfly.Core.Channel.Test {
    [TestClass]
    public class ChannelUnitTest {
        /*
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
        */

        public static async Task TestChannel(IChannelServer channelServer, Action start) {
            // Listen for new channels at /test to be created
            Channel channelA = null;
            TestDisposable testDisposableA = new TestDisposable();

            Channel channelB = null;
            TestDisposable testDisposableB = new TestDisposable();

            // Register route
            var route = channelServer.RegisterRoute("/test");

            // Register channel
            route.RegisterChannel(channelKey: "A", handler: (vars, channel) => {
                channelA = channel;
                return testDisposableA;
            });
            route.RegisterChannel(channelKey: "B", handler: (vars, channel) => {
                channelB = channel;
                return testDisposableB;
            });
            channelServer.Start();
            if (start != null) start();

            var testAuthId = "123";

            // Test creating a channel from the client
            var channelClient = new WebSocketChannelClient("ws://localhost:8080/test", $"Test {testAuthId}", heartbeatEveryMillis: 1000);
            channelClient.Start();
            await Task.Delay(500);
            Assert.AreEqual(1, channelServer.AuthenticatedConnections.Count);
            Assert.IsNotNull(channelServer.GetConnection(testAuthId));

            List<string> messageCollectorA = new List<string>();
            channelClient.Subscribe(message => {
                string data = message.GetAs("data", (string)null);
                messageCollectorA.Add(data);
            }, channelKey: "A");
            await Task.Delay(500);
            Assert.IsNotNull(channelA);

            List<string> messageCollectorB = new List<string>();
            channelClient.Subscribe(message => {
                string data = message.GetAs("data", (string)null);
                messageCollectorB.Add(data);
            }, channelKey: "B");
            await Task.Delay(500);
            Assert.IsNotNull(channelB);

            // Test if sending a message on the server is received on the client
            channelServer.GetConnection(testAuthId, true).QueueMessage(channelKey: channelA.ChannelKey, messageType: "TypeA", data: "HelloA");
            await Task.Delay(200);
            Assert.AreEqual(1, messageCollectorA.Count);
            Assert.AreEqual(0, messageCollectorB.Count);
            Assert.AreEqual("HelloA", messageCollectorA[0]);

            // Test if sending a message on the server is received on the client
            channelServer.GetConnection(testAuthId, true).QueueMessage(channelKey: channelB.ChannelKey, messageType: "TypeB", data: "HelloB");
            await Task.Delay(200);
            Assert.AreEqual(1, messageCollectorA.Count);
            Assert.AreEqual(1, messageCollectorB.Count);
            Assert.AreEqual("HelloB", messageCollectorB[0]);

            // Test if heartbeats keep the channel alive properly
            await Task.Delay(3000);
            Assert.AreEqual(1, channelServer.AuthenticatedConnections.Count);

            // Test if channel is disposed if it is removed from server
            channelClient.Dispose();
            await Task.Delay(3000);
            Assert.AreEqual(0, channelServer.AuthenticatedConnections.Count);

            // Test if the disposable returned by OnNewChannel() was disposed as well
            Assert.AreEqual(true, testDisposableA.disposed);
            Assert.AreEqual(true, testDisposableB.disposed);
        }
    }

    public class TestDisposable : IDisposable {
        public bool disposed = false;

        public void Dispose() {
            this.disposed = true;
        }
    }
}
