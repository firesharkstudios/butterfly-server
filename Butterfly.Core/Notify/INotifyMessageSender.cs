using System;
using System.Threading.Tasks;

namespace Butterfly.Notify {

    public interface INotifyMessageSender {

        Task SendAsync(string from, string to, string subject, string bodyText, string bodyHtml);

        DateTime CanSendNextAt { get; }
    }
}
