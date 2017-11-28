using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Butterfly.Client.DotNet;
using Butterfly.Util;

namespace Butterfly.Channel.Test {
    [TestClass]
    public class ChannelUnitTest {
        [TestMethod]
        public async Task RedHttpServerChannel() {
            var redHttpServer = new RedHttpServerNet45.RedHttpServer(8080);
            using (var channelServer = new Butterfly.RedHttpServer.RedHttpServerChannelServer(redHttpServer)) {

                IChannel newChannel = null;
                channelServer.OnNewChannel("/test", channel => {
                    newChannel = channel;
                    return null;
                });
                channelServer.Start();
                redHttpServer.Start();

                List<string> messageCollector = new List<string>();
                var channelClient = new WebSocketChannelClient("ws://localhost:8080/test/123", json => {
                    var message = JsonUtil.Deserialize<string>(json);
                    messageCollector.Add(message);
                });
                channelClient.Start();
                await Task.Delay(200);
                Assert.IsNotNull(newChannel);

                channelServer.Queue("123", "Hello");
                await Task.Delay(200);
                Assert.AreEqual(1, messageCollector.Count);
                Assert.AreEqual("Hello", messageCollector[0]);
            }
        }
    }
}
