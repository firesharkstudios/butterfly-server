using NLog;

using Butterfly.Core.Channel;
using Butterfly.Core.Database;
using Butterfly.Core.WebApi;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Example.HelloWorld.Server {
    public class Setup {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static void Init(IDatabase database, IWebApiServer webApiServer, IChannelServer channelServer) {
            // Listen for API requests
            webApiServer.OnPost($"/api/todo/insert", async (req, res) => {
                var todo = await req.ParseAsJsonAsync<Dict>();
                await database.InsertAndCommitAsync<string>("todo", todo);
            });
            webApiServer.OnPost($"/api/todo/delete", async (req, res) => {
                var id = await req.ParseAsJsonAsync<string>();
                await database.DeleteAndCommitAsync("todo", id);
            });

            // Listen for websocket connections to /ws
            var route = channelServer.RegisterRoute("/ws");

            // Register a channel that creates a DynamicView on the todo table 
            // (sends all the initial data in the todo table and sends changes to the todo table)
            route.RegisterChannel(
                channelKey: "todos", 
                handlerAsync: async (vars, channel) => await database.CreateAndStartDynamicView(
                    "todo",
                    listener: dataEventTransaction => channel.Queue(dataEventTransaction)
                )
            );
        }

    }
}
