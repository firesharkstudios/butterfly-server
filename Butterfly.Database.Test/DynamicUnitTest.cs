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
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Butterfly.Database.Dynamic;
using Butterfly.Database.Event;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Database.Test {
    [TestClass]
    public class DynamicUnitTest {
        [TestMethod]
        public async Task DynamicMemoryDatabase() {
            BaseDatabase database = new Butterfly.Database.Memory.MemoryDatabase();
            await this.TestDatabase(database);
        }

        [TestMethod]
        public async Task DynamicMySqlDatabase() {
            BaseDatabase database = new Butterfly.Database.MySql.MySqlDatabase("Server=127.0.0.1;Uid=test;Pwd=test!123;Database=butterfly_test");
            await this.TestDatabase(database);
        }

        [TestMethod]
        public async Task DynamicPostgresDatabase() {
            BaseDatabase database = new Butterfly.Database.Postgres.PostgresDatabase("Host=localhost;Username=postgres;Password=test!123;Database=butterfly_test");
            await this.TestDatabase(database);
        }

        [TestMethod]
        public async Task DynamicSQLiteDatabase() {
            BaseDatabase database = new Butterfly.Database.SQLite.SQLiteDatabase("butterfly_test.sqlite");
            await this.TestDatabase(database);
        }

        public async Task TestDatabase(BaseDatabase database) {
            database.CreateFromResourceFileAsync(Assembly.GetExecutingAssembly(), "Butterfly.Database.Test.db.sql").Wait();
            database.SetDefaultValue("id", () => Guid.NewGuid().ToString(), "employee");
            database.SetDefaultValue("created_at", () => DateTime.Now);
            database.SetDefaultValue("updated_at", () => DateTime.Now);

            await this.TruncateData(database);
            (object salesDepartmentId, object hrDepartmentId, object customerServiceDepartmentId) = await this.InsertBasicData(database);
            await this.TestInsertUpdateDeleteEvents(database, salesDepartmentId, "SELECT * FROM employee", "name", "Joe Sales, Jr", 1);
            await this.TestInsertUpdateDeleteEvents(database, salesDepartmentId,"SELECT id, name FROM employee", "department_id", -1, 0);
        }

        protected async Task TruncateData(BaseDatabase database) {
            using (ITransaction transaction = await database.BeginTransaction()) {
                foreach (var tableName in database.Tables.Keys) {
                    await transaction.TruncateAsync(tableName);
                }
                await transaction.CommitAsync();
            }
        }

        protected async Task<(object, object, object)> InsertBasicData(BaseDatabase database) {
            object salesDepartmentId;
            object hrDepartmentId;
            object customerServiceDepartmentId;
            using (ITransaction transaction = await database.BeginTransaction()) {
                // Add Sales department using full INSERT statements with @@names and @@values
                salesDepartmentId = await transaction.InsertAsync("INSERT INTO department (@@names) VALUES (@@values)", new {
                    name = "Sales",
                });
                object mikeEmployeeId = await transaction.InsertAsync("INSERT INTO employee (@@names) VALUES (@@values)", new {
                    name = "Mike in Sales",
                    department_id = salesDepartmentId,
                    birthday = DBNull.Value,
                });
                await transaction.InsertAsync("INSERT INTO employee_contact (@@names) VALUES (@@values)", new {
                    employee_id = mikeEmployeeId,
                    contact_type = "Phone",
                    contact_data = "+18005551000",
                });
                await transaction.InsertAsync("INSERT INTO employee_contact (@@names) VALUES (@@values)", new {
                    employee_id = mikeEmployeeId,
                    contact_type = "Email",
                    contact_data = "mike.sales@butterfly.com",
                });

                // Add HR department using full INSERT statements with individual field parameters
                hrDepartmentId = await transaction.InsertAsync("INSERT INTO department (name) VALUES (@name)", new {
                    name = "HR",
                });
                object bobEmployeeId = await transaction.InsertAsync("INSERT INTO employee (name, department_id) VALUES (@name, @department_id)", new {
                    name = "Bob in HR",
                    department_id = hrDepartmentId,
                    birthday = new DateTime(1990, 01, 01),
                });
                await transaction.InsertAsync("INSERT INTO employee_contact (employee_id, contact_type, contact_data) VALUES (@employee_id, @contact_type, @contact_data)", new {
                    employee_id = bobEmployeeId,
                    contact_type = "Phone",
                    contact_data = "+18005551000",
                });
                await transaction.InsertAsync("INSERT INTO employee_contact (employee_id, contact_type, contact_data) VALUES (@employee_id, @contact_type, @contact_data)", new {
                    employee_id = bobEmployeeId,
                    contact_type = "Email",
                    contact_data = "bob.hr@butterfly.com",
                });

                // Add Customer Service department using just table names
                customerServiceDepartmentId = await transaction.InsertAsync("department", new {
                    name = "Customer Service",
                });
                object maryEmployeeId = await transaction.InsertAsync("employee", new {
                    name = "Mary in Customer Service",
                    department_id = customerServiceDepartmentId,
                });
                await transaction.InsertAsync("employee_contact", new {
                    employee_id = maryEmployeeId,
                    contact_type = "Phone",
                    contact_data = "+18005551001",
                });
                await transaction.InsertAsync("employee_contact", new {
                    employee_id = maryEmployeeId,
                    contact_type = "Email",
                    contact_data = "mary.sales@butterfly.com",
                });

                await transaction.CommitAsync();
            }

            return (salesDepartmentId, hrDepartmentId, customerServiceDepartmentId);
        }

        public async Task TestInsertUpdateDeleteEvents(BaseDatabase database, object salesDepartmentId, string selectSourceSql, string updateField, object updateValue, int updateCount) {
            List<DataEventTransaction> dataEventTransactionCollector = new List<DataEventTransaction>();
            using (DynamicViewSet dynamicViewSet = new DynamicViewSet(database, listener: dataEventTransaction => {
                dataEventTransactionCollector.Add(dataEventTransaction);
            })) {
                dataEventTransactionCollector.Clear();
                DynamicView employeeDynamicView = dynamicViewSet.CreateDynamicView(selectSourceSql);
                await dynamicViewSet.StartAsync();
                Assert.AreEqual(1, dataEventTransactionCollector.Count);
                Assert.AreEqual(5, dataEventTransactionCollector[0].dataEvents.Length);
                Assert.AreEqual(DataEventType.InitialBegin, dataEventTransactionCollector[0].dataEvents[0].dataEventType);
                Assert.AreEqual(DataEventType.Initial, dataEventTransactionCollector[0].dataEvents[1].dataEventType);
                Assert.AreEqual(DataEventType.Initial, dataEventTransactionCollector[0].dataEvents[2].dataEventType);
                Assert.AreEqual(DataEventType.Initial, dataEventTransactionCollector[0].dataEvents[3].dataEventType);
                Assert.AreEqual(DataEventType.InitialEnd, dataEventTransactionCollector[0].dataEvents[4].dataEventType);

                // Confirm that an insert event is created
                dataEventTransactionCollector.Clear();
                object joeSalesEmployeeId;
                using (ITransaction transaction = await database.BeginTransaction()) {
                    // Add Joe Sales employee
                    joeSalesEmployeeId = await transaction.InsertAsync("INSERT INTO employee (@@names) VALUES (@@values)", new {
                        name = "Joe Sales",
                        department_id = salesDepartmentId,
                    });
                    await transaction.CommitAsync();
                }
                await Task.Delay(50);
                Assert.AreEqual(1, dataEventTransactionCollector.Count);
                Assert.AreEqual(1, dataEventTransactionCollector[0].dataEvents.Length);
                Assert.AreEqual(DataEventType.Insert, dataEventTransactionCollector[0].dataEvents[0].dataEventType);

                // Confirm that an update event is created
                dataEventTransactionCollector.Clear();
                using (ITransaction transaction = await database.BeginTransaction()) {
                    // Update Joe Sales employee
                    await transaction.UpdateAsync($"UPDATE employee SET {updateField}=@{updateField} WHERE id=@id", new Dict {
                        ["id"] = joeSalesEmployeeId,
                        [updateField] = updateValue
                    });
                    await transaction.CommitAsync();
                }
                await Task.Delay(50);
                Assert.AreEqual(updateCount, dataEventTransactionCollector.Count);
                if (updateCount > 0) {
                    Assert.AreEqual(1, dataEventTransactionCollector[0].dataEvents.Length);
                    Assert.AreEqual(DataEventType.Update, dataEventTransactionCollector[0].dataEvents[0].dataEventType);
                    Assert.AreEqual(updateValue, (dataEventTransactionCollector[0].dataEvents[0] as ChangeDataEvent).record[updateField]);
                }

                // Confirm that a delete event is created
                dataEventTransactionCollector.Clear();
                using (ITransaction transaction = await database.BeginTransaction()) {
                    // Delete Joe Sales employee
                    await transaction.DeleteAsync("DELETE FROM employee WHERE id=@id", new {
                        id = joeSalesEmployeeId,
                    });
                    await transaction.CommitAsync();
                }
                await Task.Delay(50);
                Assert.AreEqual(1, dataEventTransactionCollector.Count);
                Assert.AreEqual(1, dataEventTransactionCollector[0].dataEvents.Length);
                Assert.AreEqual(DataEventType.Delete, dataEventTransactionCollector[0].dataEvents[0].dataEventType);
            }
        }

    }
}
