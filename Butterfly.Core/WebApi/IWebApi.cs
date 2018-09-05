/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Threading.Tasks;

namespace Butterfly.Core.WebApi {
    /// <summary>
    /// Allows receiving API requests via HTTP (inspired by Node.js' Express)<para/>
    /// </summary>
    /// <remarks>
    /// Example usage...<para/>
    /// <code>
    ///     var webApi = new SomeWebApi();
    ///     webApi.OnGet("/chat-messages", async(req, res) => {
    ///         // Handle the request and return any objects as needed (will be JSON encoded)
    ///     });
    ///     webApi.OnPost("/login", async(req, res) => {
    ///         // Handle the request and return any objects as needed (will be JSON encoded)
    ///     });
    ///     webApi.Compile();
    /// </code>
    /// </remarks>
    public interface IWebApi : IDisposable {
        /// <summary>
        /// Adds a <paramref name="listener"/> executed when DELETE requests are received matching <paramref name="path"/>
        /// </summary>
        /// <param name="path">Only execute the <paramref name="listener"/> when DELETE requests match this path. Can use tokens like <code>{id}</code> in the path with the values of these tokens available in <see cref="IHttpRequest.PathParams">IHttpRequest.PathParams</see>.</param>
        /// <param name="listener">Execute this async handler when a DELETE request is received matching the <paramref name="path"/> (passes <see cref="IHttpRequest"/> and <see cref="IHttpResponse"/> instances to the handler)</param>
        void OnDelete(string path, Func<IHttpRequest, IHttpResponse, Task> listener);

        /// <summary>
        /// Adds a <paramref name="listener"/> executed when GET requests are received matching <paramref name="path"/>
        /// </summary>
        /// <param name="path">Only execute the <paramref name="listener"/> when GET requests match this path. Can use tokens like <code>{id}</code> in the path with the values of these tokens available in <see cref="IHttpRequest.PathParams">IHttpRequest.PathParams</see>.</param>
        /// <param name="listener">Execute this async handler when a GET request is received matching the <paramref name="path"/> (passes <see cref="IHttpRequest"/> and <see cref="IHttpResponse"/> instances to the handler)</param>
        void OnGet(string path, Func<IHttpRequest, IHttpResponse, Task> listener);

        /// <summary>
        /// Adds a <paramref name="listener"/> executed when POST requests are received matching <paramref name="path"/>
        /// </summary>
        /// <param name="path">Only execute the <paramref name="listener"/> when POST requests match this path. Can use tokens like <code>{id}</code> in the path with the values of these tokens available in <see cref="IHttpRequest.PathParams">IHttpRequest.PathParams</see>.</param>
        /// <param name="listener">Execute this async handler when a POST request is received matching the <paramref name="path"/> (passes <see cref="IHttpRequest"/> and <see cref="IHttpResponse"/> instances to the handler)</param>
        void OnPost(string path, Func<IHttpRequest, IHttpResponse, Task> listener);

        /// <summary>
        /// Adds a <paramref name="listener"/> executed when PUT requests are received matching <paramref name="path"/>
        /// </summary>
        /// <param name="path">Only execute the <paramref name="listener"/> when PUT requests match this path. Can use tokens like <code>{id}</code> in the path with the values of these tokens available in <see cref="IHttpRequest.PathParams">IHttpRequest.PathParams</see>.</param>
        /// <param name="listener">Execute this async handler when a PUT request is received matching the <paramref name="path"/> (passes <see cref="IHttpRequest"/> and <see cref="IHttpResponse"/> instances to the handler)</param>
        void OnPut(string path, Func<IHttpRequest, IHttpResponse, Task> listener);

        /// <summary>
        /// Compile the web api
        /// </summary>
        void Compile();
    }
}
