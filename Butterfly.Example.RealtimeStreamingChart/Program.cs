using System;

using Butterfly.Util;
using Butterfly.Web.EmbedIO;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Example.RealtimeStreamingChart {
    class Program {
        static readonly Random random = new Random();

        static void Main(string[] args) {
            const int port = 8000;
            var wwwPath = args.Length > 0 ? args[0] : "../../../www";
            using (var embedIOContext = new EmbedIOContext($"http://+:{port}/", wwwPath)) {
                // When a client subscribes to "data-feed", create an instance of RunEvery that pushes
                // a new row over the channel every 1000ms
                embedIOContext.SubscriptionApi.OnSubscribe("data-feed", (vars, channel) => {
                    return new RunEvery(() => {
                        var row = new Dict {
                            ["timestamp"] = DateTime.Now.ToUnixTimestamp(),
                            ["cpu_core_1"] = random.Next(),
                            ["cpu_core_2"] = random.Next()
                        };

                        // You can specify whatever data type you wish to help your clients
                        // understand what they are receiving, just using "data" in this example
                        channel.Queue("data", row);
                    }, 1000);
                });

                embedIOContext.Start();
                Console.ReadLine();
            }
        }

    }
}
