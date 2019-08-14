/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Butterfly.Core.WebApi;
using System.Collections.Generic;
using System.Threading.Tasks;

using Dict = System.Collections.Generic.Dictionary<string, object>;

using System.IO;
using System.Linq;

namespace Butterfly.Core.Util {
    public static class IHttpResponseX {
        /// <summary>
        /// Write the <paramref name="value"/> as JSON
        /// </summary>
        /// <param name="me"></param>
        /// <param name="value"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        public static Task WriteAsJsonAsync(this IHttpResponse me, object value, string contentType = "application/json") {
            var json = JsonUtil.Serialize(value);
            return me.WriteAsTextAsync(json, contentType);
        }

        /// <summary>
        /// Write the <paramref name="rows"/> as CSV
        /// </summary>
        /// <param name="me"></param>
        /// <param name="rows"></param>
        /// <param name="fieldNames"></param>
        /// <param name="headings"></param>
        /// <returns></returns>
        public static async Task WriteAsCsvAsync(this IHttpResponse me, Dict[] rows, string[] fieldNames, string[] headings = null) {
            var newHeadings = headings ?? fieldNames;
            if (newHeadings.Length != fieldNames.Length) throw new System.Exception("Length of fieldNames and headings parameters must be the same");

            using (var writer = new StreamWriter(me.OutputStream)) {
                var headerText = string.Join(",", newHeadings.Select(x => $"\"{x.Replace("\"", "\\\"")}\""));
                await writer.WriteLineAsync(headerText);
                foreach (var row in rows) {
                    var rowText = string.Join(",", fieldNames.Select(x => row.GetAs(x, "")));
                    await writer.WriteLineAsync(rowText);
                }
            }
        }
    }
}
