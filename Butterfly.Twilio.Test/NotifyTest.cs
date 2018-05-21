using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Butterfly.Twilio;
using Butterfly.Core.Notify.Test;

namespace Butterfly.Notify.Test {
    [TestClass]
    public class UnitTest1 {
        [TestMethod]
        public async Task SendTwilioSmsNotifyMessage() {
            var notifyMessageSender = new TwilioPhoneTextNotifyMessageSender("my-sid", "my-auth-token");
            await NotifyTest.SendPhoneTextNotifyMessage(notifyMessageSender);
        }
    }
}
