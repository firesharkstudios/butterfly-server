/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;

using NLog;

using Butterfly.Core.Notify;
using System.IO;
using MimeKit;
using Amazon;

namespace Butterfly.Aws {
    public class AwsSesEmailNotifyMessageSender : BaseNotifyMessageSender {

        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        // Based on https://github.com/gianluis90/amazon-send-email/blob/master/SendEmailWithAttachments/Program.cs
        protected override async Task<string> DoSendAsync(string from, string to, string subject, string bodyText, string bodyHtml, string[] attachments) {
            logger.Debug($"DoSendAsync():from={from},to={to},subject={subject}");

            using (var client = new AmazonSimpleEmailServiceClient(Amazon.RegionEndpoint.USEast1))
            using (var messageStream = new MemoryStream()) {
                var message = new MimeMessage();
                var builder = new BodyBuilder() { TextBody = bodyText };
                if (!string.IsNullOrEmpty(bodyHtml)) builder.HtmlBody = bodyHtml;

                message.From.Add(MailboxAddress.Parse(from));
                message.To.Add(MailboxAddress.Parse(to));
                message.Subject = subject;

                if (attachments != null) {
                    foreach (var attachment in attachments) {
                        using (FileStream stream = File.Open(attachment, FileMode.Open)) builder.Attachments.Add(Path.GetFileName(attachment), stream);
                    }
                }

                message.Body = builder.ToMessageBody();
                message.WriteTo(messageStream);

                var request = new SendRawEmailRequest() {
                    RawMessage = new RawMessage() { Data = messageStream }
                };

                var response = await client.SendRawEmailAsync(request);
                return response.MessageId;
            }
        }

        /*
        protected override async Task<string> DoSendAsync(string from, string to, string subject, string bodyText, string bodyHtml) {
            logger.Debug($"DoSendAsync():from={from},to={to},subject={subject}");

            Destination destination = new Destination {
                ToAddresses = (new List<string>() { to })
            };

            Content subjectContent = new Content(subject);
            Content bodyContent = new Content(bodyText);
            Body myBody = new Body(bodyContent);
            if (!string.IsNullOrEmpty(bodyHtml)) myBody.Html = new Content(bodyHtml);

            Message message = new Message(subjectContent, myBody);
            SendEmailRequest sendEmailRequest = new SendEmailRequest(from, destination, message);

            AmazonSimpleEmailServiceClient client = new AmazonSimpleEmailServiceClient(Amazon.RegionEndpoint.USEast1);

            logger.Debug($"DoSendAsync():client.SendEmailAsync()");
            SendEmailResponse sendEmailResponse = await client.SendEmailAsync(sendEmailRequest);

            logger.Debug($"DoSendAsync():client.SendEmailAsync():sendEmailResponse.MessageId={sendEmailResponse.MessageId}");
            return sendEmailResponse.MessageId;
        }
        */

    }
}

