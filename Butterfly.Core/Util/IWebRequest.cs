/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;

namespace Butterfly.Core.Util {
    public interface IWebRequest {
        Uri RequestUrl { get;  }

        Dictionary<string, string> Headers { get; }
        
        Dictionary<string, string> PathParams { get; }

        Dictionary<string, string> QueryParams { get; }

    }
}
