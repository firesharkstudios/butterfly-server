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

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Butterfly.Util {
    public static class DictionaryX {

        public static V GetAs<T, U, V>(this Dictionary<T, U> me, T key, V defaultValue) {
            if (me.TryGetValue(key, out U value) && value != null) {
                if (value is JObject) {
                    return (value as JObject).ToObject<V>();
                }
                else if (value is JArray) {
                    return (value as JArray).ToObject<V>();
                }
                else {
                    return (V)Convert.ChangeType(value, typeof(V));
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
    }
}
