using System;
using System.Reflection;
using System.Threading.Tasks;
using Butterfly.Core.Database;
using Butterfly.Core.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;

namespace Butterfly.Core.Auth.Test {
    [TestClass]
    public class AuthTest {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [TestMethod]
        public async Task SimpleAuthTest() {
            //var database = new Butterfly.Core.Database.Memory.MemoryDatabase();
            var database = new Butterfly.MySql.MySqlDatabase("Server=127.0.0.1;Uid=test;Pwd=test!123;Database=butterfly_auth_test");
            await TruncateData(database);

            database.CreateFromResourceFile(Assembly.GetExecutingAssembly(), "Butterfly.Core.Auth.Test.db.sql");
            database.SetDefaultValue("id", tableName => Guid.NewGuid().ToString());
            database.SetDefaultValue("created_at", tableName => DateTime.Now.ToUnixTimestamp());
            database.SetDefaultValue("updated_at", tableName => DateTime.Now.ToUnixTimestamp());
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
            AuthToken authenticateAuthToken = await authManager.AuthenticateAsync(registerAuthToken.id);

            await authManager.ForgotPasswordAsync("johnsmith");

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
