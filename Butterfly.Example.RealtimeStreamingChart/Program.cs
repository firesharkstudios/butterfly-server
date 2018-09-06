using System;

using Butterfly.Core.Util;
using Butterfly.EmbedIO;

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
                        var row = GetRow();

                        // You can specify whatever data type you wish to help your clients
                        // understand what they are receiving, just using "data" in this example
                        channel.Queue("data", row);
                    }, 1000);
                });

                embedIOContext.Start();
                ProcessX.OpenBrowser($"http://localhost:{port}/");
                Console.ReadLine();
            }
        }

        // Replace this implementation with something that reads your real-time data
        static object GetRow() {
            return new Dict {
                ["timestamp"] = DateTime.Now.ToUnixTimestamp(),
                ["cpu_core_1"] = random.Next(),
                ["cpu_core_2"] = random.Next()
            };
        }

        /*
         * This would also work
        static object GetRow() {
            return new {
                timestamp = DateTime.Now.ToUnixTimestamp(),
                cpu_core_1 = random.Next(),
                cpu_core_2 = random.Next()
            };
        }

         * This would also work
        public class MyRecord {
            public long timestamp;
            public double cpu_core_1;
            public double cpu_core_2;
        }
        static object GetRow() {
            return new MyRecord {
                timestamp = DateTime.Now.ToUnixTimestamp(),
                cpu_core_1 = random.Next(),
                cpu_core_2 = random.Next()
            };
        }

         * This would also work if...
         * - database was instance of Butterfly.Core.Database.IDatabase
         * - database contained a cpu table with a single row with fields timestamp, cpu_core_1, cpu_core_2
        static Task<object> GetRow() {
            return database.SelectRowAsync("SELECT timestamp, cpu_core_1, cpu_core2 FROM cpu");
        }
        */

    }
}
