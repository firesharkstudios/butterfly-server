/* Any copyright is dedicated to the Public Domain.
 * http://creativecommons.org/publicdomain/zero/1.0/ */
 
using System;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;

using Butterfly.Core.Auth;
using Butterfly.Core.Database;
using Butterfly.Core.Util;

namespace Butterfly.Core.Test {
    [TestClass]
    public class AuthTest {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [TestMethod]
        public async Task SimpleAuthTest() {
            var database = new Butterfly.Core.Database.Memory.MemoryDatabase();
            //var database = new Butterfly.MySql.MySqlDatabase("Server=127.0.0.1;Uid=test;Pwd=test!123;Database=butterfly_auth_test");
            await TruncateData(database);

            await database.CreateFromResourceFileAsync(Assembly.GetExecutingAssembly(), "Butterfly.Core.Test.db.sql");
            database.SetDefaultValue("id", tableName => Guid.NewGuid().ToString());
            database.SetDefaultValue("created_at", tableName => DateTime.Now.ToUnixTimestamp());
            database.SetDefaultValue("updated_at", tableName => DateTime.Now.ToUnixTimestamp());
            database.AddInputPreprocessor(BaseDatabase.RemapTypeInputPreprocessor<DateTime>(dateTime => dateTime.ToUnixTimestamp()));

            AuthManager authManager = new AuthManager(database, userTableRoleFieldName: null, onForgotPassword: user => {
                logger.Debug($"onForgotPassword():user={user}");
                return Task.FromResult(0);
            });
            UserRefToken registerAuthToken = await authManager.RegisterAsync(new {
                username = "johnsmith",
                first_name = "John",
                last_name = "Smith",
                email = "john@fireshark.com",
                phone = "+13162105368",
                password = "test123"
            });
            if (database.CanJoin) {
                AuthToken authToken = await authManager.AuthenticateAsync(UserRefTokenAuthenticator.AUTH_TYPE, registerAuthToken.id);
            }

            await authManager.ForgotPasswordAsync("johnsmith");

            //await Task.Delay(30000);
        }

        public static async Task TruncateData(IDatabase database) {
            using (ITransaction transaction = await database.BeginTransactionAsync()) {
                foreach (var tableName in database.TableByName.Keys) {
                    await transaction.TruncateAsync(tableName);
                }
                await transaction.CommitAsync();
            }
        }

    }
}
