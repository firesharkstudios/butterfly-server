/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.IO;
using System.Threading.Tasks;

namespace Butterfly.Core.WebApi {
    public interface IHttpResponse {
        string GetHeader(string name);

        void SetHeader(string name, string value);

        int StatusCode { get; set; }

        string StatusText { get; set; }

        void SendRedirect(string url);

        Stream OutputStream { get; }

        Task WriteAsTextAsync(string value);

        Task WriteAsJsonAsync(object value);
    }
}
