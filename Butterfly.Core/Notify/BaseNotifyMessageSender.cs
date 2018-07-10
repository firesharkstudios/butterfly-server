using System;
using System.Threading.Tasks;

namespace Butterfly.Core.Notify {

    public abstract class BaseNotifyMessageSender : INotifyMessageSender {

        protected readonly int sendEveryMillis;

        protected DateTime lastSend = DateTime.MinValue;

        public BaseNotifyMessageSender(int sendEveryMillis = 1000) {
            this.sendEveryMillis = sendEveryMillis;
        }

        public Task<string> SendAsync(string from, string to, string subject, string bodyText, string bodyHtml) {
            this.lastSend = DateTime.Now;
            return this.DoSendAsync(from, to, subject, bodyText, bodyHtml);
        }

        protected abstract Task<string> DoSendAsync(string from, string to, string subject, string bodyText, string bodyHtml);

        public DateTime CanSendNextAt {
            get {
                return this.lastSend.AddMilliseconds(this.sendEveryMillis);
            }
        }

    }
}
