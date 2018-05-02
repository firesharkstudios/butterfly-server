using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using NLog;

using Butterfly.Database;
using Butterfly.Util;
using Butterfly.WebApi;

using Dict = System.Collections.Generic.Dictionary<string, object>;
using Butterfly.Util.Field;

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

        protected readonly NotifyMessage verifyEmailNotifyMessage;
        protected readonly NotifyMessage verifyPhoneTextNotifyMessage;
        protected readonly byte verifyNotifyMessagePriority;
        protected readonly string verifyCodeFormat;

        protected readonly static EmailFieldValidator EMAIL_FIELD_VALIDATOR = new EmailFieldValidator("email", false, true);
        protected readonly static PhoneFieldValidator PHONE_FIELD_VALIDATOR = new PhoneFieldValidator("phone", false);

        protected readonly static Random RANDOM = new Random();

        public NotifyManager(IDatabase database, INotifyMessageSender emailNotifyMessageSender = null, INotifyMessageSender phoneTextNotifyMessageSender = null, string notifyMessageTableName = "notify_message", string notifyVerifyTableName = "notify_verify", int verifyCodeExpiresSeconds = 3600, string verifyEmailFile = null, string verifyPhoneTextFile = null, byte verifyNotifyMessagePriority = 10, string verifyCodeFormat = "###-###") {
            this.database = database;
            this.emailNotifyMessageEngine = emailNotifyMessageSender == null ? null : new NotifyMessageEngine(NotifyMessageType.Email, emailNotifyMessageSender, database, notifyMessageTableName);
            this.phoneTextNotifyMessageEngine = phoneTextNotifyMessageSender == null ? null : new NotifyMessageEngine(NotifyMessageType.PhoneText, phoneTextNotifyMessageSender, database, notifyMessageTableName);
            this.notifyMessageTableName = notifyMessageTableName;
            this.notifyVerifyTableName = notifyVerifyTableName;
            this.verifyCodeExpiresSeconds = verifyCodeExpiresSeconds;

            this.verifyEmailNotifyMessage = verifyEmailFile!=null ? NotifyMessage.ParseFile(verifyEmailFile) : null;
            this.verifyPhoneTextNotifyMessage = verifyPhoneTextFile!=null ? NotifyMessage.ParseFile(verifyPhoneTextFile) : null;
            this.verifyNotifyMessagePriority = verifyNotifyMessagePriority;
            this.verifyCodeFormat = verifyCodeFormat;
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
            webApiServer.OnPost($"{pathPrefix}/send-verify-code", async (req, res) => {
                Dict values = await req.ParseAsJsonAsync<Dict>();
                string contact = values.GetAs("contact", (string)null);
                await this.SendVerifyCodeAsync(contact);
            });
        }

        public async Task SendVerifyCodeAsync(string contact) {
            logger.Debug($"SendVerifyCodeAsync():contact={contact}");

            // Scrub contact
            string scrubbedContact = Validate(contact);
            logger.Debug($"SendVerifyCodeAsync():scrubbedContact={scrubbedContact}");

            // Generate code and expires at
            int digits = this.verifyCodeFormat.Count(x => x=='#');
            int min = (int)Math.Pow(10, digits - 1);
            int max = (int)Math.Pow(10, digits) - 1;
            int code = RANDOM.Next(0, max - min) + min;
            logger.Debug($"SendVerifyCodeAsync():digits={digits},min={min},max={max},code={code}");
            DateTime expiresAt = DateTime.Now.AddSeconds(this.verifyCodeExpiresSeconds);

            // Insert/update database
            string id = await this.database.SelectValueAsync<string>($"SELECT id FROM {this.notifyVerifyTableName}", new {
                contact = scrubbedContact
            });
            if (id == null) {
                await this.database.InsertAndCommitAsync<string>(this.notifyVerifyTableName, new {
                    contact = scrubbedContact,
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

            // Send notify message
            var notifyMessageType = DetectNotifyMessageType(scrubbedContact);
            NotifyMessage notifyMessage = null;
            switch (notifyMessageType) {
                case NotifyMessageType.Email:
                    if (this.verifyEmailNotifyMessage == null) throw new Exception("Server must be configured with verify email notify message");
                    notifyMessage = this.verifyEmailNotifyMessage;
                    break;
                case NotifyMessageType.PhoneText:
                    if (this.verifyPhoneTextNotifyMessage == null) throw new Exception("Server must be configured with verify phone text notify message");
                    notifyMessage = this.verifyPhoneTextNotifyMessage;
                    break;
            }
            var evaluatedNotifyMessage = notifyMessage.Evaluate(new {
                contact = scrubbedContact,
                code = code.ToString(this.verifyCodeFormat)
            });
            await this.Queue(evaluatedNotifyMessage, this.verifyNotifyMessagePriority);
        }

        public async Task<string> VerifyAsync(string contact, int code) {
            logger.Debug($"VerifyAsync():contact={contact},code={code}");
            string scrubbedContact = Validate(contact);
            Dict result = await this.database.SelectRowAsync($"SELECT verify_code, expires_at FROM {this.notifyVerifyTableName}", new {
                contact = scrubbedContact
            });
            int verifyCode = result.GetAs("verify_code", -1);
            if (code == -1 || code!=verifyCode) throw new Exception("Invalid contact and/or verify code");

            int expiresAtUnix = result.GetAs("expires_at", -1);
            if (DateTimeX.FromUnixTimestamp(expiresAtUnix) < DateTime.Now) throw new Exception("Expired verify code");

            return scrubbedContact;
        }

        protected static string Validate(string value) {
            logger.Debug($"Validate():value={value}");
            if (value.Contains("@")) {
                return EMAIL_FIELD_VALIDATOR.Validate(value);
            }
            else {
                return PHONE_FIELD_VALIDATOR.Validate(value);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="priority">Higher number indicates higher priority</param>
        /// <param name="notifyMessage"></param>
        /// <returns></returns>
        public async Task Queue(NotifyMessage notifyMessage, byte priority = 0) {
            if (string.IsNullOrEmpty(notifyMessage.from)) throw new Exception("From address cannot be blank");
            string scrubbedFrom = Validate(notifyMessage.from);

            if (string.IsNullOrEmpty(notifyMessage.to)) throw new Exception("To address cannot be blank");
            string scrubbedTo = Validate(notifyMessage.to);

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
                            logger.Error(e);
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
