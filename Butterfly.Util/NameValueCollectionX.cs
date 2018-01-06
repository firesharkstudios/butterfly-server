using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Butterfly.Util {
    public static class NameValueCollectionX {
        public static Dictionary<string, string> ToDictionary(this NameValueCollection nvc) {
            return nvc.AllKeys.ToDictionary(k => k, k => nvc[k]);
        }
    }
}
