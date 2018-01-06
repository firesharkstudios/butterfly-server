using System;
using System.Net;
using System.Net.Http.Headers;

namespace Butterfly.Util {
    public static class IWebRequestX {
        public static AuthenticationHeaderValue GetAuthenticationHeaderValue(this IWebRequest me) {
            string text = me.Headers[HttpRequestHeader.Authorization.ToString()];
            return System.Net.Http.Headers.AuthenticationHeaderValue.Parse(text);
        }
    }
}
