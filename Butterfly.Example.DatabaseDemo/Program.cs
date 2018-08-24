using System;
using System.Threading.Tasks;

using NLog;

using Butterfly.Core.Database;
using Butterfly.Core.Util;

namespace Butterfly.Example.DatabaseDemo {
    /*
     * The database layer is perhaps the most interesting part of Butterfly Server .NET.
     * This simple demo...
     * 1) Creates a Memory database
     * 2) Populates the database with sample data
     * 3) Creates a DynamicQuery
     * 4) Shows how various changes to the database impact the DynamicQuery
     */
    class Program {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args) {
            var database = new Butterfly.Core.Database.Memory.MemoryDatabase();
            //var database = new Butterfly.MySql.MySqlDatabase("Server=127.0.0.1;Uid=test;Pwd=test!123;Database=butterfly_test");

            var task = Run(database);
            Console.ReadLine();
        }

        static async Task Run(IDatabase database) {
            database.CreateFromText(@"CREATE TABLE user_todo (
	            id VARCHAR(50) NOT NULL,
	            name VARCHAR(40) NOT NULL,
                username VARCHAR(25) NOT NULL,
	            PRIMARY KEY(id),
                INDEX username (username)
            );");

            // Create ids like ut_fffa58f2-ccd6-433b-91f0-d708e7ecd205
            database.SetDefaultValue("id", tableName => $"{tableName.Abbreviate()}_{Guid.NewGuid().ToString()}");

            // First, let's add some sample records
            string task1Id;
            string task2Id;
            string task3Id;
            using (ITransaction transaction = await database.BeginTransactionAsync()) {
                task1Id = await transaction.InsertAsync<string>("user_todo", new {
                    name = "Task #1",
                    username = "spongebob"
                });

                task2Id = await transaction.InsertAsync<string>("user_todo", new {
                    name = "Task #2",
                    username = "spongebob"
                });

                task3Id = await transaction.InsertAsync<string>("user_todo", new {
                    name = "Task #3",
                    username = "patrick"
                });

                await transaction.CommitAsync();
            }

            // Next, let's create a DynamicView that will echo the initial matching records
            // to the Console and will also echo any changes to the records to the Console
            logger.Info("Creating the DynamicView...");
            var dynamicViewSet = database.CreateAndStartDynamicView(
                "SELECT * FROM user_todo WHERE username=@username",
                dataEventTransaction => {
                    var json = JsonUtil.Serialize(dataEventTransaction, format: true);
                    logger.Info(json);
                },
                new {
                    username = "spongebob"
                }
            );

            // Next, let's INSERT a record that matches our resultset criteria above
            // (this will trigger the DynamicView handler above to write the change to the Console)
            logger.Info("Inserting a matching record...");
            await database.InsertAndCommitAsync<string>("user_todo", new {
                name = "Task #4",
                username = "spongebob"
            });

            // Next, let's INSERT a record that does NOT match our resultset criteria
            // (this will NOT trigger the DynamicView handler above to write the change to the Console)
            logger.Info("Inserting a non-matching record...");
            await database.InsertAndCommitAsync<string>("user_todo", new {
                name = "Task #5",
                username = "patrick"
            });

            // Next, let's UPDATE a record that matches our resultset criteria above
            // (this will trigger the DynamicView handler above to write the change to the Console)
            logger.Info("Updating a matching record...");
            await database.UpdateAndCommitAsync("user_todo", new {
                id = task1Id,
                name = "Updated Task #1"
            });

            // Next, let's UPDATE a record that does NOT match our resultset criteria above
            // (this will NOT trigger the DynamicView handler above to write the change to the Console)
            logger.Info("Updating a non-matching record...");
            await database.UpdateAndCommitAsync("user_todo", new {
                id = task3Id,
                name = "Updated Task #3"
            });

            // Discard the DynamicView
            dynamicViewSet.Dispose();

            // This demo used a single table query; however, multiple table queries with 
            // INNER and LEFT joins are also supported 
            // (requires a database supporting joins like MySQL, Postgres, SQLite, etc) 
        }
    }
}
