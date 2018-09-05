/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Threading.Tasks;

namespace Butterfly.Core.WebApi {
    /// <summary>
    /// Allows handling requests/responses to a Web API via HTTP (inspired by Node.js' Express)<para/>
    /// </summary>
    /// <example>
    /// <code>
    /// webApi.OnGet("/chat-messages", async(req, res) => {
    /// // Handle the request and return any objects as needed (will be JSON encoded)
    /// });
    /// webApi.OnPost("/login", async(req, res) => {
    /// // Handle the request and return any objects as needed (will be JSON encoded)
    /// });
    /// webApi.Compile();
    /// </code>
    /// </example>
    public interface IWebApi : IDisposable {
        /// <summary>
        /// Adds a <paramref name="listener"/> executed when DELETE requests are received matching <paramref name="path"/>
        /// </summary>
        /// <example>
        /// <code>
        /// webApi.OnDelete("/api/todo", async(req, res) => {
        /// var id = await req.ParseAsJsonAsync&lt;string&gt;();
        /// await database.DeleteAndCommitAsync("todo", id);
        /// });
        /// webApi.OnDelete("/api/todo/{id}", async(req, res) => {
        /// var id = req.PathParams.GetAs("id", "");
        /// await database.DeleteAndCommitAsync("todo", id);
        /// });
        /// </code>    
        /// </example>
        /// <param name="path">Only execute the <paramref name="listener"/> when DELETE requests match this path.</param>
        /// <param name="listener">Execute this async handler when a DELETE request is received matching the <paramref name="path"/></param>
        void OnDelete(string path, Func<IHttpRequest, IHttpResponse, Task> listener);

        /// <summary>
        /// Adds a <paramref name="listener"/> executed when GET requests are received matching <paramref name="path"/>
        /// </summary>
        /// <example>
        /// <code>
        /// webApi.OnGet("/api/todos", async(req, res) => {
        ///     Dict[] rows = await database.SelectRowsAsync("SELECT * FROM todo");
        ///     await res.WriteAsJsonAsync(rows);
        /// });
        /// webApi.OnGet("/api/todo/{id}", async(req, res) => {
        ///     var id = req.PathParams.GetAs("id", "");
        ///     Dict row = await database.SelectRowAsync("SELECT * FROM todo", id);
        ///     await res.WriteAsJsonAsync(row);
        /// });
        /// </code>
        /// </example>
        /// <param name="path">Only execute the <paramref name="listener"/> when GET requests match this path.</param>
        /// <param name="listener">Execute this async handler when a GET request is received matching the <paramref name="path"/></param>
        void OnGet(string path, Func<IHttpRequest, IHttpResponse, Task> listener);

        /// <summary>
        /// Adds a <paramref name="listener"/> executed when POST requests are received matching <paramref name="path"/>
        /// </summary>
        /// <example>
        /// <code>
        /// webApi.OnPost("/api/todo", async(req, res) => {
        ///     var row = await req.ParseAsJsonAsync&lt;Dict&gt;();
        ///     await database.InsertAndCommitAsync&lt;string&gt;("todo", row);
        /// });
        /// </code>
        /// </example>
        /// <param name="path">Only execute the <paramref name="listener"/> when POST requests match this path.</param>
        /// <param name="listener">Execute this async handler when a POST request is received matching the <paramref name="path"/></param>
        void OnPost(string path, Func<IHttpRequest, IHttpResponse, Task> listener);

        /// <summary>
        /// Adds a <paramref name="listener"/> executed when PUT requests are received matching <paramref name="path"/>
        /// </summary>
        /// <example>
        /// <code>
        /// webApi.OnPost("/api/todo", async(req, res) => {
        ///     var row = await req.ParseAsJsonAsync&lt;Dict&gt;();
        ///     await database.UpdateAndCommitAsync("todo", row);
        /// });
        /// </code>
        /// </example>
        /// <param name="path">Only execute the <paramref name="listener"/> when PUT requests match this path.</param>
        /// <param name="listener">Execute this async handler when a PUT request is received matching the <paramref name="path"/></param>
        void OnPut(string path, Func<IHttpRequest, IHttpResponse, Task> listener);

        /// <summary>
        /// Compile the web api (must call after adding all handlers)
        /// </summary>
        void Compile();
    }
}
