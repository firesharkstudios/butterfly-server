/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Butterfly.Core.WebApi {
    /// <summary>
    /// Internal class used to store references to new web handlers
    /// </summary>
    public class WebHandler {
        public HttpMethod method;
        public string path;
        public Func<IHttpRequest, IHttpResponse, Task> listener;
    }
}
