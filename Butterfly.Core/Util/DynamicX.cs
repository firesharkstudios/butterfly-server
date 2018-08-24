/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace Butterfly.Core.Util {
    public static class DynamicX {
        public static ICollection<PropertyInfo> GetProperties(dynamic values) {
            PropertyInfo[] propertyInfos = values.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            return propertyInfos.Where(x => x.GetIndexParameters().Length == 0).ToArray();
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
