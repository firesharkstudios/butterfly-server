using System;

using NLog;

using Butterfly.Core.Auth;
using Butterfly.Core.Database;
using Butterfly.Core.Util;
using Butterfly.Core.WebApi;

using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

using Dict = System.Collections.Generic.Dictionary<string, object>;
using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using MimeKit;
using System.Text.RegularExpressions;

namespace BuildHeroServer.Modules {

    public static class AwsModule {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /*
        {
          "notificationType": "Received",
          "mail": {
            "timestamp": "2018-07-10T18:22:00.741Z",
            "source": "kent@fireshark.com",
            "messageId": "u9a6c5jkdgcoedc32ads509o2nqap9g0qiq2c601",
            "destination": [
              "reply-beta@notify.buildhero.io"
            ],
            "headersTruncated": false,
            "headers": [
              {
                "name": "Return-Path",
                "value": "<kent@fireshark.com>"
              },
              {
                "name": "Received",
                "value": "from mail-oi0-f46.google.com (mail-oi0-f46.google.com [209.85.218.46]) by inbound-smtp.us-east-1.amazonaws.com with SMTP id u9a6c5jkdgcoedc32ads509o2nqap9g0qiq2c601 for reply-beta@notify.buildhero.io; Tue, 10 Jul 2018 18:22:00 +0000 (UTC)"
              },
              {
                "name": "X-SES-Spam-Verdict",
                "value": "PASS"
              },
              {
                "name": "X-SES-Virus-Verdict",
                "value": "PASS"
              },
              {
                "name": "Received-SPF",
                "value": "none (spfCheck: 209.85.218.46 is neither permitted nor denied by domain of fireshark.com) client-ip=209.85.218.46; envelope-from=kent@fireshark.com; helo=mail-oi0-f46.google.com;"
              },
              {
                "name": "Authentication-Results",
                "value": "amazonses.com; spf=none (spfCheck: 209.85.218.46 is neither permitted nor denied by domain of fireshark.com) client-ip=209.85.218.46; envelope-from=kent@fireshark.com; helo=mail-oi0-f46.google.com; dkim=pass header.i=@fireshark-com.20150623.gappssmtp.com;"
              },
              {
                "name": "X-SES-RECEIPT",
                "value": "AEFBQUFBQUFBQUFGMm5Od3kySkhHVnRMdVJ2YnZtRHppZXZaU1FiWU5FUnlFRGJLczYxSTVYcityZFcyQVI0WC9IelFSUzNiZk9JWTMzUFdITFdnVWVqYkdhbHVsRVlKTjUxenNFMmdEa21PQzNyenpEcmdrbXBGVkxLaExzTXpTZXM3Y2NaQlR2MklGVUI0VDhNT1RZTVVaVkdOSGVxZWZFclpVUE4zTHlLU1VKcjZUWVdUeGFVTW1DeU1yb0FkWUNpNUNmTjMxZkYySGZFUGoxZmZTWm9xMm1FVGI1OXZabkxlbDBWYW1HNEwrRWJqOGRVQkE2R2ZFUXJaQmxPNHE4N3RTYm5ER1Ywck1mMmtBTkdkRHVlRlRkVEdPTmJmQzVZMzAyUysxZFBFSDhpVHEzdVRxaWc9PQ=="
              },
              {
                "name": "X-SES-DKIM-SIGNATURE",
                "value": "a=rsa-sha256; q=dns\/txt; b=epQSf3jF69\/44tOmu3\/PXPUG0MWuVHp+SAycAoGwC9D3orqN9toG6G+jMsHUwydKwBKE2ou9SQW5tsChDisjHwL5+zYLgeGI0+r2X2ojOnQ17y4VXyIhogbpjbIbrcq7xGNMAx6EwbroTijIO3bjRasEubm8wvbA5M6eVXat8IY=; c=relaxed\/simple; s=ug7nbtf4gccmlpwj322ax3p6ow6yfsug; d=amazonses.com; t=1531246921; v=1; bh=rCvb\/rliX0Bu7QSBMKBgnuk9WXTZL4xX2+qkbn4snRE=; h=From:To:Cc:Bcc:Subject:Date:Message-ID:MIME-Version:Content-Type:X-SES-RECEIPT;"
              },
              {
                "name": "Received",
                "value": "by mail-oi0-f46.google.com with SMTP id q11-v6so18887281oic.12 for <reply-beta@notify.buildhero.io>; Tue, 10 Jul 2018 11:22:00 -0700 (PDT)"
              },
              {
                "name": "DKIM-Signature",
                "value": "v=1; a=rsa-sha256; c=relaxed\/relaxed; d=fireshark-com.20150623.gappssmtp.com; s=20150623; h=to:from:subject:message-id:date:user-agent:mime-version:content-language:content-transfer-encoding; bh=Ik3CNMW982boSjuqoOY15nERNHJSA4+Y\/E8dCd5YIH4=; b=PGZP7GQ0\/2Dt+KMlgPno+i7AQwhYLiMBxeXj8Oi0IKOTYSEmcxH76OWx05r2XOsodfif5Bcy1EprM+nHoaPSNQwdFwqUweVGb2FMK3fWtCfquuChSXDJ3LPCmj\/lGNgkr14iwq1Ai0bB4CJe2xA+2Dcl3E7dXTZoeB+2yG174\/TE7wXYAxv84L\/+ZbDWNq\/gbq2c7t+F+lTfSv40p0BwUfDfr+dUxpnWK6MDqmo5jypUPr0vt3RWI6vkN+EYkeiv2T7PIvvjFgMcYK4fdnN153su\/wFGcVqb+EFLIgijZhsGsE1iEgJ4l2vAFKkmlDF5SyaFT5pUbfEknwucyvfZlg=="
              },
              {
                "name": "X-Google-DKIM-Signature",
                "value": "v=1; a=rsa-sha256; c=relaxed\/relaxed; d=1e100.net; s=20161025; h=x-gm-message-state:to:from:subject:message-id:date:user-agent :mime-version:content-language:content-transfer-encoding; bh=Ik3CNMW982boSjuqoOY15nERNHJSA4+Y\/E8dCd5YIH4=; b=Agjzt6NlgufjLsFmXiUbpbvU\/1CWMIaV+HDumKqigcjjRKA57sgVmyV1gGRsfrZtgL ld\/hci3enKBBnAvNPRT\/q\/pMH+Htts6NpVTZA3OjjiCcPb9z8f9HRtL0us8SkeQCtgK1 q7m0UWL0DicLt65JTSfGwnA4YXMht+Iky1+Q\/5bn1+Vqz4rSUqG3nXoIwglUufP\/K8\/y kmBAhhi\/n4dpJkHrLIdsoTwlo7GNplW7HDvWU07jvP\/qlcHUvaT3gnvKOD3gvvUW5256 Fiu77Eg7mU\/opgYOdwUon1l8oh8Ezym6O8epNNXQ0s+Pp37zKbN9gJl5i7+cD+iXLzx2 YtqA=="
              },
              {
                "name": "X-Gm-Message-State",
                "value": "APt69E0\/3DTccqKy6vBMqjVZcUxUZzGbJpL1TE3xkBk2ypYA+DBA3O4M GmV5ywLtlRix6q8KtLfiF\/DOk492cHo="
              },
              {
                "name": "X-Google-Smtp-Source",
                "value": "AAOMgpfs61XK29PN36t0EnGiHzZxmHEeVX+G3OJjPN38iIQXzA115bdEkh+qt4o9zw2WjE3mytZAVg=="
              },
              {
                "name": "X-Received",
                "value": "by 2002:aca:30d4:: with SMTP id w203-v6mr27409368oiw.8.1531246919985; Tue, 10 Jul 2018 11:21:59 -0700 (PDT)"
              },
              {
                "name": "Return-Path",
                "value": "<kent@fireshark.com>"
              },
              {
                "name": "Received",
                "value": "from [192.168.1.71] (wsip-68-99-118-196.ks.ok.cox.net. [68.99.118.196]) by smtp.gmail.com with ESMTPSA id x64-v6sm15762091oig.44.2018.07.10.11.21.59 for <reply-beta@notify.buildhero.io> (version=TLS1_2 cipher=ECDHE-RSA-AES128-GCM-SHA256 bits=128\/128); Tue, 10 Jul 2018 11:21:59 -0700 (PDT)"
              },
              {
                "name": "To",
                "value": "reply-beta@notify.buildhero.io"
              },
              {
                "name": "From",
                "value": "Kent Johnson <kent@fireshark.com>"
              },
              {
                "name": "Subject",
                "value": "Test #4"
              },
              {
                "name": "Message-ID",
                "value": "<fdba1e03-0d6b-bfc4-9216-b8b235c1e3cb@fireshark.com>"
              },
              {
                "name": "Date",
                "value": "Tue, 10 Jul 2018 13:21:58 -0500"
              },
              {
                "name": "User-Agent",
                "value": "Mozilla\/5.0 (Windows NT 10.0; WOW64; rv:52.0) Gecko\/20100101 Thunderbird\/52.9.0"
              },
              {
                "name": "MIME-Version",
                "value": "1.0"
              },
              {
                "name": "Content-Type",
                "value": "text\/plain; charset=utf-8; format=flowed"
              },
              {
                "name": "Content-Language",
                "value": "en-US"
              },
              {
                "name": "Content-Transfer-Encoding",
                "value": "7bit"
              }
            ],
            "commonHeaders": {
              "returnPath": "kent@fireshark.com",
              "from": [
                "Kent Johnson <kent@fireshark.com>"
              ],
              "date": "Tue, 10 Jul 2018 13:21:58 -0500",
              "to": [
                "reply-beta@notify.buildhero.io"
              ],
              "messageId": "<fdba1e03-0d6b-bfc4-9216-b8b235c1e3cb@fireshark.com>",
              "subject": "Test #4"
            }
          },
          "receipt": {
            "timestamp": "2018-07-10T18:22:00.741Z",
            "processingTimeMillis": 412,
            "recipients": [
              "reply-beta@notify.buildhero.io"
            ],
            "spamVerdict": {
              "status": "PASS"
            },
            "virusVerdict": {
              "status": "PASS"
            },
            "spfVerdict": {
              "status": "GRAY"
            },
            "dkimVerdict": {
              "status": "GRAY"
            },
            "dmarcVerdict": {
              "status": "GRAY"
            },
            "action": {
              "type": "S3",
              "topicArn": "arn:aws:sns:us-east-1:127745885679:notify_beta_buildhero_io",
              "bucketName": "notify-beta.buildhero.io",
              "objectKeyPrefix": "",
              "objectKey": "u9a6c5jkdgcoedc32ads509o2nqap9g0qiq2c601"
            }
          }
        }
        */
        static Regex IN_REPLY_TO_REGEX = new Regex(@"\<([^\@]+)\@");

