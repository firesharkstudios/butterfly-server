using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Butterfly.Util {
    public static class StringX {
        public static string Hash(this string me) {
            var bytes = new UTF8Encoding().GetBytes(me);
            byte[] hashBytes;
            using (var algorithm = new System.Security.Cryptography.SHA512Managed()) {
                hashBytes = algorithm.ComputeHash(bytes);
            }
            return Convert.ToBase64String(hashBytes);
        }
    }
}
