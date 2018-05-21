using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Butterfly.Core.Channel.Test;
using Butterfly.WebApi.Test;

namespace Butterfly.EmbedIO.Test {
    [TestClass]
    public class EmbedIOTest {
        [TestMethod]
        public async Task EmbedIOChannel() {
            using (var webServer = new Unosquare.Labs.EmbedIO.WebServer("http://localhost:8080/", Unosquare.Labs.EmbedIO.Constants.RoutingStrategy.Regex))
            using (var channelServer = new EmbedIOChannelServer(webServer, mustReceiveHeartbeatMillis: 2000)) {
                await ChannelUnitTest.TestChannel(channelServer, () => {
                    webServer.RunAsync();
                });
            }
        }

        [TestMethod]
        public async Task EmbedIOWeb() {
            using (var embedIOWebServer = new Unosquare.Labs.EmbedIO.WebServer("http://localhost:8080/", Unosquare.Labs.EmbedIO.Constants.RoutingStrategy.Regex))
            using (var webApiServer = new EmbedIOWebApiServer(embedIOWebServer)) {
                await WebApiUnitTest.TestWeb(webApiServer, "http://localhost:8080/", () => {
                    embedIOWebServer.RunAsync();
                });
            }
        }

    }

}
