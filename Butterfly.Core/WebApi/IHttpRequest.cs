/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Butterfly.Core.Util;

namespace Butterfly.Core.WebApi {
    /// <inheritdoc/>
    public interface IHttpRequest : IWebRequest {
        /// <summary>
        /// Parses the body of a POST as JSON
        /// </summary>
        /// <typeparam name="T">The type of object to return</typeparam>
        /// <returns>Returns an instance of <typeparamref name="T"/></returns>
        Task<T> ParseAsJsonAsync<T>();

        /// <summary>
        /// Parses the body of a POST as URL encoded data (like from a form)
        /// </summary>
        /// <typeparam name="T">The type of object to return</typeparam>
        /// <returns>Returns an instance of <typeparamref name="T"/></returns>
        Task<Dictionary<string, string>> ParseAsUrlEncodedAsync();

        /// <summary>
        /// Parses the body of a POST as a multipart stream (based on https://www.nuget.org/packages/HttpMultipartParser/)
        /// </summary>
        /// <param name="onData">Callback with parameters for name, fileName, type, disposition, buffer, bytes</param>
        /// <param name="onParameter">Callback with parameters for name, data</param>
        void ParseAsMultipartStream(Action<string, string, string, string, byte[], int> onData, Action<string, string> onParameter = null);
    }
}
