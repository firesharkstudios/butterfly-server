using System;
using System.Threading.Tasks;

namespace Butterfly.Core.Notify {

    public interface INotifyMessageSender {

        Task<string> SendAsync(string from, string to, string subject, string bodyText, string bodyHtml);

        DateTime CanSendNextAt { get; }
    }
}
