using System;
using System.Threading;
using System.Threading.Tasks;

using NLog;

using Butterfly.Database;
using Butterfly.Util;

using Dict = System.Collections.Generic.Dictionary<string, object>;
using System.Text.RegularExpressions;
using Butterfly.WebApi;

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

        protected readonly string notifyMessageTableName;
        protected readonly string notifyVerifyTableName;
        protected readonly int verifyCodeExpiresSeconds;

        protected readonly Random random = new Random();

        public NotifyManager(IDatabase database, INotifyMessageSender emailNotifyMessageSender = null, INotifyMessageSender phoneTextNotifyMessageSender = null, string notifyMessageTableName = "notify_message", string notifyVerifyTableName = "notify_verify", int verifyCodeExpiresSeconds = 3600) {
            this.database = database;
            this.emailNotifyMessageEngine = emailNotifyMessageSender == null ? null : new NotifyMessageEngine(NotifyMessageType.Email, emailNotifyMessageSender, database, notifyMessageTableName);
            this.phoneTextNotifyMessageEngine = phoneTextNotifyMessageSender == null ? null : new NotifyMessageEngine(NotifyMessageType.PhoneText, phoneTextNotifyMessageSender, database, notifyMessageTableName);
            this.notifyMessageTableName = notifyMessageTableName;
            this.notifyVerifyTableName = notifyVerifyTableName;
            this.verifyCodeExpiresSeconds = verifyCodeExpiresSeconds;
        }

        public void Start() {
            this.emailNotifyMessageEngine?.Start();
            this.phoneTextNotifyMessageEngine?.Start();
        }

        public void Stop() {
            this.emailNotifyMessageEngine?.Stop();
            this.phoneTextNotifyMessageEngine?.Stop();
        }

        public void SetupWebApi(IWebApiServer webApiServer, string pathPrefix = "/api/notify") {
            webApiServer.OnGet($"{pathPrefix}/create-verify-code", async (req, res) => {
                Dict values = await req.ParseAsJsonAsync<Dict>();
                string contact = values.GetAs("contact", (string)null);
                await this.CreateVerifyCodeAsync(contact);
            });

            webApiServer.OnGet($"{pathPrefix}/verify", async (req, res) => {
                Dict values = await req.ParseAsJsonAsync<Dict>();
                string contact = values.GetAs("contact", (string)null);
                int code = values.GetAs("code", -1);
                await this.VerifyAsync(contact, code);
            });
        }

        public async Task CreateVerifyCodeAsync(string contact) {
            string scrubbedContact = Scrub(contact);
            int code = this.random.Next(0, 899999) + 100000;
            string id = await this.database.SelectValueAsync<string>($"SELECT id FROM {this.notifyVerifyTableName}", new {
                verify_contact = scrubbedContact
            });

            DateTime expiresAt = DateTime.Now.AddSeconds(this.verifyCodeExpiresSeconds);
            if (id == null) {
                await this.database.InsertAndCommitAsync<string>(this.notifyVerifyTableName, new {
                    verify_contact = scrubbedContact,
                    verify_code = code,
                    expires_at = expiresAt,
                });
            }
            else {
                await this.database.UpdateAndCommitAsync(this.notifyVerifyTableName, new {
                    id,
                    verify_code = code,
                    expires_at = expiresAt,
                });
            }
        }

        public async Task VerifyAsync(string contact, int code) {
            string scrubbedContact = Scrub(contact);
            int verifyCode = await this.database.SelectValueAsync<int>($"SELECT verify_code FROM {this.notifyVerifyTableName}", new {
                verify_contact = scrubbedContact
            }, -1);
            if (code == -1 || code!=verifyCode) throw new Exception("Invalid contact and/or verify code");
        }

        protected static string Scrub(string contact) {
            if (contact.Contains("@")) {
                return ScrubEmail(contact);
            }
            else {
                return ScrubPhone(contact);
            }
        }

        protected static string ScrubEmail(string email, bool stripName = false) {
            int leftPos = email.IndexOf('<');
            int rightPos = email.LastIndexOf('>');
            if (leftPos > 0 && rightPos > 0 && leftPos < rightPos) {
                string name = email.Substring(0, leftPos).Trim();
                string address = email.Substring(leftPos + 1, rightPos - leftPos - 1).Trim().ToLower();
                if (!address.Contains("@")) {
                    throw new Exception("Email address must contain @");
                }
                else if (stripName) {
                    return address;
                }
                else {
                    return $"{name} <{address}>";
                }
            }
            else if (!email.Contains("@")) {
                throw new Exception("Email address must contain @");
            }
            else {
                return email.ToLower();
            }
        }

        protected static readonly Regex NON_PHONE_CHARS = new Regex(@"\D");

        protected static string ScrubPhone(string phone) {
            string newPhone = NON_PHONE_CHARS.Replace(phone, "");
            if (!newPhone.StartsWith("+") && newPhone.Length==10) {
                return $"+1{newPhone}";
            }
            else {
                return newPhone;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="priority">Higher number indicates higher priority</param>
        /// <param name="notifyMessage"></param>
        /// <returns></returns>
        public async Task Queue(NotifyMessage notifyMessage, byte priority = 0) {
            string scrubbedFrom = Scrub(notifyMessage.from);
            string scrubbedTo = Scrub(notifyMessage.to);
            NotifyMessageType notifyMessageType = this.DetectNotifyMessageType(scrubbedTo);
            switch (notifyMessageType) {
                case NotifyMessageType.Email:
                    if (this.emailNotifyMessageEngine == null) throw new Exception("No email message sender configured");
                    await this.emailNotifyMessageEngine.Queue(priority, scrubbedFrom, scrubbedTo, notifyMessage.subject, notifyMessage.bodyText, notifyMessage.bodyHtml);
                    break;
                case NotifyMessageType.PhoneText:
                    if (this.phoneTextNotifyMessageEngine == null) throw new Exception("No phone text message sender configured");
                    await this.phoneTextNotifyMessageEngine.Queue(priority, scrubbedFrom, scrubbedTo, notifyMessage.subject, notifyMessage.bodyText, notifyMessage.bodyHtml);
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
                        FROM " + this.notifyMessageTableName + @"
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
