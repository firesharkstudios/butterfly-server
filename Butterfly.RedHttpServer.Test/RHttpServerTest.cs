/* Any copyright is dedicated to the Public Domain.
 * http://creativecommons.org/publicdomain/zero/1.0/ */

using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Red;

using Butterfly.Core.Test;

namespace Butterfly.RHttpServer.Test {
    [TestClass]
    public class RHttpServerTest {
        [TestMethod]
        public async Task ChannelTest() {
            var webServer = new RedHttpServer(8000);
            using (var subscriptionApi = new RedHttpServerSubscriptionApi(webServer, path: "/ws", mustReceiveHeartbeatMillis: 2000, getAuthToken: (authType, authValue) => authValue, getId: authToken => authToken.ToString())) {
                await ChannelUnitTest.TestChannel(subscriptionApi, "ws://localhost:8000/ws", () => {
                    webServer.RunAsync();
                });
            }
        }

        [TestMethod]
        public async Task WebTest() {
            var webServer = new RedHttpServer(8000);
            using (var webApi = new RedHttpServerWebApi(webServer)) {
                await WebApiUnitTest.TestWeb(webApi, "http://localhost:8000/", () => {
                    webServer.RunAsync();
                });
            }
        }
    }
}
