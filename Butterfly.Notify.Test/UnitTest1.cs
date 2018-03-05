using System;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Butterfly.Util;
using Butterfly.Database;
using Butterfly.Notify.Aws;
using Butterfly.Notify.Twilio;

namespace Butterfly.Notify.Test {
    [TestClass]
    public class UnitTest1 {
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
        public async Task SendEmailNotifyMessage() {
            IDatabase database = new Butterfly.Database.Memory.MemoryDatabase();
            database.CreateFromResourceFile(Assembly.GetExecutingAssembly(), "Butterfly.Notify.Test.db.sql");
            database.SetInsertDefaultValue("id", tableName => Guid.NewGuid().ToString());
            database.SetInsertDefaultValue("created_at", tableName => DateTime.Now);

            var notifyMessageSender = new AwsSesNotifyMessageSender();
            var notifyMessageManager = new NotifyMessageManager(database, emailNotifyMessageSender: notifyMessageSender);
            notifyMessageManager.Start();
            var notifyMessage = new NotifyMessage("kent@fireshark.com", "kent13304@yahoo.com", "Test SES", "Just testing", null);
            await notifyMessageManager.Queue(0, notifyMessage);
            await Task.Delay(200000);
        }

        [TestMethod]
        public async Task SendPhoneTextNotifyMessage() {
            IDatabase database = new Butterfly.Database.Memory.MemoryDatabase();
            database.CreateFromResourceFile(Assembly.GetExecutingAssembly(), "Butterfly.Notify.Test.db.sql");
            database.SetInsertDefaultValue("id", tableName => Guid.NewGuid().ToString());
            database.SetInsertDefaultValue("created_at", tableName => DateTime.Now);

            var notifyMessageSender = new TwilioPhoneTextNotifyMessageSender("my-sid", "my-auth-token");
            var notifyMessageManager = new NotifyMessageManager(database, phoneTextNotifyMessageSender: notifyMessageSender);
            notifyMessageManager.Start();
            var notifyMessage = new NotifyMessage("+1 316 712 7412", "+1 316 555 1212", null, "Just testing", null);
            await notifyMessageManager.Queue(0, notifyMessage);
            await Task.Delay(200000);
        }

    }
}
