/*
 * Copyright 2017 Fireshark Studios, LLC
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Butterfly.Database.Event;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Database.Test {
    [TestClass]
    [DeploymentItem(@"x86\SQLite.Interop.dll", "x86")]
    public class DatabaseUnitTest {
        [TestMethod]
        public void ParseTableRefs() {
            IDatabase database = new Butterfly.Database.Memory.MemoryDatabase();
            database.CreateFromResourceFile(Assembly.GetExecutingAssembly(), "Butterfly.Database.Test.db.sql");
            var tableRefs = StatementTableRef.ParseTableRefs(database, "employee_contact ec INNER JOIN employee e ON ec.employee_id=e.id AND 1=2 left JOIN department d on e.department_id=d.id and 1=2");
            Assert.AreEqual(3, tableRefs.Length);
            Assert.AreEqual(tableRefs[0].table.Name, "employee_contact");
            Assert.AreEqual(tableRefs[0].tableAlias, "ec");
            Assert.AreEqual(tableRefs[1].table.Name, "employee");
            Assert.AreEqual(tableRefs[1].tableAlias, "e");
            Assert.AreEqual(tableRefs[2].table.Name, "department");
            Assert.AreEqual(tableRefs[2].tableAlias, "d");
        }

        [TestMethod]
        public async Task DataMemoryDatabase() {
            IDatabase database = new Butterfly.Database.Memory.MemoryDatabase();
            await this.TestDatabase(database);
        }

        [TestMethod]
        public async Task DataMySqlDatabase() {
            IDatabase database = new Butterfly.Database.MySql.MySqlDatabase("Server=127.0.0.1;Uid=test;Pwd=test!123;Database=butterfly_test");
            await this.TestDatabase(database);
        }

        [TestMethod]
        public async Task DataPostgresDatabase() {
            IDatabase database = new Butterfly.Database.Postgres.PostgresDatabase("Host=localhost;Username=postgres;Password=test!123;Database=butterfly_test");
            await this.TestDatabase(database);
        }

        [TestMethod]
        public async Task DataSQLiteDatabase() {
            IDatabase database = new Butterfly.Database.SQLite.SQLiteDatabase("butterfly_test.sqlite");
            await this.TestDatabase(database);
        }

        public async Task TestDatabase(IDatabase database) {
            database.CreateFromResourceFile(Assembly.GetExecutingAssembly(), "Butterfly.Database.Test.db.sql");
            database.SetInsertDefaultValue("id", tableName => Guid.NewGuid().ToString(), "employee");
            database.SetInsertDefaultValue("created_at", tableName => DateTime.Now);
            database.SetInsertDefaultValue("updated_at", tableName => DateTime.Now);

            await TruncateData(database);
            await this.TestTransactions(database);

            await TruncateData(database);
            (object salesDepartmentId, object hrDepartmentId, object customerServiceDepartmentId) = await InsertBasicData(database);
            await this.SelectBasicData(database, salesDepartmentId, hrDepartmentId, customerServiceDepartmentId);
            await this.UpdateAndDeleteBasicData(database, hrDepartmentId);
        }

        public static async Task TruncateData(IDatabase database) {
            using (ITransaction transaction = await database.BeginTransactionAsync()) {
                foreach (var tableName in database.Tables.Keys) {
                    await transaction.TruncateAsync(tableName);
                }
                await transaction.CommitAsync();
            }
        }

        public static async Task<(object, object, object)> InsertBasicData(IDatabase database) {
            string salesDepartmentId;
            string hrDepartmentId;
            string customerServiceDepartmentId;
            using (ITransaction transaction = await database.BeginTransactionAsync()) {
                // Add Sales department
                salesDepartmentId = await transaction.InsertAsync<string>("department", new {
                    name = "Sales",
                });
                string mikeEmployeeId = await transaction.InsertAsync<string>("employee", new {
                    name = "Mike in Sales",
                    department_id = salesDepartmentId,
                    birthday = DBNull.Value,
                });
                await transaction.InsertAsync<string>("employee_contact", new {
                    employee_id = mikeEmployeeId,
                    contact_type = "Phone",
                    contact_data = "+18005551000",
                });
                await transaction.InsertAsync<string>("employee_contact", new {
                    employee_id = mikeEmployeeId,
                    contact_type = "Email",
                    contact_data = "mike.sales@butterfly.com",
                });

                // Add HR department using full INSERT statements with individual field parameters
                hrDepartmentId = await transaction.InsertAsync<string>("INSERT INTO department (name) VALUES (@someName)", new {
                    someName = "HR",
                });
                object bobEmployeeId = await transaction.InsertAsync<string>("INSERT INTO employee (name, department_id, birthday) VALUES (@name, @department_id, @birthday)", new {
                    name = "Bob in HR",
                    department_id = hrDepartmentId,
                    birthday = new DateTime(1990, 01, 01),
                });
                await transaction.InsertAsync<string>("INSERT INTO employee_contact (employee_id, contact_type, contact_data) VALUES (@employee_id, @contact_type, @contact_data)", new {
                    employee_id = bobEmployeeId,
                    contact_type = "Phone",
                    contact_data = "+18005551000",
                });
                await transaction.InsertAsync<string>("INSERT INTO employee_contact (employee_id, contact_type, contact_data) VALUES (@employee_id, @contact_type, @contact_data)", new {
                    employee_id = bobEmployeeId,
                    contact_type = "Email",
                    contact_data = "bob.hr@butterfly.com",
                });

                // Add Customer Service department using just table names
                customerServiceDepartmentId = await transaction.InsertAsync<string>("department", new {
                    name = "Customer Service",
                });
                string maryEmployeeId = await transaction.InsertAsync<string>("employee", new {
                    name = "Mary in Customer Service",
                    department_id = customerServiceDepartmentId,
                });
                await transaction.InsertAsync<string>("employee_contact", new {
                    employee_id = maryEmployeeId,
                    contact_type = "Phone",
                    contact_data = "+18005551001",
                });
                await transaction.InsertAsync<string>("employee_contact", new {
                    employee_id = maryEmployeeId,
                    contact_type = "Email",
                    contact_data = "mary.sales@butterfly.com",
                });

                await transaction.CommitAsync();
            }

            return (salesDepartmentId, hrDepartmentId, customerServiceDepartmentId);
        }

        protected async Task TestTransactions(IDatabase database) {
            using (ITransaction transaction = await database.BeginTransactionAsync()) {
                // Add Sales department
                await transaction.InsertAsync<string>("department", new {
                    name = "Sales",
                });

                Dict[] allDepartments1 = await database.SelectRowsAsync("SELECT * FROM department");
                Assert.AreEqual(0, allDepartments1.Length);

                transaction.Rollback();
            }

            Dict[] allDepartments2 = await database.SelectRowsAsync("SELECT * FROM department");
            Assert.AreEqual(0, allDepartments2.Length);

            using (ITransaction transaction = await database.BeginTransactionAsync()) {
                // Add Sales department
                await transaction.InsertAsync<string>("department", new {
                    name = "Sales",
                });

                Dict[] allDepartments1 = await database.SelectRowsAsync("SELECT * FROM department");
                Assert.AreEqual(0, allDepartments1.Length);

                await transaction.CommitAsync();
            }

            Dict[] allDepartments3 = await database.SelectRowsAsync("SELECT * FROM department");
            Assert.AreEqual(1, allDepartments3.Length);
        }

        protected async Task SelectBasicData(IDatabase database, object salesDepartmentId, object hrDepartmentId, object customerServiceDepartmentId) {
            // Test retrieving all departments
            Dict[] allDepartments = await database.SelectRowsAsync("SELECT * FROM department");
            Assert.AreEqual(3, allDepartments.Length);

            // Test retrieving a single row on a non-indexed field (long syntax)
            Dict hrDepartmentA = await database.SelectRowAsync("SELECT * FROM department WHERE name=@name", new {
                name = "HR"
            });
            Assert.IsNotNull(hrDepartmentA);

            // Test retrieving a single row on a non-indexed field (short syntax)
            Dict hrDepartmentB = await database.SelectRowAsync("department", new {
                name = "HR"
            });
            Assert.IsNotNull(hrDepartmentB);

            // Test retrieving rows on an indexed field
            Dict[] someDepartments0 = await database.SelectRowsAsync("SELECT * FROM department WHERE id=@id", new {
                id = salesDepartmentId,
            });
            Assert.AreEqual(1, someDepartments0.Length);

            // Test retrieving a single value on a non-indexed field
            string testHrDepartmentId1 = await database.SelectValueAsync<string>("SELECT id FROM department WHERE name=@name", new {
                name = "HR"
            }, null);
            Assert.AreEqual(hrDepartmentId, testHrDepartmentId1);

            // Test retrieving rows on an indexed field with an empty array
            Dict[] someDepartments1 = await database.SelectRowsAsync("SELECT * FROM department WHERE id=@id", new {
                id = new string[] { },
            });
            Assert.AreEqual(0, someDepartments1.Length);

            // Test retrieving rows on an indexed field with a single value array
            Dict[] someDepartments2 = await database.SelectRowsAsync("SELECT * FROM department WHERE id=@id", new {
                id = new object[] { salesDepartmentId },
            });
            Assert.AreEqual(1, someDepartments2.Length);

            // Test retrieving rows on an indexed field with a two value array
            Dict[] someDepartments3 = await database.SelectRowsAsync("SELECT * FROM department WHERE id=@id", new {
                id = new object[] { salesDepartmentId, hrDepartmentId },
            });
            Assert.AreEqual(2, someDepartments3.Length);

            // Test retrieving rows on an indexed field with a three value array
            Dict[] someDepartments4 = await database.SelectRowsAsync("SELECT * FROM department WHERE id=@id", new {
                id = new object[] { salesDepartmentId, hrDepartmentId, customerServiceDepartmentId },
            });
            Assert.AreEqual(3, someDepartments4.Length);


            // Test retrieving rows on a negated indexed field
            Dict[] someDepartments10 = await database.SelectRowsAsync("SELECT * FROM department WHERE id!=@id", new {
                id = salesDepartmentId,
            });
            Assert.AreEqual(2, someDepartments10.Length);

            // Test retrieving rows on a negated indexed field with an empty array
            Dict[] someDepartments11 = await database.SelectRowsAsync("SELECT * FROM department WHERE id!=@id", new {
                id = new string[] { },
            });
            Assert.AreEqual(3, someDepartments11.Length);

            // Test retrieving rows on an indexed field with a negated single value array
            Dict[] someDepartments12 = await database.SelectRowsAsync("SELECT * FROM department WHERE id!=@id", new {
                id = new object[] { salesDepartmentId },
            });
            Assert.AreEqual(2, someDepartments12.Length);

            // Test retrieving rows on an indexed field with a negated two value array
            Dict[] someDepartments13 = await database.SelectRowsAsync("SELECT * FROM department WHERE id!=@id", new {
                id = new object[] { salesDepartmentId, hrDepartmentId },
            });
            Assert.AreEqual(1, someDepartments13.Length);

            // Test retrieving rows on an indexed field with a negated three value array
            Dict[] someDepartments14 = await database.SelectRowsAsync("SELECT * FROM department WHERE id!=@id", new {
                id = new object[] { salesDepartmentId, hrDepartmentId, customerServiceDepartmentId },
            });
            Assert.AreEqual(0, someDepartments14.Length);


            // Test retrieving all employees
            Dict[] allEmployees = await database.SelectRowsAsync("SELECT * FROM employee");
            Assert.AreEqual(3, allEmployees.Length);

            // Test retrieving employees on date criteria that is null
            Dict[] someEmployees1 = await database.SelectRowsAsync("SELECT * FROM employee WHERE birthday=@birthday", new {
                birthday = DBNull.Value
            });
            Assert.AreEqual(2, someEmployees1.Length);

            // Test retrieving employees on date criteria that is not null
            Dict[] someEmployees2 = await database.SelectRowsAsync("SELECT * FROM employee WHERE birthday!=@birthday", new {
                birthday = DBNull.Value
            });
            Assert.AreEqual(1, someEmployees2.Length);

            // Test retrieving employees on date criteria greater than a date
            Dict[] someEmployees3 = await database.SelectRowsAsync("SELECT * FROM employee WHERE birthday>@birthday", new {
                birthday = new DateTime(1950, 01, 01)
            });
            Assert.AreEqual(1, someEmployees3.Length);

            // Test retrieving employees on date criteria less than a date
            Dict[] someEmployees4 = await database.SelectRowsAsync("SELECT * FROM employee WHERE birthday<@birthday", new {
                birthday = new DateTime(1950, 01, 01)
            });
            Assert.AreEqual(0, someEmployees4.Length);
        }

        protected async Task UpdateAndDeleteBasicData(IDatabase database, object hrDepartmentId) {
            List<DataEventTransaction> dataEventTransactionCollector = new List<DataEventTransaction>();
            using (database.OnNewCommittedTransaction(dataEventTransaction => {
                dataEventTransactionCollector.Add(dataEventTransaction);
            })) {
                // Test updating rows on an indexed field
                dataEventTransactionCollector.Clear();
                int count1 = await database.UpdateAndCommitAsync("department", new {
                    id = hrDepartmentId,
                    name = "Newest HR"
                });
                Assert.AreEqual(1, count1);
                Assert.AreEqual(1, dataEventTransactionCollector.Count);
                Assert.AreEqual(1, dataEventTransactionCollector[0].dataEvents.Length);
                Assert.AreEqual(DataEventType.Update, dataEventTransactionCollector[0].dataEvents[0].dataEventType);

                // Test updating rows on an indexed field
                dataEventTransactionCollector.Clear();
                int count2 = await database.UpdateAndCommitAsync("UPDATE department SET name=@name WHERE id=@id", new {
                    id = hrDepartmentId,
                    name = "Newer HR"
                });
                Assert.AreEqual(1, count2);
                Assert.AreEqual(1, dataEventTransactionCollector.Count);
                Assert.AreEqual(1, dataEventTransactionCollector[0].dataEvents.Length);
                Assert.AreEqual(DataEventType.Update, dataEventTransactionCollector[0].dataEvents[0].dataEventType);

                // Test deleting rows on an indexed field
                dataEventTransactionCollector.Clear();
                int count3 = await database.DeleteAndCommitAsync("DELETE FROM department WHERE id=@id", new {
                    id = hrDepartmentId
                });
                Assert.AreEqual(1, count3);
                Assert.AreEqual(1, dataEventTransactionCollector.Count);
                Assert.AreEqual(1, dataEventTransactionCollector[0].dataEvents.Length);
                Assert.AreEqual(DataEventType.Delete, dataEventTransactionCollector[0].dataEvents[0].dataEventType);

                // Test deleting rows on an indexed field
                dataEventTransactionCollector.Clear();
                int count4 = await database.DeleteAndCommitAsync("department", new {
                    id = hrDepartmentId
                });
                Assert.AreEqual(0, count4);
            }
        }

    }
}
