/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;

namespace Butterfly.Core.Util {
    public interface IWebRequest {
        /// <summary>
        /// Retrieves the URL used to make the request
        /// </summary>
        Uri RequestUrl { get;  }

        /// <summary>
        /// Retrieves the request headers
        /// </summary>
        Dictionary<string, string> Headers { get; }

        /// <summary>
        /// Retrieves the path params
        /// </summary>
        /// <example>
        /// <code>
        /// webApi.OnGet("/todo/{id}", (req, res) => {
        ///     // Opening /todo/123 would echo id=123 to the console below
        ///     Console.Writeline($"id={req.PathParams["id"]}");
        /// });
        /// </code>
        /// </example>
        Dictionary<string, string> PathParams { get; }

        /// <summary>
        /// Retrieves the query string params
        /// </summary>
        /// <example>
        /// <code>
        ///     webApi.OnGet("/todo", (req, res) => {
        ///         // Opening /todo?id=123 would echo id=123 to the console below
        ///         Console.Writeline($"id={req.QueryParams["id"]}");
        ///     });
        /// </code>
        /// </example>
        Dictionary<string, string> QueryParams { get; }
    }
}
