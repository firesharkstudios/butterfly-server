using System;
using System.Reflection;

using NLog;

using Butterfly.Core.Channel;
using Butterfly.Core.Database;
using Butterfly.Core.Util;
using Butterfly.Core.WebApi;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Example.HelloWorld.Server {
    public class Setup {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static void Init(IDatabase database, IWebApiServer webApiServer, IChannelServer channelServer) {
            // Setup database
            database.CreateFromText(@"CREATE TABLE todo (
	            id VARCHAR(50) NOT NULL,
	            name VARCHAR(40) NOT NULL,
	            PRIMARY KEY(id)
            );");
            database.SetDefaultValue("id", tableName => $"{tableName.Abbreviate()}_{Guid.NewGuid().ToString()}");

            // Listen for API requests
            webApiServer.OnPost("/api/todo/insert", async (req, res) => {
                var todo = await req.ParseAsJsonAsync<Dict>();
                await database.InsertAndCommitAsync<string>("todo", todo);
            });
            webApiServer.OnPost("/api/todo/delete", async (req, res) => {
                var id = await req.ParseAsJsonAsync<string>();
                await database.DeleteAndCommitAsync("todo", id);
            });

            // Listen for subscribe requests...
            // - The handler must return an IDisposable object (gets disposed when the channel is unsubscribed)
            // - The handler can push data to the client by calling channel.Queue()
            channelServer.OnSubscribe("todos", (vars, channel) => {
                return database.CreateAndStartDynamicView("todo", dataEventTransaction => channel.Queue(dataEventTransaction));
            });
        }

    }
}
