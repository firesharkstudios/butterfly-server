using System;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Butterfly.Auth.Test {
    [TestClass]
    public class AuthTest {
        [TestMethod]
        public async Task SimpleAuthTest() {
            //var database = new Butterfly.Database.Memory.MemoryDatabase();
            var database = new Butterfly.Database.MySql.MySqlDatabase("Server=127.0.0.1;Uid=test;Pwd=test!123;Database=butterfly_auth_test");

            database.CreateFromResourceFile(Assembly.GetExecutingAssembly(), "Butterfly.Auth.Test.db.sql");
            database.SetInsertDefaultValue("id", tableName => Guid.NewGuid().ToString());
            database.SetInsertDefaultValue("created_at", tableName => DateTime.Now);
            database.SetInsertDefaultValue("updated_at", tableName => DateTime.Now);

            AuthManager authManager = new AuthManager(database);
            AuthToken registerAuthToken = await authManager.RegisterAsync(new {
                username = "johnsmith",
                first_name = "John",
                last_name = "Smith",
                email = "john@fireshark.com",
                password = "test123"
            });
            AuthToken authenticateAuthToken = await authManager.Authenticate(registerAuthToken.id);
        }
    }
}
