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

using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Util {
    public static class DictionaryX {
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
