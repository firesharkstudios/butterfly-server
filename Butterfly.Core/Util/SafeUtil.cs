/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Text;
using System.Text.RegularExpressions;

namespace Butterfly.Core.Util {
    // Based on http://kristjansson.us/?p=748
    // Removes these confusing pairs of letters/numbers...
    //   I, 1, l
    //   0, O
    //   B, 8
    //   Z, 2
    //   S, 5
    public static class SafeUtil {
        public const string FULL_SAFE_BASE_ALPHABET = "abcdefghijkmnopqrstuvwxyzACDEFGHJKLMNPQRTUVWXY34679";

        public static string GetRandom(int length, string alphabet = FULL_SAFE_BASE_ALPHABET) {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < length; i++) {
                int index = RandomUtil.Next(alphabet.Length);
                char ch = alphabet[index];
                sb.Append(ch);
            }
            return sb.ToString();
        }

        public static string ToSafeBase(ulong number, string alphabet = FULL_SAFE_BASE_ALPHABET) {
            StringBuilder sb = new StringBuilder();
            uint alphabetLength = (uint)alphabet.Length;
            do {
                sb.Insert(0, alphabet[(int)(number % alphabetLength)]);
                number = number / alphabetLength;
            }
            while (number != 0);
            return sb.ToString();
        }

        public static ulong FromSafeBase(string text, string alphabet = FULL_SAFE_BASE_ALPHABET) {
            ulong result = 0;
            uint alphabetLength = (uint)alphabet.Length;
            for (int i = 0; i < text.Length; i++) {
                result = (ulong)(result * alphabetLength) + (ulong)alphabet.IndexOf(text[i]);
            }
            return result;
        }

        public static string Scrub(string raw, string alphabet = FULL_SAFE_BASE_ALPHABET) {
            Regex regex = new Regex(@"[^" + alphabet + "]");
            return regex.Replace(raw.ToUpper(), "");
        }

        public static Match InvalidCharacterMatch(string raw, string alphabet = FULL_SAFE_BASE_ALPHABET) {
            Regex regex = new Regex(@"[^" + alphabet + "]");
            return regex.Match(raw);
        }
    }

}
