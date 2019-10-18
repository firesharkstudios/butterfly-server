/* Any copyright is dedicated to the Public Domain.
 * http://creativecommons.org/publicdomain/zero/1.0/ */

using System;
using System.Linq;
using System.Threading.Tasks;

using NLog;

using Butterfly.Util;
using Butterfly.Web;
using Butterfly.Web.WebApi;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Example.Todos {
    class Program {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        // Using async Task Main() requires adding <LangVersion>latest</LangVersion> to .csproj file
        static async Task Main(string[] args) {
            logger.Info("Main()");
            using (var embedIOContext = new Butterfly.Web.EmbedIO.EmbedIOContext("http://+:8000/")) {
                // Create a MemoryDatabase (no persistence, limited features)
                var database = new Butterfly.Db.Memory.MemoryDatabase();
                await database.CreateFromSqlAsync(@"CREATE TABLE todo (
	                id VARCHAR(50) NOT NULL,
	                name VARCHAR(40) NOT NULL,
	                PRIMARY KEY(id)
                );");
                database.SetDefaultValue("id", tableName => $"{tableName.Abbreviate()}_{Guid.NewGuid().ToString()}");

                // Listen for API requests
                embedIOContext.WebApi.OnPost("/api/todo/insert", async (req, res) => {
                    logger.Info("/api/todo/insert");
                    var todo = await req.ParseAsJsonAsync<Dict>();
                    await database.InsertAndCommitAsync<string>("todo", todo);
                });
                embedIOContext.WebApi.OnPost("/api/todo/delete", async (req, res) => {
                    logger.Info("/api/todo/delete");
                    var id = await req.ParseAsJsonAsync<string>();
                    await database.DeleteAndCommitAsync("todo", id);
                });

                // Listen for subscribe requests...
                // - The handler must return an IDisposable object (gets disposed when the channel is unsubscribed)
                // - The handler can push data to the client by calling channel.Queue()
                embedIOContext.SubscriptionApi.OnSubscribe("todos", (vars, channel) => {
                    string clientName = vars?.GetAs("clientName", "");
                    logger.Info($"OnSubscribe():todos,clientName={clientName}");
                    return database.CreateAndStartDynamicViewAsync("todo", dataEventTransaction => {
                        var eventTypes = string.Join(",", dataEventTransaction.dataEvents.Select(x => x.dataEventType.ToString()));
                        logger.Info($"clientName={clientName},eventTypes={eventTypes}");
                        channel.Queue(dataEventTransaction);
                    });
                });

                embedIOContext.Start();

                Console.ReadLine();
            }
        }
    }
}
