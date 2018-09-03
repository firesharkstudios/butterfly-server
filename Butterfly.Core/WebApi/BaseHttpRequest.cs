/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using HttpMultipartParser;

using Butterfly.Core.Util;
using NLog;
using System.Web;

namespace Butterfly.Core.WebApi {
    public abstract class BaseHttpRequest : IHttpRequest {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected abstract Stream InputStream { get; }

        public abstract Uri RequestUrl { get; }

        public abstract Dictionary<string, string> Headers { get; }

        public abstract Dictionary<string, string> PathParams { get; }

        public abstract Dictionary<string, string> QueryParams { get; }

        public async Task<Dictionary<string, string>> ParseAsUrlEncodedAsync() {
            using (StreamReader reader = new StreamReader(this.InputStream)) {
                string text = await reader.ReadToEndAsync();
                return HttpUtility.ParseQueryString(text).ToDictionary();
            }
        }

        public async Task<T> ParseAsJsonAsync<T>() {
            using (StreamReader reader = new StreamReader(this.InputStream)) {
                string json = await reader.ReadToEndAsync();
                return JsonUtil.Deserialize<T>(json);
            }
        }

        public void ParseAsMultipartStream(Action<string, string, string, string, byte[], int> onData, Action<string, string> onParameter = null) {
            logger.Debug("ParseAsMultipartStream()");
            var parser = new StreamingMultipartFormDataParser(this.InputStream);
            if (onParameter != null) {
                parser.ParameterHandler += parameter => onParameter(parameter.Name, parameter.Data);
            }
            parser.FileHandler += (name, fileName, type, disposition, buffer, bytes) => onData(name, fileName, type, disposition, buffer, bytes);
            parser.Run();
        }

    }
}
