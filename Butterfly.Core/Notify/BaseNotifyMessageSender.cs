/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

 using System;
using System.Threading.Tasks;

namespace Butterfly.Core.Notify {

    public abstract class BaseNotifyMessageSender : INotifyMessageSender {

        protected readonly int sendEveryMillis;

        protected DateTime lastSend = DateTime.MinValue;

        public BaseNotifyMessageSender(int sendEveryMillis = 1000) {
            this.sendEveryMillis = sendEveryMillis;
        }

        public Task<string> SendAsync(string from, string to, string subject, string bodyText, string bodyHtml, string[] attachments) {
            this.lastSend = DateTime.Now;
            return this.DoSendAsync(from, to, subject, bodyText, bodyHtml, attachments);
        }

        protected abstract Task<string> DoSendAsync(string from, string to, string subject, string bodyText, string bodyHtml, string[] attachments);

        public DateTime CanSendNextAt {
            get {
                return this.lastSend.AddMilliseconds(this.sendEveryMillis);
            }
        }

    }
}
