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
using System.Text.RegularExpressions;

namespace Butterfly.Core.Util {
    public static class CommandLineUtil {

        public static Dictionary<string, string> Parse(string commandLine) {
            Dictionary<string, string> result = new Dictionary<string, string>();

            Match match = Regex.Match(" " + commandLine, @"(\s+|^)[\-](.+?)(?=(\s+\-|\s*$))");
            while (match.Success) {
                int posEquals = match.Groups[2].Value.IndexOf('=');
                int posSpace = match.Groups[2].Value.IndexOf(' ');
                int pos;
                if (posEquals>=0 && posSpace>=0) {
                    pos = Math.Min(posEquals, posSpace);
                }
                else if (posEquals>=0) {
                    pos = posEquals;
                }
                else if (posSpace>=0) {
                    pos = posSpace;
                }
                else {
                    pos = match.Groups[2].Value.Length;
                }
                string key = match.Groups[2].Value.Substring(0, pos).Trim();
                string value = match.Groups[2].Value.Substring(pos+1).Trim();
                result[key] = value;

                match = match.NextMatch();
            }

            return result;
        }

        public static string Build(Dictionary<string, string> switches) {
            StringBuilder sb = new StringBuilder();
            foreach (var keyValue in switches) {
                if (sb.Length > 0) sb.Append(" ");
                sb.Append("-");
                sb.Append(keyValue.Key);
                sb.Append("=");
                sb.Append(keyValue.Value);
            }
            return sb.ToString();
        }
    }

}
