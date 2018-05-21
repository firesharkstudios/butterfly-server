using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Butterfly.Aws;

namespace Butterfly.Core.Notify.Test {
    [TestClass]
    public class UnitTest1 {
        [TestMethod]
        public async Task SendAwsSesNotifyMessage() {
            var notifyMessageSender = new AwsSesEmailNotifyMessageSender();
            await NotifyTest.SendEmailNotifyMessage(notifyMessageSender);
        }
    }
}
