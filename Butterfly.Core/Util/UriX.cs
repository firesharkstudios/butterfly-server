/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

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
