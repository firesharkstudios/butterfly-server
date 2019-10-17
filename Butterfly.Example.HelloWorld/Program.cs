/* Any copyright is dedicated to the Public Domain.
 * http://creativecommons.org/publicdomain/zero/1.0/ */

using System;
using System.Threading.Tasks;

using Butterfly.Util;
using Butterfly.Web;

namespace Butterfly.Example.HelloWorld {
    class Program {
        // Using async Task Main() requires adding <LangVersion>latest</LangVersion> to .csproj file
        static async Task Main(string[] args) {
            const int port = 8000;
            var wwwPath = args.Length > 0 ? args[0] : "../../../www";
            using (var embedIOContext = new Butterfly.Web.EmbedIO.EmbedIOContext($"http://+:{port}/", wwwPath)) {
                // Create a MemoryDatabase (no persistence, limited features)
                var database = new Butterfly.Db.Memory.MemoryDatabase();
                await database.CreateFromSqlAsync(@"CREATE TABLE message (
	                id INT NOT NULL AUTO_INCREMENT,
	                text VARCHAR(40) NOT NULL,
	                PRIMARY KEY (id)
                );");

                // Listen for API requests
                embedIOContext.WebApi.OnPost("/api/message/insert", async (req, res) => {
                    var text = await req.ParseAsJsonAsync<dynamic>();
                    await database.InsertAndCommitAsync<long>("message", new {
                        text
                    });
                });

                // Listen for subscribe requests...
                // - The handler must return an IDisposable object (gets disposed when the channel is unsubscribed)
                // - The handler can push data to the client by calling channel.Queue()
                embedIOContext.SubscriptionApi.OnSubscribe("my-channel", (vars, channel) => {
                    return database.CreateAndStartDynamicViewAsync("message", dataEventTransaction => channel.Queue(dataEventTransaction));
                });

                embedIOContext.Start();

                Console.WriteLine($"Opening http://localhost:{port}/ in a browser...");
                ProcessX.OpenBrowser($"http://localhost:{port}/");
                Console.ReadLine();
            }
        }

    }
}
