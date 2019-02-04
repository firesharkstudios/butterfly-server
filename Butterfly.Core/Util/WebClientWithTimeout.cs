using System;
using System.Net;

namespace Butterfly.Core.Util {
    public class WebClientWithTimeout : WebClient {

        protected readonly int timeoutMillis;

        public WebClientWithTimeout(int timeoutMillis) {
            this.timeoutMillis = timeoutMillis;
        }

        protected override WebRequest GetWebRequest(Uri uri) {
            WebRequest w = base.GetWebRequest(uri);
            w.Timeout = this.timeoutMillis;
            return w;
        }

    }
}
