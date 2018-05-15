using System;
using System.Text;

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
    }
}
