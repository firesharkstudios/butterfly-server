using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using NLog;

namespace Butterfly.Notify.Aws {
    public class AwsSesNotifyMessageSender : INotifyMessageSender {

        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        // Choose the AWS region of the Amazon SES endpoint you want to connect to. Note that your sandbox 
        // status, sending limits, and Amazon SES identity-related settings are specific to a given 
        // AWS region, so be sure to select an AWS region in which you set up Amazon SES. Here, we are using 
        // the US West (Oregon) region. Examples of other regions that Amazon SES supports are USEast1 
        // and EUWest1. For a complete list, see http://docs.aws.amazon.com/ses/latest/DeveloperGuide/regions.html 
        protected readonly Amazon.RegionEndpoint region = Amazon.RegionEndpoint.USWest2;

        public AwsSesNotifyMessageSender(string regionText = "USEast1") {
            this.region = Amazon.RegionEndpoint.USEast1;
            //this.region = Amazon.RegionEndpoint.GetBySystemName(regionText);
        }

        public DateTime CanSendNextAt => throw new NotImplementedException();

        public async Task SendAsync(string from, string to, string subject, string bodyText, string bodyHtml) {
            Destination destination = new Destination {
                ToAddresses = (new List<string>() { to })
            };

            Content subjectContent = new Content(subject);
            Content bodyContent = new Content(bodyText);
            Body myBody = new Body(bodyContent);
            if (!string.IsNullOrEmpty(bodyHtml)) myBody.Html = new Content(bodyHtml);

            Message message = new Message(subjectContent, myBody);
            SendEmailRequest request = new SendEmailRequest(from, destination, message);

            AmazonSimpleEmailServiceClient client = new AmazonSimpleEmailServiceClient(this.region);

            try {
                await client.SendEmailAsync(request);
            }
            catch (Exception ex) {
                Console.WriteLine("The email was not sent.");
                Console.WriteLine("Error message: " + ex.Message);
            }
        }

    }
}

