/* Any copyright is dedicated to the Public Domain.
 * http://creativecommons.org/publicdomain/zero/1.0/ */

using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Red;

using Butterfly.Core.Test;
using Butterfly.RHttpServer;

namespace Butterfly.EmbedIO.Test {
    [TestClass]
    public class EmbedIOTest {
        [TestMethod]
        public async Task EmbedIOChannel() {
            var webServer = new RedHttpServer(8000);
            using (var subscriptionApi = new RedHttpServerSubscriptionApi(webServer, path: "/ws", mustReceiveHeartbeatMillis: 2000, getAuthToken: (authType, authValue) => authValue, getId: authToken => authToken.ToString())) {
                await ChannelUnitTest.TestChannel(subscriptionApi, "ws://localhost:8000/ws", () => {
                    webServer.RunAsync();
                });
            }
        }

        [TestMethod]
        public async Task EmbedIOWeb() {
            var webServer = new RedHttpServer(8000);
            using (var webApi = new RedHttpServerWebApi(webServer)) {
                await WebApiUnitTest.TestWeb(webApi, "http://localhost:8000/", () => {
                    webServer.RunAsync();
                });
            }
        }
    }
}
