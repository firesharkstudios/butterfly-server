/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.IO;
using System.Threading.Tasks;

namespace Butterfly.Core.WebApi {
    public interface IHttpResponse {
        /// <summary>
        /// Retrieve a response header
        /// </summary>
        /// <param name="name">The name of the response header to retrieve</param>
        /// <returns>The header value</returns>
        string GetHeader(string name);

        /// <summary>
        /// Set a response header
        /// </summary>
        /// <param name="name">The name of the response header</param>
        /// <param name="value">The value of the response header</param>
        void SetHeader(string name, string value);

        /// <summary>
        /// The HTTP status code
        /// </summary>
        int StatusCode { get; set; }

        /// <summary>
        /// The HTTP status text
        /// </summary>
        string StatusText { get; set; }

        /// <summary>
        /// Redirect the response to the <paramref name="url"/>
        /// </summary>
        /// <param name="url">The redirect url</param>
        void SendRedirect(string url);

        /// <summary>
        /// The output stream
        /// </summary>
        Stream OutputStream { get; }

        /// <summary>
        /// Write the <paramref name="value"/> as plain text
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        Task WriteAsTextAsync(string value);

        /// <summary>
        /// Write the <paramref name="value"/> as JSON
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        Task WriteAsJsonAsync(object value);
    }
}
