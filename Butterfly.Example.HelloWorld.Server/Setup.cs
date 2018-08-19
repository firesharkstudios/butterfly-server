using NLog;

using Butterfly.Core.Channel;
using Butterfly.Core.WebApi;
using Butterfly.Core.Database;

namespace Butterfly.Example.HelloWorld.Server {
    class Setup {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static void Init(IDatabase database, IWebApiServer webApiServer, IChannelServer channelServer) {
            // Listen for API requests
            webApiServer.OnPost("/api/message/insert", async (req, res) => {
                var text = await req.ParseAsJsonAsync<dynamic>();
                await database.InsertAndCommitAsync<long>("message", new {
                    text
                });
            });

            // Listen for subscribe requests...
            // - The handler must return an IDisposable object (gets disposed when the channel is unsubscribed)
            // - The handler can push data to the client by calling channel.Queue()
            channelServer.OnSubscribe("my-channel", (vars, channel) => {
                return database.CreateAndStartDynamicView("message", dataEventTransaction => channel.Queue(dataEventTransaction));
            });
        }

    }
}