        public static void SetupWebApi(IDatabase database, AuthManager authManager, IWebApiServer webApiServer, string topicArn, string endPoint, string bucketName, Func<string, string, string[], Task<string>> handler) {
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
                        // /api/aws/email-reply-callback,evt={"Type":"Notification","MessageId":"8f8c4805-a15f-5c87-8ae4-1bbb5ad628b9","TopicArn":"arn:aws:sns:us-east-1:127745885679:notify_beta_buildhero_io","Subject":"Amazon SES Email Receipt Notification","Message":"{\"notificationType\":\"Received\",\"mail\":{\"timestamp\":\"2018-07-10T18:22:00.741Z\",\"source\":\"kent@fireshark.com\",\"messageId\":\"u9a6c5jkdgcoedc32ads509o2nqap9g0qiq2c601\",\"destination\":[\"reply-beta@notify.buildhero.io\"],\"headersTruncated\":false,\"headers\":[{\"name\":\"Return-Path\",\"value\":\"<kent@fireshark.com>\"},{\"name\":\"Received\",\"value\":\"from mail-oi0-f46.google.com (mail-oi0-f46.google.com [209.85.218.46]) by inbound-smtp.us-east-1.amazonaws.com with SMTP id u9a6c5jkdgcoedc32ads509o2nqap9g0qiq2c601 for reply-beta@notify.buildhero.io; Tue, 10 Jul 2018 18:22:00 +0000 (UTC)\"},{\"name\":\"X-SES-Spam-Verdict\",\"value\":\"PASS\"},{\"name\":\"X-SES-Virus-Verdict\",\"value\":\"PASS\"},{\"name\":\"Received-SPF\",\"value\":\"none (spfCheck: 209.85.218.46 is neither permitted nor denied by domain of fireshark.com) client-ip=209.85.218.46; envelope-from=kent@fireshark.com; helo=mail-oi0-f46.google.com;\"},{\"name\":\"Authentication-Results\",\"value\":\"amazonses.com; spf=none (spfCheck: 209.85.218.46 is neither permitted nor denied by domain of fireshark.com) client-ip=209.85.218.46; envelope-from=kent@fireshark.com; helo=mail-oi0-f46.google.com; dkim=pass header.i=@fireshark-com.20150623.gappssmtp.com;\"},{\"name\":\"X-SES-RECEIPT\",\"value\":\"AEFBQUFBQUFBQUFGMm5Od3kySkhHVnRMdVJ2YnZtRHppZXZaU1FiWU5FUnlFRGJLczYxSTVYcityZFcyQVI0WC9IelFSUzNiZk9JWTMzUFdITFdnVWVqYkdhbHVsRVlKTjUxenNFMmdEa21PQzNyenpEcmdrbXBGVkxLaExzTXpTZXM3Y2NaQlR2MklGVUI0VDhNT1RZTVVaVkdOSGVxZWZFclpVUE4zTHlLU1VKcjZUWVdUeGFVTW1DeU1yb0FkWUNpNUNmTjMxZkYySGZFUGoxZmZTWm9xMm1FVGI1OXZabkxlbDBWYW1HNEwrRWJqOGRVQkE2R2ZFUXJaQmxPNHE4N3RTYm5ER1Ywck1mMmtBTkdkRHVlRlRkVEdPTmJmQzVZMzAyUysxZFBFSDhpVHEzdVRxaWc9PQ==\"},{\"name\":\"X-SES-DKIM-SIGNATURE\",\"value\":\"a=rsa-sha256; q=dns/txt; b=epQSf3jF69/44tOmu3/PXPUG0MWuVHp+SAycAoGwC9D3orqN9toG6G+jMsHUwydKwBKE2ou9SQW5tsChDisjHwL5+zYLgeGI0+r2X2ojOnQ17y4VXyIhogbpjbIbrcq7xGNMAx6EwbroTijIO3bjRasEubm8wvbA5M6eVXat8IY=; c=relaxed/simple; s=ug7nbtf4gccmlpwj322ax3p6ow6yfsug; d=amazonses.com; t=1531246921; v=1; bh=rCvb/rliX0Bu7QSBMKBgnuk9WXTZL4xX2+qkbn4snRE=; h=From:To:Cc:Bcc:Subject:Date:Message-ID:MIME-Version:Content-Type:X-SES-RECEIPT;\"},{\"name\":\"Received\",\"value\":\"by mail-oi0-f46.google.com with SMTP id q11-v6so18887281oic.12 for <reply-beta@notify.buildhero.io>; Tue, 10 Jul 2018 11:22:00 -0700 (PDT)\"},{\"name\":\"DKIM-Signature\",\"value\":\"v=1; a=rsa-sha256; c=relaxed/relaxed; d=fireshark-com.20150623.gappssmtp.com; s=20150623; h=to:from:subject:message-id:date:user-agent:mime-version:content-language:content-transfer-encoding; bh=Ik3CNMW982boSjuqoOY15nERNHJSA4+Y/E8dCd5YIH4=; b=PGZP7GQ0/2Dt+KMlgPno+i7AQwhYLiMBxeXj8Oi0IKOTYSEmcxH76OWx05r2XOsodfif5Bcy1EprM+nHoaPSNQwdFwqUweVGb2FMK3fWtCfquuChSXDJ3LPCmj/lGNgkr14iwq1Ai0bB4CJe2xA+2Dcl3E7dXTZoeB+2yG174/TE7wXYAxv84L/+ZbDWNq/gbq2c7t+F+lTfSv40p0BwUfDfr+dUxpnWK6MDqmo5jypUPr0vt3RWI6vkN+EYkeiv2T7PIvvjFgMcYK4fdnN153su/wFGcVqb+EFLIgijZhsGsE1iEgJ4l2vAFKkmlDF5SyaFT5pUbfEknwucyvfZlg==\"},{\"name\":\"X-Google-DKIM-Signature\",\"value\":\"v=1; a=rsa-sha256; c=relaxed/relaxed; d=1e100.net; s=20161025; h=x-gm-message-state:to:from:subject:message-id:date:user-agent :mime-version:content-language:content-transfer-encoding; bh=Ik3CNMW982boSjuqoOY15nERNHJSA4+Y/E8dCd5YIH4=; b=Agjzt6NlgufjLsFmXiUbpbvU/1CWMIaV+HDumKqigcjjRKA57sgVmyV1gGRsfrZtgL ld/hci3enKBBnAvNPRT/q/pMH+Htts6NpVTZA3OjjiCcPb9z8f9HRtL0us8SkeQCtgK1 q7m0UWL0DicLt65JTSfGwnA4YXMht+Iky1+Q/5bn1+Vqz4rSUqG3nXoIwglUufP/K8/y kmBAhhi/n4dpJkHrLIdsoTwlo7GNplW7HDvWU07jvP/qlcHUvaT3gnvKOD3gvvUW5256 Fiu77Eg7mU/opgYOdwUon1l8oh8Ezym6O8epNNXQ0s+Pp37zKbN9gJl5i7+cD+iXLzx2 YtqA==\"},{\"name\":\"X-Gm-Message-State\",\"value\":\"APt69E0/3DTccqKy6vBMqjVZcUxUZzGbJpL1TE3xkBk2ypYA+DBA3O4M GmV5ywLtlRix6q8KtLfiF/DOk492cHo=\"},{\"name\":\"X-Google-Smtp-Source\",\"value\":\"AAOMgpfs61XK29PN36t0EnGiHzZxmHEeVX+G3OJjPN38iIQXzA115bdEkh+qt4o9zw2WjE3mytZAVg==\"},{\"name\":\"X-Received\",\"value\":\"by 2002:aca:30d4:: with SMTP id w203-v6mr27409368oiw.8.1531246919985; Tue, 10 Jul 2018 11:21:59 -0700 (PDT)\"},{\"name\":\"Return-Path\",\"value\":\"<kent@fireshark.com>\"},{\"name\":\"Received\",\"value\":\"from [192.168.1.71] (wsip-68-99-118-196.ks.ok.cox.net. [68.99.118.196]) by smtp.gmail.com with ESMTPSA id x64-v6sm15762091oig.44.2018.07.10.11.21.59 for <reply-beta@notify.buildhero.io> (version=TLS1_2 cipher=ECDHE-RSA-AES128-GCM-SHA256 bits=128/128); Tue, 10 Jul 2018 11:21:59 -0700 (PDT)\"},{\"name\":\"To\",\"value\":\"reply-beta@notify.buildhero.io\"},{\"name\":\"From\",\"value\":\"Kent Johnson <kent@fireshark.com>\"},{\"name\":\"Subject\",\"value\":\"Test #4\"},{\"name\":\"Message-ID\",\"value\":\"<fdba1e03-0d6b-bfc4-9216-b8b235c1e3cb@fireshark.com>\"},{\"name\":\"Date\",\"value\":\"Tue, 10 Jul 2018 13:21:58 -0500\"},{\"name\":\"User-Agent\",\"value\":\"Mozilla/5.0 (Windows NT 10.0; WOW64; rv:52.0) Gecko/20100101 Thunderbird/52.9.0\"},{\"name\":\"MIME-Version\",\"value\":\"1.0\"},{\"name\":\"Content-Type\",\"value\":\"text/plain; charset=utf-8; format=flowed\"},{\"name\":\"Content-Language\",\"value\":\"en-US\"},{\"name\":\"Content-Transfer-Encoding\",\"value\":\"7bit\"}],\"commonHeaders\":{\"returnPath\":\"kent@fireshark.com\",\"from\":[\"Kent Johnson <kent@fireshark.com>\"],\"date\":\"Tue, 10 Jul 2018 13:21:58 -0500\",\"to\":[\"reply-beta@notify.buildhero.io\"],\"messageId\":\"<fdba1e03-0d6b-bfc4-9216-b8b235c1e3cb@fireshark.com>\",\"subject\":\"Test #4\"}},\"receipt\":{\"timestamp\":\"2018-07-10T18:22:00.741Z\",\"processingTimeMillis\":412,\"recipients\":[\"reply-beta@notify.buildhero.io\"],\"spamVerdict\":{\"status\":\"PASS\"},\"virusVerdict\":{\"status\":\"PASS\"},\"spfVerdict\":{\"status\":\"GRAY\"},\"dkimVerdict\":{\"status\":\"GRAY\"},\"dmarcVerdict\":{\"status\":\"GRAY\"},\"action\":{\"type\":\"S3\",\"topicArn\":\"arn:aws:sns:us-east-1:127745885679:notify_beta_buildhero_io\",\"bucketName\":\"notify-beta.buildhero.io\",\"objectKeyPrefix\":\"\",\"objectKey\":\"u9a6c5jkdgcoedc32ads509o2nqap9g0qiq2c601\"}}}","Timestamp":"2018-07-10 18:22:01","SignatureVersion":"1","Signature":"q6wXpZSMbBFXFi9YLpi6r0Wqi2m6mTcKHyR+IU0nSDhmXq2Tb5xvary6wv4O2XPnVJV4psOkPU645CQvqzZI0B3iP75uY27heRfDMHOKjpykd00WFJ4EpqACdEeIPEqIfqct/XHy5069DyTSqpG4m759uAG75Kfv0e2HzqUsdi8oBg/mpH0gZj9sQj/3S/47Vapmq5XpC9l3f/NPwrWC6AfUsrrnvhLnGis1TnyISb+Bjvn2S8NRrvJu5jIt7Kr/VKF1gldMyEmLvU1Z8ZzpcAm3L+sKo7Gk7yAg09LzuutsNOJ4gEDiYWVGE6iQn91nmTPK92x+t0+1hzj8x/yU7Q==","SigningCertURL":"https://sns.us-east-1.amazonaws.com/SimpleNotificationService-eaea6120e66ea12e88dcd8bcbddca752.pem","UnsubscribeURL":"https://sns.us-east-1.amazonaws.com/?Action=Unsubscribe&SubscriptionArn=arn:aws:sns:us-east-1:127745885679:notify_beta_buildhero_io:7d8952af-5ca9-4909-9a3e-bcb11f10ecd2"}
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

