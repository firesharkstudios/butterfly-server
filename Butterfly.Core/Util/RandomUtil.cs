using System;
using System.Security.Cryptography;

namespace Butterfly.Core.Util {
    public static class RandomUtil {
        static readonly RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();

        public static int Next(int max) {
            byte[] rand = new byte[4];
            provider.GetBytes(rand);
            int i = BitConverter.ToUInt16(rand, 0);
            return i % max;
        }

    }
}
