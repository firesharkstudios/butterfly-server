/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

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
