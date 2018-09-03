using System;

using Microsoft.AspNetCore.Http;

namespace Butterfly.RHttpServer {
    public static class HttpRequestX {
        public static Uri ToUri(this HttpRequest me) {
            return new Uri($"{me.Scheme}://{me.Host}{me.Path}?{me.QueryString}");
        }
    }
}
