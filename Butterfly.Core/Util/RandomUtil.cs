/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

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
