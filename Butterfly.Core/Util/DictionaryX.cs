/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Core.Util {
    public static class DictionaryX {

        public static async Task<bool> SyncAsync(this Dict[] me, Dict[] toRecords, string[] keyFieldNames, Func<Dict, Task<bool>> insertFunc, Func<Dict, Dict, Task<bool>> updateFunc, Func<Dict, Dict, Task<bool>> deleteFunc, char keyFieldDelim = ';') {
            bool changed = false;
            if (me.Length == 0 && toRecords.Length == 0) return changed;

            List<object> existingIds = me.Length==0 ? new List<object>() : me.Select(x => x.GetKeyValue(keyFieldNames, delim: keyFieldDelim)).ToList();
            List<object> newIds = toRecords.Length == 0 ? new List<object>() : toRecords.Select(x => x.GetKeyValue(keyFieldNames, delim: keyFieldDelim, throwErrorIfMissingKeyField: false)).ToList();

            for (int i = 0; i < existingIds.Count; i++) {
                int newIndex = newIds.IndexOf(existingIds[i]);
                if (newIndex == -1) {
                    var keys = keyFieldNames.ToDictionary(x => x, x => me[i][x]);
                    bool deleteChanged = await deleteFunc(keys, me[i]);
                    if (deleteChanged) changed = true;
                }
                else if (!toRecords[newIndex].IsSame(me[i])) {
                    bool updateChanged = await updateFunc(me[i], toRecords[newIndex]);
                    if (updateChanged) changed = true;
                }
            }

            for (int i = 0; i < newIds.Count; i++) {
                int existingIndex = newIds[i] == null ? -1 : existingIds.IndexOf(newIds[i]);
                if (existingIndex == -1) {
                    bool insertChanged = await insertFunc(toRecords[i]);
                    if (insertChanged) changed = true;
                }
            }

            return changed;
        }

        public static async Task<Task<bool>[]> ConcurrentSyncAsync(this Dict[] me, Dict[] toRecords, string[] keyFieldNames, Func<Dict, Task<bool>> insertFunc, Func<Dict, Dict, Task<bool>> updateFunc, Func<Dict, Dict, Task<bool>> deleteFunc, SemaphoreSlim semaphoreSlim, char keyFieldDelim = ';') {
            if (me.Length == 0 && toRecords.Length == 0) return new Task<bool>[] { };

            List<object> existingIds = me.Length == 0 ? new List<object>() : me.Select(x => x.GetKeyValue(keyFieldNames, delim: keyFieldDelim)).ToList();
            List<object> newIds = toRecords.Length == 0 ? new List<object>() : toRecords.Select(x => x.GetKeyValue(keyFieldNames, delim: keyFieldDelim, throwErrorIfMissingKeyField: false)).ToList();

            List<Task<bool>> tasks = new List<Task<bool>>();
            for (int i = 0; i < existingIds.Count; i++) {
                int newIndex = newIds.IndexOf(existingIds[i]);
                if (newIndex == -1) {
                    var keys = keyFieldNames.ToDictionary(x => x, x => me[i][x]);
                    await semaphoreSlim.WaitAsync();
                    var task = deleteFunc(keys, me[i]);
                    var continueWithTask = task.ContinueWith(t => semaphoreSlim.Release());
                    tasks.Add(task);
                }
                else if (!toRecords[newIndex].IsSame(me[i])) {
                    await semaphoreSlim.WaitAsync();
                    var task = updateFunc(me[i], toRecords[newIndex]);
                    var continueWithTask = task.ContinueWith(t => semaphoreSlim.Release());
                    tasks.Add(task);
                }
            }

            for (int i = 0; i < newIds.Count; i++) {
                int existingIndex = newIds[i] == null ? -1 : existingIds.IndexOf(newIds[i]);
                if (existingIndex == -1) {
                    await semaphoreSlim.WaitAsync();
                    var task = insertFunc(toRecords[i]);
                    var continueWithTask = task.ContinueWith(t => semaphoreSlim.Release());
                    tasks.Add(task);
                }
            }

            return tasks.ToArray();
        }

        public static object GetKeyValue(this Dict me, string[] fieldNames, char delim = ';', bool throwErrorIfMissingKeyField = true) {
            StringBuilder sb = new StringBuilder();
            bool isFirst = true;
            foreach (var fieldName in fieldNames) {
                if (isFirst) isFirst = false;
                else sb.Append(delim);

                if (!me.ContainsKey(fieldName)) {
                    if (throwErrorIfMissingKeyField) throw new Exception($"Could not get key field '{fieldName}' to build key value");
                    return null;
                }
                else {
                    sb.Append(me[fieldName]);
                }
            }
            return sb.ToString();
        }

        public static Dict ParseKeyValue(object keyValue, string[] keyFieldNames, char delim = ';') {
            Dict result = new Dict();
            if (keyValue is string keyValueText) {
                string[] keyValueParts = keyValueText.Split(delim);
                for (int i = 0; i < keyFieldNames.Length; i++) {
                    result[keyFieldNames[i]] = keyValueParts[i];
                }
            }
            else if (keyFieldNames.Length == 1) {
                result[keyFieldNames[0]] = keyValue;
            }
            else {
                throw new Exception("Cannot parse key value that is not a string and keyFieldNames.Length!=1");
            }
            return result;
        }


        public static bool ContainsAnyKey<T, U>(this Dictionary<T, U> me, T[] keys) {
            foreach (var key in keys) {
                if (me.ContainsKey(key)) return true;
            }
            return false;
        }

        public static bool ContainsAllKeys<T, U>(this Dictionary<T, U> me, T[] keys) {
            foreach (var key in keys) {
                if (!me.ContainsKey(key)) return false;
            }
            return true;
        }

        public static string ToString<T, U>(this Dictionary<T, U> me, string keyValueDelim, string itemDelim) {
            return string.Join(itemDelim, me.Select(x => $"{x.Key}{keyValueDelim}{x.Value}"));
        }

        /// <summary>
        /// Retrieves a value from a <paramref name="me"/> (or return the <paramref name="defaultValue"/> if missing or null).<para/>
        /// </summary>
        /// <example>
        /// <code>
        /// // Prints "test"
        /// Console.WriteLine(new Dict{ ["text"] = "test" }.GetAs("text", ""));
        /// 
        /// // Prints "other"
        /// Console.WriteLine(new Dict{ }.GetAs("text", "other"));
        /// 
        /// // Prints 2
        /// Console.WriteLine(new Dict{ ["number"] = "2" }.GetAs("number", -1));
        /// </code>
        /// </example>
        /// <typeparam name="T">Dictionary key type</typeparam>
        /// <typeparam name="U">Dictionary value type</typeparam>
        /// <typeparam name="V">Return type</typeparam>
        /// <param name="me">Source dictionary</param>
        /// <param name="key">Key of the desired value in dictionary</param>
        /// <param name="defaultValue">Default value if value is null</param>
        /// <returns>A value of type V</returns>
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
