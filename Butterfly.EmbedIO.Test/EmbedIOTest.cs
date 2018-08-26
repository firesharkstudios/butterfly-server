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
            using (var webServer = new Unosquare.Labs.EmbedIO.WebServer("http://localhost:8000/", Unosquare.Labs.EmbedIO.Constants.RoutingStrategy.Regex))
            using (var channelServer = new EmbedIOChannelServer(webServer, path: "/ws", mustReceiveHeartbeatMillis: 2000, getAuthToken: (authType, authValue) => authValue, getId: authToken => authToken.ToString())) {
                await ChannelUnitTest.TestChannel(channelServer, "ws://localhost:8000/ws", () => {
                    webServer.RunAsync();
                });
            }
        }

        [TestMethod]
        public async Task EmbedIOWeb() {
            using (var webServer = new Unosquare.Labs.EmbedIO.WebServer("http://localhost:8000/", Unosquare.Labs.EmbedIO.Constants.RoutingStrategy.Regex))
            using (var webApiServer = new EmbedIOWebApiServer(webServer)) {
                await WebApiUnitTest.TestWeb(webApiServer, "http://localhost:8000/", () => {
                    webServer.RunAsync();
                });
            }
        }

    }

}
