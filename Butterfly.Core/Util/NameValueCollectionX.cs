using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Butterfly.Core.Util {
    public static class NameValueCollectionX {
        public static Dictionary<string, string> ToDictionary(this NameValueCollection nameValueCollection, bool forceUpperCaseKeys = false) {
            return nameValueCollection.AllKeys.ToDictionary(k => forceUpperCaseKeys ? k.ToUpper() : k, k => nameValueCollection[k]);
        }
    }
}
