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

using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json.Linq;

namespace Butterfly.Util {
    public static class DictionaryX {

        public static V GetAs<T, U, V>(this Dictionary<T, U> me, T key, V defaultValue) {
            if (me.TryGetValue(key, out U value) && value != null) {
                Type vType = typeof(V);
                Type nullableUnderlyingVType = Nullable.GetUnderlyingType(vType);
                if (nullableUnderlyingVType != null) {
                    if (value == null) {
                        return default(V);
                    }
                    else {
                        vType = nullableUnderlyingVType;
                    }
                }

                if (vType.IsEnum) {
                    return (V)Enum.Parse(vType, value.ToString(), true);
                }
                else if ((value is int || value is long) && vType==typeof(DateTime)) {
                    var longValue = (long)Convert.ChangeType(value, typeof(long));
                    return (V)(object)DateTimeX.FromUnixTimestamp(longValue);
                }
                else if (value is JObject) {
                    return (value as JObject).ToObject<V>();
                }
                else if (value is JArray) {
                    return (value as JArray).ToObject<V>();
                }
                else {
                    return (V)Convert.ChangeType(value, vType);
                }
            }
            else {
                return defaultValue;
            }
        }

        public static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> tuple, out T1 key, out T2 value) {
            key = tuple.Key;
            value = tuple.Value;
        }

        public static Dictionary<T, U> UpdateFrom<T, U>(this Dictionary<T, U> me, Dictionary<T, U> from) {
            if (from != null) {
                foreach (KeyValuePair<T, U> keyValuePair in from) {
                    me[keyValuePair.Key] = keyValuePair.Value;
                }
            }
            return me;
        }

        public static bool IsSame<T, U>(this Dictionary<T, U> me, Dictionary<T, U> other) {
            if (me.Count != other.Count) {
                return false;
            }
            else {
                foreach (var keyValuePair in me) {
                    if (keyValuePair.Value == null) {
                        if (other[keyValuePair.Key] != null) return false;
                    }
                    else if (!keyValuePair.Value.Equals(other[keyValuePair.Key])) {
                        return false;
                    }
                }
                return true;
            }
        }

        /*
         * Examples...
         *      {user: {name: 'Mark', email: 'mark@xyz.com'}, valid: true}.Format("Hello, {user.name}") returns "Hello, Mark"
         */
        public static string Format(this Dictionary<string, object> dictionary, string format, string paramOpenDelim = "{", string paramCloseDelim = "}") {
            if (format == null) {
                return null;
            }
            else {
                StringBuilder sb = new StringBuilder();
                int lastIndex = -1;
                while (true) {
                    int paramOpenIndex = format.IndexOf(paramOpenDelim, lastIndex + 1);
                    if (paramOpenIndex >= 0) {
                        int paramCloseIndex = format.IndexOf(paramCloseDelim, paramOpenIndex + 1);

                        string expression = format.Substring(paramOpenIndex + 1, paramCloseIndex - paramOpenIndex - 1);
                        string[] expressionParts = expression.Split('.');
                        Dictionary<string, object> currentDictionary = dictionary;
                        object value = null;
                        for (int i = 0; i < expressionParts.Length; i++) {
                            if (!currentDictionary.TryGetValue(expressionParts[i], out value)) {
                                //logger.Error("Format():Could not resolve expression '" + expression + "'");
                                break;
                            }
                            else if (i < expressionParts.Length - 1) {
                                currentDictionary = (Dictionary<string, object>)value;
                            }
                        }

                        sb.Append(format.Substring(lastIndex + 1, paramOpenIndex - lastIndex - 1));
                        sb.Append(value == null ? "" : value.ToString());
                        lastIndex = paramCloseIndex + paramCloseDelim.Length - 1;
                    }
                    else {
                        sb.Append(format.Substring(lastIndex + 1, format.Length - lastIndex - 1));
                        break;
                    }
                }
                return sb.ToString();
            }
        }

    }
}
