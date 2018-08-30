/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Butterfly.Core.Util;

namespace Butterfly.Core.WebApi {
    public interface IHttpRequest : IWebRequest {
        string UserAgent { get; }
        string UserHostAddress { get; }
        string UserHostName { get; }

        Task<T> ParseAsJsonAsync<T>();
        Task<Dictionary<string, string>> ParseAsUrlEncodedAsync();
        void ParseAsMultipartStream(Action<string, string, string, string, byte[], int> onData, Action<string, string> onParameter = null);
    }
}
