/* Any copyright is dedicated to the Public Domain.
 * http://creativecommons.org/publicdomain/zero/1.0/ */

using System;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Butterfly.Core.Util;
using Butterfly.Core.Database;

namespace Butterfly.Core.Notify.Test {
    [TestClass]
    public class NotifyTest {
        [TestMethod]
        public void ParseNotifyMessage() {
            var email = FileX.LoadResourceAsText(Assembly.GetExecutingAssembly(), "Butterfly.Notify.Test.email.txt");
            var templateNotifyMessage = NotifyMessage.Parse(email);
            var notifyMessage = templateNotifyMessage.Evaluate(new {
                first_name = "Bob"
            });
            Assert.IsTrue(notifyMessage.bodyText.Trim().StartsWith("Hello, Bob."));
        }

        [TestMethod]
        public static async Task SendEmailNotifyMessage(INotifyMessageSender notifyMessageSender) {
            IDatabase database = new Butterfly.Core.Database.Memory.MemoryDatabase();
            await database.CreateFromResourceFileAsync(Assembly.GetExecutingAssembly(), "Butterfly.Notify.Test.db.sql");
            database.SetDefaultValue("id", tableName => Guid.NewGuid().ToString());
            database.SetDefaultValue("created_at", tableName => DateTime.Now);

            var notifyMessageManager = new NotifyManager(database, emailNotifyMessageSender: notifyMessageSender);
            notifyMessageManager.Start();
            var notifyMessage = new NotifyMessage("kent@fireshark.com", "kent13304@yahoo.com", "Test SES", "Just testing", null);
            using (ITransaction transaction = await database.BeginTransactionAsync()) {
                await notifyMessageManager.Queue(transaction, notifyMessage);
                await transaction.CommitAsync();
            }
            await Task.Delay(200000);
        }

        [TestMethod]
        public static async Task SendPhoneNotifyMessage(INotifyMessageSender notifyMessageSender) {
            IDatabase database = new Butterfly.Core.Database.Memory.MemoryDatabase();
            await database.CreateFromResourceFileAsync(Assembly.GetExecutingAssembly(), "Butterfly.Notify.Test.db.sql");
            database.SetDefaultValue("id", tableName => Guid.NewGuid().ToString());
            database.SetDefaultValue("created_at", tableName => DateTime.Now);

            var notifyMessageManager = new NotifyManager(database, phoneNotifyMessageSender: notifyMessageSender);
            notifyMessageManager.Start();
            var notifyMessage = new NotifyMessage("+1 316 712 7412", "+1 316 555 1212", null, "Just testing", null);
            using (ITransaction transaction = await database.BeginTransactionAsync()) {
                await notifyMessageManager.Queue(transaction, notifyMessage);
            }
            await Task.Delay(200000);
        }

    }
}
