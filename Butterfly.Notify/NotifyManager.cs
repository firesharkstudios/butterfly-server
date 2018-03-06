using System;
using System.Threading;
using System.Threading.Tasks;

using NLog;

using Butterfly.Database;
using Butterfly.Util;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Notify {

    public enum NotifyMessageType {
        Email,
        PhoneText,
    }

    public class NotifyManager {

        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly IDatabase database;

        protected readonly NotifyMessageEngine emailNotifyMessageEngine;
        protected readonly NotifyMessageEngine phoneTextNotifyMessageEngine;

        public NotifyManager(IDatabase database, INotifyMessageSender emailNotifyMessageSender = null, INotifyMessageSender phoneTextNotifyMessageSender = null, string notifyMessageTableName = "notify_message") {
            this.database = database;
            this.emailNotifyMessageEngine = emailNotifyMessageSender == null ? null : new NotifyMessageEngine(NotifyMessageType.Email, emailNotifyMessageSender, database, notifyMessageTableName);
            this.phoneTextNotifyMessageEngine = phoneTextNotifyMessageSender == null ? null : new NotifyMessageEngine(NotifyMessageType.PhoneText, phoneTextNotifyMessageSender, database, notifyMessageTableName);
        }

        public void Start() {
            this.emailNotifyMessageEngine?.Start();
            this.phoneTextNotifyMessageEngine?.Start();
        }

        public void Stop() {
            this.emailNotifyMessageEngine?.Stop();
            this.phoneTextNotifyMessageEngine?.Stop();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="priority">Higher number indicates higher priority</param>
        /// <param name="notifyMessage"></param>
        /// <returns></returns>
        public async Task Queue(NotifyMessage notifyMessage, byte priority = 0) {
            NotifyMessageType notifyMessageType = this.DetectNotifyMessageType(notifyMessage.to);
            switch (notifyMessageType) {
                case NotifyMessageType.Email:
                    if (this.emailNotifyMessageEngine == null) throw new Exception("No email message sender configured");
                    await this.emailNotifyMessageEngine.Queue(priority, notifyMessage.from, notifyMessage.to, notifyMessage.subject, notifyMessage.bodyText, notifyMessage.bodyHtml);
                    break;
                case NotifyMessageType.PhoneText:
                    if (this.phoneTextNotifyMessageEngine == null) throw new Exception("No phone text message sender configured");
                    await this.phoneTextNotifyMessageEngine.Queue(priority, notifyMessage.from, notifyMessage.to, notifyMessage.subject, notifyMessage.bodyText, notifyMessage.bodyHtml);
                    break;
            }
        }

        protected NotifyMessageType DetectNotifyMessageType(string to) {
            if (string.IsNullOrWhiteSpace(to)) throw new Exception("Invalid contact '" + to + "'");
            else if (to.Contains("@")) return NotifyMessageType.Email;
            else if (to.StartsWith("+")) return NotifyMessageType.PhoneText;
            else throw new Exception("Invalid to '" + to + "'");
        }

        protected class NotifyMessageEngine {
            protected readonly NotifyMessageType notifyMessageType;
            protected readonly INotifyMessageSender notifyMessageSender;
            protected readonly IDatabase database;
            protected readonly string notifyMessageTableName;

            protected CancellationTokenSource cancellationTokenSource = null;

            public NotifyMessageEngine(NotifyMessageType notifyMessageType, INotifyMessageSender notifyMessageSender, IDatabase database, string notifyMessageTableName) {
                this.notifyMessageType = notifyMessageType;
                this.notifyMessageSender = notifyMessageSender;
                this.database = database;
                this.notifyMessageTableName = notifyMessageTableName;
            }

            protected bool started;

            public void Start() {
                if (!this.started) {
                    Task task = this.Run();
                }
            }

            public void Stop() {
                this.started = false;
                this.Pulse();
            }

            public async Task Queue(byte priority, string from, string to, string subject, string bodyText, string bodyHtml = null) {
                logger.Debug($"Queue():type={this.notifyMessageType},priority={priority},from={from},to={to},subject={subject}");
                await this.database.InsertAndCommitAsync<string>(this.notifyMessageTableName, new {
                    message_type = (byte)this.notifyMessageType,
                    message_priority = priority,
                    message_from = from,
                    message_to = to,
                    message_subject = subject,
                    message_body_text = bodyText,
                    message_body_html = bodyHtml,
                });
                this.Pulse();
            }

            protected void Pulse() {
                this.cancellationTokenSource?.Cancel();
            }

            async Task Run() {
                this.started = true;
                while (this.started) {
                    DateTime start = DateTime.Now;

                    Dict message = await this.database.SelectRowAsync(
                        @"SELECT *
                        FROM notify_message
                        WHERE message_type=@messageType AND sent_at IS NULL
                        ORDER BY message_priority DESC, created_at", new {
                            messageType = (byte)this.notifyMessageType
                        });
                    if (message == null) {
                        logger.Debug("Run():Waiting indefinitely");
                        try {
                            this.cancellationTokenSource = new CancellationTokenSource();
                            await Task.Delay(60000, cancellationTokenSource.Token);
                        }
                        catch (TaskCanceledException) {
                            this.cancellationTokenSource = null;
                        }
                        logger.Debug("Run():Waking up");
                    }
                    else {
                        NotifyMessageType notifyMessageType = message.GetAs("type", NotifyMessageType.Email);
                        string error = null;
                        try {
                            string from = message.GetAs("message_from", (string)null);
                            string to = message.GetAs("message_to", (string)null);
                            string subject = message.GetAs("message_subject", (string)null);
                            string bodyText = message.GetAs("message_body_text", (string)null);
                            string bodyHtml = message.GetAs("message_body_html", (string)null);
                            logger.Debug($"Run():Sending message to {to}");

                            await this.notifyMessageSender.SendAsync(from, to, subject, bodyText, bodyHtml);
                        }
                        catch (Exception e) {
                            error = e.Message;
                        }

                        var id = message.GetAs("id", (string)null);
                        await this.database.UpdateAndCommitAsync(this.notifyMessageTableName, new {
                            id,
                            sent_at = DateTime.Now,
                            send_error = error,
                        });

                        int totalMillis = (int)(this.notifyMessageSender.CanSendNextAt - DateTime.Now).TotalMilliseconds;
                        if (totalMillis>0) {
                            logger.Debug("Run():Sleeping for " + totalMillis + "ms");
                            await Task.Delay(totalMillis);
                        }
                    }

                }
            }
        }
    }
}
