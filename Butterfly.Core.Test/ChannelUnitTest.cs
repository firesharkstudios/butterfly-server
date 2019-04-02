/* Any copyright is dedicated to the Public Domain.
 * http://creativecommons.org/publicdomain/zero/1.0/ */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Butterfly.Core.Util;
using Butterfly.Client.DotNet;
using Butterfly.Core.Channel;

namespace Butterfly.Core.Test {
    [TestClass]
    public class ChannelUnitTest {
        public static async Task TestChannel(ISubscriptionApi subscriptionApi, string url, Action start) {
            // Listen for new channels at /test to be created
            Butterfly.Core.Channel.Channel channelA = null;
            TestDisposable testDisposableA = new TestDisposable();

            Butterfly.Core.Channel.Channel channelB = null;
            TestDisposable testDisposableB = new TestDisposable();

            // Register channel
            subscriptionApi.OnSubscribe(channelKey: "A", handler: (vars, channel) => {
                channelA = channel;
                return testDisposableA;
            });
            subscriptionApi.OnSubscribe(channelKey: "B", handler: (vars, channel) => {
                channelB = channel;
                return testDisposableB;
            });
            subscriptionApi.Start();
            start?.Invoke();

            var testAuthId = "123";

            // Test creating a channel from the client
            var channelClient = new WebSocketChannelClient(url, $"Test {testAuthId}", heartbeatEveryMillis: 1000);
            channelClient.Start();
            await Task.Delay(3000);
            Assert.AreEqual(1, subscriptionApi.AuthenticatedConnections.Count);
            Assert.IsNotNull(subscriptionApi.GetConnection(testAuthId));

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
            subscriptionApi.GetConnection(testAuthId, true).QueueMessage(channelKey: channelA.ChannelKey, messageType: "TypeA", data: "HelloA");
            await Task.Delay(200);
            Assert.AreEqual(1, messageCollectorA.Count);
            Assert.AreEqual(0, messageCollectorB.Count);
            Assert.AreEqual("HelloA", messageCollectorA[0]);

            // Test if sending a message on the server is received on the client
            subscriptionApi.GetConnection(testAuthId, true).QueueMessage(channelKey: channelB.ChannelKey, messageType: "TypeB", data: "HelloB");
            await Task.Delay(200);
            Assert.AreEqual(1, messageCollectorA.Count);
            Assert.AreEqual(1, messageCollectorB.Count);
            Assert.AreEqual("HelloB", messageCollectorB[0]);

            // Test if heartbeats keep the channel alive properly
            await Task.Delay(3000);
            Assert.AreEqual(1, subscriptionApi.AuthenticatedConnections.Count);

            // Test if channel is disposed if it is removed from server
            channelClient.Dispose();
            await Task.Delay(3000);
            Assert.AreEqual(0, subscriptionApi.AuthenticatedConnections.Count);

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
