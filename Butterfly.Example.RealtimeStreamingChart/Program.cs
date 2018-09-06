using System;

using Butterfly.Core.Util;
using Butterfly.EmbedIO;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Example.RealtimeStreamingChart {
    class Program {
        static readonly Random random = new Random();

        static void Main(string[] args) {
            using (var embedIOContext = new EmbedIOContext("http://+:8000/", "../../../www")) {
                // When a client subscribes to "data-feed", create an instance of RunEvery that pushes
                // a new row over the channel every 1000ms
                embedIOContext.SubscriptionApi.OnSubscribe("data-feed", (vars, channel) => {
                    return new RunEvery(() => {
                        var row = GetRow();

                        // You can specify whatever data type you wish to help your clients
                        // understand what they are receiving, just using "data" in this example
                        channel.Queue("data", row);
                    }, 1000);
                });

                embedIOContext.Start();
                ProcessX.OpenBrowser($"http://localhost:8000/");
                Console.ReadLine();
            }
        }

        // Replace this implementation with something that reads your real-time data
        static Dict GetRow() {
            return new Dict {
                ["timestamp"] = DateTime.Now.ToUnixTimestamp(),
                ["cpu_core_1"] = random.Next(),
                ["cpu_core_2"] = random.Next()
            };
        }
    }
}
