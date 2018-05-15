using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core.Util {
    public static class UriX {
        public static Dictionary<string, string> ParseQuery(this Uri me) {
            if (!string.IsNullOrEmpty(me.Query) && me.Query.Length>0) {
                return me.Query.Substring(1).Split('&').ToDictionary(x => x.Split('=')[0], x => Uri.UnescapeDataString(x.Split('=')[1]));
            }
            return new Dictionary<string, string>();
        }
    }
}
