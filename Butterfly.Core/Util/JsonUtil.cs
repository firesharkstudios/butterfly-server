/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Butterfly.Core.Util {

    public static class JsonUtil {
        private readonly static Regex SCRUB_EXTRA_COMMAS_REGEX = new Regex(@"\,\s*(?=[\}\]])");

        private readonly static StringEnumConverter STRING_ENUM_CONVERTER = new StringEnumConverter();
        private readonly static IsoDateTimeConverter DATE_TIME_CONVERTER = new IsoDateTimeConverter() {
            DateTimeFormat = "yyyy-MM-dd HH:mm:ss"
        };

        private readonly static JsonConverter[] CONVERTERS = new JsonConverter[] { STRING_ENUM_CONVERTER, DATE_TIME_CONVERTER };

        public static T Deserialize<T>(string rawJson) {
            string json = ScrubExtraCommas(rawJson);
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static object Deserialize(Type type, string rawJson) {
            string json = ScrubExtraCommas(rawJson);
            return JsonConvert.DeserializeObject(json, type);
        }

        private static string ScrubExtraCommas(string json) {
            string result = SCRUB_EXTRA_COMMAS_REGEX.Replace(json, "");
            return result;
        }

        public static async Task<T> ReadJsonFile<T>(string file, bool throwErrorIfNotExists = false) {
            if (File.Exists(file)) {
                string json = await FileX.ReadTextAsync(file);
                return Deserialize<T>(json);
            }
            else if (throwErrorIfNotExists) {
                throw new Exception($"File {file} does not exist");
            }
            else {
                return default(T);
            }
        }

        public static void WriteToFile(object obj, string file, bool format = false, bool ignoreNulls = false) {
            string path = Path.GetDirectoryName(file);
            string tempFile = Path.Combine(path, "~" + Path.GetFileName(file));

            using (Stream stream = new FileStream(tempFile, FileMode.Create)) {
                WriteToStream(obj, stream, format, ignoreNulls);
            }

            if (File.Exists(file)) {
                File.Delete(file);
            }
            File.Move(tempFile, file);
        }

        public static void WriteToStream(object obj, Stream stream, bool format = false, bool ignoreNulls = false) {
            JsonSerializer serializer = new JsonSerializer();
            foreach (var converter in CONVERTERS) {
                serializer.Converters.Add(converter);
            }
            serializer.NullValueHandling = ignoreNulls ? NullValueHandling.Ignore : NullValueHandling.Include;
            serializer.Formatting = format ? Formatting.Indented : Formatting.None;
            using (StreamWriter writer = new StreamWriter(stream)) {
                serializer.Serialize(writer, obj);
            }
        }

        public static string Serialize(object obj, bool format = false, bool ignoreNulls = false) {
            JsonSerializerSettings settings = new JsonSerializerSettings {
                Converters = CONVERTERS,
                NullValueHandling = ignoreNulls ? NullValueHandling.Ignore : NullValueHandling.Include
            };
            return JsonConvert.SerializeObject(obj, format ? Formatting.Indented : Formatting.None, settings);
        }
    }

}
