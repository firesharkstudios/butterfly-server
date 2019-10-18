/* Any copyright is dedicated to the Public Domain.
 * http://creativecommons.org/publicdomain/zero/1.0/ */

using System;
using System.Threading.Tasks;

using Butterfly.Db;
using Butterfly.Db.Dynamic;
using Butterfly.Util;

namespace Butterfly.Example.Database {
    class Program {
        /*
         * Requires creating a MySQL database like...
         *   CREATE DATABASE butterfly_db_demo;
         *   USE butterfly_db_demo;
         *   CREATE TABLE user (
         *     id VARCHAR(50) NOT NULL,
         *     name VARCHAR(40) NOT NULL,
         *     PRIMARY KEY(id)
         *   );
         *   CREATE TABLE todo (
         *     id VARCHAR(50) NOT NULL,
         *     name VARCHAR(40) NOT NULL,
         *     user_id VARCHAR(50) NOT NULL,
         *     is_done VARCHAR(1) NOT NULL,
         *     PRIMARY KEY(id),
         *     KEY user_id (user_id)
         *   );
         *   GRANT ALL PRIVILEGES ON butterfly_db_demo.* TO 'test'@'localhost' IDENTIFIED BY 'test!123';
         */
        static void Main(string[] args) {
            Console.WriteLine("You must create a 'butterfly_db_demo' database using the SQL in the comments of Program.cs and provide the appropriate connect string to run this demo.  Press any ENTER to continue.");
            Console.ReadLine();

            // Get a reference to a MySQL database (fix the connect string for your environment)
            var database = new Butterfly.Db.MySql.MySqlDatabase("Server=127.0.0.1;Uid=test;Pwd=test!123;Database=butterfly_db_demo");

            // Create ids like u_fffa58f2-ccd6-433b-91f0-d708e7ecd205
            database.SetDefaultValue("id", tableName => $"{tableName.Abbreviate()}_{Guid.NewGuid().ToString()}");

            Run(database);
            Console.ReadLine();
        }

        static async Task Run(IDatabase database) {
            // Create sample data
            Console.WriteLine("Creating sample data...");
            string spongebobId, patrickId;
            string todo1Id, todo2Id, todo3Id, todo4Id;
            using (var transaction = await database.BeginTransactionAsync()) {
                await transaction.TruncateAsync("user");
                await transaction.TruncateAsync("todo");

                // Create Spongebob and Patrick user records
                spongebobId = await transaction.InsertAsync<string>("user", new {
                    name = "Spongebob",
                });

                patrickId = await transaction.InsertAsync<string>("user", new {
                    name = "Patrick",
                });

                // Create some todo records
                todo1Id = await transaction.InsertAsync<string>("todo", new {
                    name = "Todo #1",
                    user_id = spongebobId,
                    is_done = "N",
                });

                todo2Id = await transaction.InsertAsync<string>("todo", new {
                    name = "Todo #2",
                    user_id = spongebobId,
                    is_done = "N",
                });

                todo3Id = await transaction.InsertAsync<string>("todo", new {
                    name = "Todo #3",
                    user_id = spongebobId,
                    is_done = "Y",
                });

                todo4Id = await transaction.InsertAsync<string>("todo", new {
                    name = "Todo #4",
                    user_id = patrickId,
                    is_done = "N",
                });

                // Don't forget to commit the changes
                await transaction.CommitAsync();
            }

            // Create a DynamicViewSet that...
            // - Uses a SELECT statement to define a resultset joining the todo and user tables filtered by isDone="N"
            // - Echoes all data events to the Console
            // Note that all the initial matching records are echoed to the Console as Initial events.
            using (DynamicViewSet dynamicViewSet = await CreateDynamicViewSet(database, "N")) {

                // This will echo an Insert data event to the Console because it matches the SELECT criteria
                Console.WriteLine("Inserting Task #5...");
                await database.InsertAndCommitAsync<string>("todo", new {
                    name = "Task #5",
                    user_id = spongebobId,
                    is_done = "N",
                });
                await Task.Delay(2500);

                // This will NOT echo an Insert data event to the Console because it does NOT match the SELECT criteria
                Console.WriteLine("Inserting Task #6...");
                await database.InsertAndCommitAsync<string>("todo", new {
                    name = "Task #6",
                    user_id = spongebobId,
                    is_done = "Y",
                });
                await Task.Delay(2500);

                // This will echo an Update data event to the Console because it matches the SELECT criteria
                Console.WriteLine("Updating task name to 'Updated Task #1'...");
                await database.UpdateAndCommitAsync("todo", new {
                    id = todo1Id,
                    name = "Updated Task #1"
                });
                await Task.Delay(2500);

                // This will echo multiple Update data events to the Console because it impacts multiple records in the resultset
                // Yes, that's pretty cool :)
                Console.WriteLine("Updating user name to 'Mr. Spongebob'");
                await database.UpdateAndCommitAsync("user", new {
                    id = spongebobId,
                    name = "Mr. Spongebob"
                });
                await Task.Delay(2500);
            }
        }

        static Task<DynamicViewSet> CreateDynamicViewSet(IDatabase database, string isDoneFilter) {
            Console.WriteLine("Creating DynamicView...");
            return database.CreateAndStartDynamicViewAsync(
                @"SELECT t.id, t.name todo_name, u.name user_name
                FROM todo t 
                    INNER JOIN user u ON t.user_id=u.id
                WHERE is_done=@isDoneFilter",
                dataEvents => {
                    var json = JsonUtil.Serialize(dataEvents, format: true);
                    Console.WriteLine($"dataEvents={json}");
                },
                new {
                    isDoneFilter
                }
            );
        }
    }
}
