/* Any copyright is dedicated to the Public Domain.
 * http://creativecommons.org/publicdomain/zero/1.0/ */

using System;

namespace Butterfly.Example.HelloWorld.Server {
    class Program {
        static void Main(string[] args) {
            using (var embedIOContext = new Butterfly.EmbedIO.EmbedIOContext(port: 8080, staticFullPath: "../../../Butterfly.Example.HelloWorld.Client/")) {
                // Create a MemoryDatabase (no persistence, limited features)
                var database = new Butterfly.Core.Database.Memory.MemoryDatabase();
                database.CreateFromText(@"CREATE TABLE message (
	                id INT NOT NULL AUTO_INCREMENT,
	                text VARCHAR(40) NOT NULL,
	                PRIMARY KEY (id)
                );");

                // Listen for API requests
                embedIOContext.WebApiServer.OnPost("/api/message/insert", async (req, res) => {
                    var text = await req.ParseAsJsonAsync<dynamic>();
                    await database.InsertAndCommitAsync<long>("message", new {
                        text
                    });
                });

                // Listen for subscribe requests...
                // - The handler must return an IDisposable object (gets disposed when the channel is unsubscribed)
                // - The handler can push data to the client by calling channel.Queue()
                embedIOContext.ChannelServer.OnSubscribe("my-channel", (vars, channel) => {
                    return database.CreateAndStartDynamicView("message", dataEventTransaction => channel.Queue(dataEventTransaction));
                });

                embedIOContext.Start();

                Console.WriteLine($"Open http://localhost:{8080}/ in a browser");
                Console.ReadLine();
            }
        }

    }
}
