/*
 * Copyright 2017 Fireshark Studios, LLC
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System.IO;
using System.Threading.Tasks;

namespace Butterfly.WebApi {
    public interface IHttpResponse {
        string GetHeader(string name);

        void SetHeader(string name, string value);

        int StatusCode { get; set; }

        string StatusText { get; set; }

        void SendRedirect(string url);

        Stream OutputStream { get; }

        Task WriteAsJsonAsync(object value);
    }
}
