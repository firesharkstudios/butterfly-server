using System;

using Butterfly.Util;
using Butterfly.Web;
using Butterfly.Web.WebApi;
using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace MyCrudApp {
    class Program {
        static void Main(string[] args) {
            using (var ctx =
              new Butterfly.Web.EmbedIO.EmbedIOContext("http://+:8000/")) {
                // Create database
                var database = new Butterfly.Db.Memory.MemoryDatabase();
                database.CreateFromSqlAsync(@"CREATE TABLE contact (
                 id VARCHAR(50) NOT NULL,
                 first_name VARCHAR(40) NOT NULL,
                 last_name VARCHAR(40) NOT NULL,
                 PRIMARY KEY(id)
                );").Wait();
                database.SetDefaultValue("id",
                  table => $"{table.Abbreviate()}_{Guid.NewGuid().ToString()}"
                );

                // Define the Web API
                ctx.WebApi.OnPost("/api/contact/insert", async (req, res) => {
                    var record = await req.ParseAsJsonAsync<Dict>();
                    await database.InsertAndCommitAsync<string>("contact", record);
                });
                ctx.WebApi.OnPost("/api/contact/update", async (req, res) => {
                    var record = await req.ParseAsJsonAsync<Dict>();
                    await database.UpdateAndCommitAsync("contact", record);
                });
                ctx.WebApi.OnPost("/api/contact/delete", async (req, res) => {
                    var id = await req.ParseAsJsonAsync<string>();
                    await database.DeleteAndCommitAsync("contact", id);
                });

                // Define the Subscription API
                ctx.SubscriptionApi.OnSubscribe(
                    "all-contacts",
                    (vars, channel) => database.CreateAndStartDynamicViewAsync(
                        "SELECT * FROM contact",
                        dataEventTransaction => channel.Queue(dataEventTransaction)
                    )
                );

                // Start the web server and wait        
                ctx.Start();
                Console.ReadLine();
            }
        }
    }
}