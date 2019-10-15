/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using NLog;

namespace Butterfly.Core.WebApi {

    /// <inheritdoc/>
    /// <summary>
    /// Base class implementing <see cref="IWebApi"/>. New implementations will normally extend this class.
    /// </summary>
    public abstract class BaseWebApi : IWebApi {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly List<WebHandler> webHandlers = new List<WebHandler>();

        public void OnDelete(string path, Func<IHttpRequest, IHttpResponse, Task> listener) {
            logger.Debug($"OnDelete():path={path}");
            webHandlers.Add(new WebHandler {
                method = HttpMethod.Delete,
                path = path,
                listener = listener
            });
        }

        public void OnGet(string path, Func<IHttpRequest, IHttpResponse, Task> listener) {
            logger.Debug($"OnGet():path={path}");
            webHandlers.Add(new WebHandler {
                method = HttpMethod.Get,
                path = path,
                listener = listener
            });
        }

        public void OnPost(string path, Func<IHttpRequest, IHttpResponse, Task> listener) {
            logger.Debug($"OnPost():path={path}");
            webHandlers.Add(new WebHandler {
                method = HttpMethod.Post,
                path = path,
                listener = listener
            });
        }

        public void OnPut(string path, Func<IHttpRequest, IHttpResponse, Task> listener) {
            logger.Debug($"OnPut():path={path}");
            webHandlers.Add(new WebHandler {
                method = HttpMethod.Put,
                path = path,
                listener = listener
            });
        }

        public List<WebHandler> WebHandlers {
            get {
                return this.webHandlers;
            }
        }

        public abstract void Compile();
        public abstract void Dispose();

    }
}
