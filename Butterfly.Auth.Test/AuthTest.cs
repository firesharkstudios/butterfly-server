using System;
using System.Reflection;
using System.Threading.Tasks;
using Butterfly.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Butterfly.Auth.Test {
    [TestClass]
    public class AuthTest {
        [TestMethod]
        public async Task SimpleAuthTest() {
            IDatabase database = new Butterfly.Database.Memory.MemoryDatabase();
            database.CreateFromResourceFile(Assembly.GetExecutingAssembly(), "Butterfly.Auth.Test.db.sql");
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
