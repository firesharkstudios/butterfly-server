using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using HttpMultipartParser;

using Butterfly.Util;

namespace Butterfly.WebApi {
    public abstract class BaseHttpRequest : IHttpRequest {
        protected abstract Stream InputStream { get; }

        public abstract Uri RequestUri { get; }

        public abstract Dictionary<string, string> Headers { get; }

        public abstract Dictionary<string, string> PathParams { get; }

        public abstract Dictionary<string, string> QueryParams { get; }

        public async Task<T> ParseAsJsonAsync<T>() {
            using (StreamReader reader = new StreamReader(this.InputStream)) {
                string json = await reader.ReadToEndAsync();
                return JsonUtil.Deserialize<T>(json);
            }
        }

        public void ParseAsMultipartStreamAsync(Action<string, string, string, string, byte[], int> onData, Action onDone = null, Action<string, string> onParameter = null) {
            var parser = new StreamingMultipartFormDataParser(this.InputStream);
            if (onParameter != null) {
                parser.ParameterHandler += parameter => onParameter(parameter.Name, parameter.Data);
            }
            parser.FileHandler += (name, fileName, type, disposition, buffer, bytes) => onData(name, fileName, type, disposition, buffer, bytes);
            if (onDone != null) {
                parser.StreamClosedHandler += () => onDone();
            }
            parser.Run();
        }

    }
}
