using NLog;

using Butterfly.Core.Channel;
using Butterfly.Core.WebApi;
using Butterfly.Core.Database;

namespace Butterfly.Example.HelloWorld.Server {
    class Setup {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static void Init(IDatabase database, IWebApiServer webApiServer, IChannelServer channelServer) {
            // Listen for API requests
            webApiServer.OnPost($"/api/message/insert", async (req, res) => {
                var text = await req.ParseAsJsonAsync<dynamic>();
                await database.InsertAndCommitAsync<long>("message", new {
                    text
                });
            });

            // Listen for websocket connections to /ws
            var route = channelServer.RegisterRoute("/ws");

            // Register a channel that creates a DynamicView on the message table 
            // (sends all the initial data in the message table and sends changes to the message table)
            route.RegisterChannel(
                channelKey: "my-channel", 
                handlerAsync: async (vars, channel) => await database.CreateAndStartDynamicView(
                    "message",
                    listener: dataEventTransaction => channel.Queue(dataEventTransaction)
                )
            );
        }

    }
}
