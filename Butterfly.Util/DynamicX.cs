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
using System.Reflection;

namespace Butterfly.Util {
    public static class DynamicX {
        public static ICollection<PropertyInfo> GetProperties(dynamic values) {
            return values.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
        }

        public static ICollection<string> GetKeys(dynamic values) {
            PropertyInfo[] properties = GetProperties(values);
            return Array.ConvertAll(properties, x => x.Name);
        }

        public static Dictionary<string, object> ToDictionary(object values) {
            var dictionary = new Dictionary<string, object>();
            var properties = GetProperties(values);
            foreach (var property in properties) {
                object obj = property.GetValue(values);
                dictionary.Add(property.Name, obj);
            }
            return dictionary;
        }
    }
}
