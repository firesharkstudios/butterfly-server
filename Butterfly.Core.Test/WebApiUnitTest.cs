/* Any copyright is dedicated to the Public Domain.
 * http://creativecommons.org/publicdomain/zero/1.0/ */

using System;
using System.Net;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Butterfly.Core.Util;
using Butterfly.Core.WebApi;

namespace Butterfly.Core.Test {
    [TestClass]
    public class WebApiUnitTest {
        /*
        [TestMethod]
        public async Task RedHttpServerWeb() {
            var redHttpServer = new RedHttpServerNet45.RedHttpServer(8080);
            using (var webApiServer = new Butterfly.WebApi.RedHttpServer.RedHttpServerWebApiServer(redHttpServer)) {
                await this.TestWeb(webApiServer, "http://localhost:8080/", () => {
                    redHttpServer.Start();
                });
            }
        }

        */

        public static async Task TestWeb(IWebApiServer webServer, string url, Action start) {
            // Add routes
            webServer.OnGet("/test-get1", async (req, res) => {
                await res.WriteAsJsonAsync("test-get-response");
            });
            webServer.OnGet("/test-get2/{id}", async (req, res) => {
                await res.WriteAsJsonAsync(req.PathParams.GetAs("id", (string)null));
            });
            webServer.OnPost("/test-post", async (req, res) => {
                var text = await req.ParseAsJsonAsync<string>();
                Assert.AreEqual("test-post-request", text);

                await res.WriteAsJsonAsync("test-post-response");
            });
            webServer.Start();

            // Start the underlying server
            start();

            // Test GET request #1
            using (WebClient webClient = new WebClient()) {
                string json = await webClient.DownloadStringTaskAsync(new Uri($"{url}test-get1"));
                string text = JsonUtil.Deserialize<string>(json);
                Assert.AreEqual("test-get-response", text);
            }

            // Test GET request #2
            using (WebClient webClient = new WebClient()) {
                string json = await webClient.DownloadStringTaskAsync(new Uri($"{url}test-get2/abc"));
                string text = JsonUtil.Deserialize<string>(json);
                Assert.AreEqual("abc", text);
            }

            // Test POST request
            using (WebClient webClient = new WebClient()) {
                string uploadJson = JsonUtil.Serialize("test-post-request");
                string downloadJson = await webClient.UploadStringTaskAsync(new Uri($"{url}test-post"), uploadJson);
                string downloadText = JsonUtil.Deserialize<string>(downloadJson);
                Assert.AreEqual("test-post-response", downloadText);
            }
        }
    }

}
