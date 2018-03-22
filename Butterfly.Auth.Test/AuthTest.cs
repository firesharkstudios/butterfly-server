using System;
using System.Reflection;
using System.Threading.Tasks;
using Butterfly.Database;
using Butterfly.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;

namespace Butterfly.Auth.Test {
    [TestClass]
    public class AuthTest {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [TestMethod]
        public async Task SimpleAuthTest() {
            //var database = new Butterfly.Database.Memory.MemoryDatabase();
            var database = new Butterfly.Database.MySql.MySqlDatabase("Server=127.0.0.1;Uid=test;Pwd=test!123;Database=butterfly_auth_test");
            await TruncateData(database);

            database.CreateFromResourceFile(Assembly.GetExecutingAssembly(), "Butterfly.Auth.Test.db.sql");
            database.SetInsertDefaultValue("id", tableName => Guid.NewGuid().ToString());
            database.SetInsertDefaultValue("created_at", tableName => DateTime.Now.ToUnixTimestamp());
            database.SetInsertDefaultValue("updated_at", tableName => DateTime.Now.ToUnixTimestamp());
            database.AddInputPreprocessor(BaseDatabase.RemapTypeInputPreprocessor<DateTime>(dateTime => dateTime.ToUnixTimestamp()));

            AuthManager authManager = new AuthManager(database, onForgotPassword: user => {
                logger.Debug($"onForgotPassword():user={user}");
            });
            AuthToken registerAuthToken = await authManager.RegisterAsync(new {
                username = "johnsmith",
                first_name = "John",
                last_name = "Smith",
                email = "john@fireshark.com",
                password = "test123"
            });
            AuthToken authenticateAuthToken = await authManager.Authenticate(registerAuthToken.id);

            await authManager.ForgotPassword("johnsmith");

            await Task.Delay(30000);
        }

        public static async Task TruncateData(IDatabase database) {
            using (ITransaction transaction = await database.BeginTransactionAsync()) {
                foreach (var tableName in database.Tables.Keys) {
                    await transaction.TruncateAsync(tableName);
                }
                await transaction.CommitAsync();
            }
        }

    }
}
