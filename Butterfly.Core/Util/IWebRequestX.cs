using System;
using System.Net;
using System.Net.Http.Headers;

namespace Butterfly.Core.Util {
    public static class IWebRequestX {
        public static AuthenticationHeaderValue GetAuthenticationHeaderValue(this IWebRequest me) {
            if (me.Headers!=null && me.Headers.TryGetValue(HttpRequestHeader.Authorization.ToString().ToUpper(), out string text)) {
                if (text!=null && AuthenticationHeaderValue.TryParse(text, out AuthenticationHeaderValue result)) return result;
            }
            return null;
        }
    }
}
