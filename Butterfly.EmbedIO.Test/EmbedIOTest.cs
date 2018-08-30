/* Any copyright is dedicated to the Public Domain.
 * http://creativecommons.org/publicdomain/zero/1.0/ */

using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Butterfly.Core.Test;

namespace Butterfly.EmbedIO.Test {
    [TestClass]
    public class EmbedIOTest {
        [TestMethod]
        public async Task EmbedIOChannel() {
            using (var webServer = new Unosquare.Labs.EmbedIO.WebServer("http://+:8000/", Unosquare.Labs.EmbedIO.Constants.RoutingStrategy.Regex))
            using (var subscriptionApi = new EmbedIOSubscriptionApi(webServer, path: "/ws", mustReceiveHeartbeatMillis: 2000, getAuthToken: (authType, authValue) => authValue, getId: authToken => authToken.ToString())) {
                await ChannelUnitTest.TestChannel(subscriptionApi, "ws://localhost:8000/ws", () => {
                    webServer.RunAsync();
                });
            }
        }

        [TestMethod]
        public async Task EmbedIOWeb() {
            using (var webServer = new Unosquare.Labs.EmbedIO.WebServer("http://+:8000/", Unosquare.Labs.EmbedIO.Constants.RoutingStrategy.Regex))
            using (var webApi = new EmbedIOWebApi(webServer)) {
                await WebApiUnitTest.TestWeb(webApi, "http://localhost:8000/", () => {
                    webServer.RunAsync();
                });
            }
        }

    }

}
