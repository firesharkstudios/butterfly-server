/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

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
