using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Util {
    public static class UriX {
        public static Dictionary<string, string> ParseQuery(this Uri me) {
            return me.Query.Substring(1).Split('&').ToDictionary(x => x.Split('=')[0], x => Uri.UnescapeDataString(x.Split('=')[1]));
        }
    }
}
