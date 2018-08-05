using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Butterfly.Core.Util {
    public static class StringX {
        public static string Hash(this string me) {
            var bytes = new UTF8Encoding().GetBytes(me);
            byte[] hashBytes;
            using (var algorithm = new System.Security.Cryptography.SHA512Managed()) {
                hashBytes = algorithm.ComputeHash(bytes);
            }
            return Convert.ToBase64String(hashBytes);
        }

        public static string Abbreviate(this string me) {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < me.Length; i++) {
                if (i == 0) {
                    sb.Append(me[i]);
                }
                else if (me[i - 1] == '_' || me[i - 1] == '-') {
                    sb.Append(me[i]);
                }
                else if (Char.IsLower(me[i - 1]) && Char.IsUpper(me[i])) {
                    sb.Append(me[i]);
                }
            }
            return sb.ToString();
        }

        // Based on https://stackoverflow.com/questions/31919830/splitting-values-by-commas-that-are-outside-parentheses
        static readonly Dictionary<string, Regex> SMART_SPLIT_REGEX_CHANGE = new Dictionary<string, Regex>();
        public static string[] SmartSplit(this string me, char delimiter = ',', char openBracket = '(', char closeBracket = ')') {
            string key = $"{delimiter}{openBracket}{closeBracket}";
            if (!SMART_SPLIT_REGEX_CHANGE.TryGetValue(key, out Regex regex)) {
                string regexText = @"(?:(?:\" + openBracket + @"(?>[^()]+|\" + openBracket + @"(?<number>)|\" + closeBracket + @"(?<-number>))*(?(number)(?!))\" + closeBracket + @")|[^" + delimiter + @"])+";
                //string regexText = @"(?:(?:\((?>[^()]+|\((?<number>)|\)(?<-number>))*(?(number)(?!))\))|[^,])+";
                regex = new Regex(regexText);
                SMART_SPLIT_REGEX_CHANGE[key] = regex;
            }
            return regex.Matches(me).Cast<Match>().Select(x => x.Value).ToArray();
        }
    }
}
