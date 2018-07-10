using System;

using NLog;

using Butterfly.Core.Auth;
using Butterfly.Core.Database;
using Butterfly.Core.Util;
using Butterfly.Core.WebApi;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twilio.TwiML;
using Twilio.TwiML.Messaging;

namespace BuildHeroServer.Modules {

    public static class TwilioModule {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static void SetupWebApi(IDatabase database, AuthManager authManager, IWebApiServer webApiServer, Func<string, string, string, string[], Task<string>> handler) {
            webApiServer.OnPost(
                "/api/twilio/webhook",
                async (req, res) => {
                    logger.Debug($"/api/twilio/webhook");
                    Dictionary<string, string> evt = await req.ParseAsUrlEncodedAsync();
                    // /api/twilio/webhook,evt={"ToCountry":"US","ToState":"KS","SmsMessageSid":"SMefe1dff2b401753d8ccc4f3e573043d0","NumMedia":"0","ToCity":"WICHITA","FromZip":"67214","SmsSid":"SMefe1dff2b401753d8ccc4f3e573043d0","FromState":"KS","SmsStatus":"received","FromCity":"WICHITA","Body":"Yes","FromCountry":"US","To":"+13167127412","ToZip":"67202","NumSegments":"1","MessageSid":"SMefe1dff2b401753d8ccc4f3e573043d0","AccountSid":"AC38a91d26f280dc180e38ff7779c6ddab","From":"+13162105368","ApiVersion":"2010-04-01"}
                    // /api/twilio/webhook,evt={"ToCountry":"US","MediaContentType0":"image/jpeg","ToState":"KS","SmsMessageSid":"MMaf04fe088967a476b6153b1394c769b9","NumMedia":"1","ToCity":"WICHITA","FromZip":"67214","SmsSid":"MMaf04fe088967a476b6153b1394c769b9","FromState":"KS","SmsStatus":"received","FromCity":"WICHITA","Body":"","FromCountry":"US","To":"+13167127412","ToZip":"67202","NumSegments":"1","MessageSid":"MMaf04fe088967a476b6153b1394c769b9","AccountSid":"AC38a91d26f280dc180e38ff7779c6ddab","From":"+13162105368","MediaUrl0":"https://api.twilio.com/2010-04-01/Accounts/AC38a91d26f280dc180e38ff7779c6ddab/Messages/MMaf04fe088967a476b6153b1394c769b9/Media/ME41e63084a78b97317f8b6028334d9078","ApiVersion":"2010-04-01"}
                    logger.Debug($"/api/twilio/webhook,evt=" + JsonUtil.Serialize(evt));
                    string fromPhone = evt.GetAs("From", (string)null);
                    string toPhone = evt.GetAs("To", (string)null);
                    string body = evt.GetAs("Body", (string)null);
                    int numMedia = evt.GetAs("NumMedia", 0);
                    string[] mediaUrls = Enumerable.Range(1, numMedia).Select(x => evt.GetAs($"MediaUrl{x}", (string)null)).ToArray();
                    string responseText = await handler(fromPhone, toPhone, body, mediaUrls);
                    logger.Debug($"/api/twilio/webhook,responseText={responseText}");
                    if (!string.IsNullOrEmpty(responseText)) {
                        res.SetHeader("Content-Type", "text/plain");
                        await res.WriteAsTextAsync(responseText);
                        /*
                        var response = new MessagingResponse();
                        var message = new Message();
                        message.Body(responseText);
                        response.Append(message);
                        await res.WriteAsTextAsync(response.ToString());
                        */
                    }
                }
            );
        }

    }

}

