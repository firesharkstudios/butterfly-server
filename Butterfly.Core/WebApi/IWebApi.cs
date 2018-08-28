/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Threading.Tasks;

namespace Butterfly.Core.WebApi {
    /// <summary>
    /// Allows receiving API requests via HTTP (inspired by Node.js' Express) by wrapping existing C# web servers.<para/>
    /// </summary>
    /// <remarks>
    /// Initializing a web api server instance...<para/>
    /// <code>
    ///     var webApi = new SomeWebApi();
    ///     webApi.OnGet("/chat-messages", async(req, res) => {
    ///         // Handle the request and return any objects as needed (will be JSON encoded)
    ///     });
    ///     webApi.OnPost("/login", async(req, res) => {
    ///         // Handle the request and return any objects as needed (will be JSON encoded)
    ///     });
    ///     webApi.Start();
    /// </code>
    /// </remarks>
    public interface IWebApi : IDisposable {
        /// <summary>
        /// Add a listener responding to GET requests
        /// </summary>
        /// <param name="path"></param>
        /// <param name="listener"></param>
        void OnGet(string path, Func<IHttpRequest, IHttpResponse, Task> listener);

        /// <summary>
        /// Add a listener responding to POST requests
        /// </summary>
        /// <param name="path"></param>
        /// <param name="listener"></param>
        void OnPost(string path, Func<IHttpRequest, IHttpResponse, Task> listener);

        /// <summary>
        /// Start the web api server
        /// </summary>
        void Start();
    }
}
