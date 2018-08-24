/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using NLog;

using Butterfly.Core.Util;
using Butterfly.Core.WebApi;

using Amazon.S3;
using Amazon.S3.Model;

using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

using MimeKit;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Aws {

    public static class AwsModule {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static readonly Regex IN_REPLY_TO_REGEX = new Regex(@"\<([^\@]+)\@");

        public static void Setup(IWebApiServer webApiServer, string topicArn, string endPoint, string bucketName, Func<string, string, string[], Task<string>> handler) {
            if (!string.IsNullOrEmpty(endPoint)) {
                Uri endPointUri = new Uri(endPoint);
                //logger.Debug($"SetupWebApi():endPointUri.PathAndQuery={endPointUri.PathAndQuery}");

                using (AmazonSimpleNotificationServiceClient amazonSimpleNotificationServiceClient = new AmazonSimpleNotificationServiceClient(Amazon.RegionEndpoint.USEast1)) {
                    SubscribeResponse subscribeResponse = amazonSimpleNotificationServiceClient.Subscribe(new SubscribeRequest {
                        TopicArn = topicArn,
                        Protocol = endPointUri.Scheme,
                        Endpoint = endPoint
                    });
                }

                AmazonS3Client amazonS3Client = new AmazonS3Client(Amazon.RegionEndpoint.USEast1);

                webApiServer.OnPost(
                    endPointUri.PathAndQuery,
                    async (req, res) => {
                        logger.Debug($"{endPointUri.PathAndQuery}");
                        Dict evt = await req.ParseAsJsonAsync<Dict>();
                        logger.Debug($"{endPointUri.PathAndQuery},evt=" + JsonUtil.Serialize(evt));
                        string type = evt.GetAs("Type", (string)null);
                        if (type == "SubscriptionConfirmation") {
                            string subscribeUrl = evt.GetAs("SubscribeURL", (string)null);
                            using (WebClient webClient = new WebClient()) {
                                string result = await webClient.DownloadStringTaskAsync(new Uri(subscribeUrl));
                                logger.Debug($"{endPointUri.PathAndQuery},result=" + result);
                            }
                        }
                        else if (type == "Notification") {
                            //string messageId = evt.GetAs("MessageId", (string)null);
                            string messageJson = evt.GetAs("Message", (string)null);
                            logger.Debug($"{endPointUri.PathAndQuery},messageJson={messageJson}");

                            Dict message = JsonUtil.Deserialize<Dict>(messageJson);
                            Dict mail = message.GetAs("mail", (Dict)null);

                            Dict[] headers = mail.GetAs("headers", (Dict[])null);
                            Dict inReplyToHeader = Array.Find(headers, x => x.GetAs("name", "") == "In-Reply-To");
                            string inReplyTo = inReplyToHeader.GetAs("value", "");
                            logger.Debug($"{endPointUri.PathAndQuery},inReplyTo={inReplyTo}");
                            Match match = IN_REPLY_TO_REGEX.Match(inReplyTo);
                            if (match.Success) {
                                string sentMessageId = match.Groups[1].Value;
                                string bucketKey = mail.GetAs("messageId", (string)null);
                                logger.Debug($"{endPointUri.PathAndQuery},sentMessageId={sentMessageId},bucketKey={bucketKey}");
                                if (!string.IsNullOrEmpty(bucketKey)) {
                                    GetObjectResponse getObjectResponse = await amazonS3Client.GetObjectAsync(new GetObjectRequest {
                                        BucketName = bucketName,
                                        Key = bucketKey
                                    });
                                    logger.Debug($"{endPointUri.PathAndQuery},getObjectResponse={getObjectResponse}");

                                    MimeMessage mimeMessage = await MimeMessage.LoadAsync(getObjectResponse.ResponseStream);
                                    logger.Debug($"{endPointUri.PathAndQuery},mimeMessage={mimeMessage}");

                                    await handler(sentMessageId, mimeMessage.TextBody, new string[] { });
                                }
                            }
                        }
                    }
                );
            }
        }

    }

}

