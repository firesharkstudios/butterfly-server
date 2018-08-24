/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Threading;

namespace Butterfly.Core.Util {
    public static class ConsoleUtil {
        public static void WaitForCancelKey() {
            ManualResetEvent quitEvent = new ManualResetEvent(false);
            Console.CancelKeyPress += (sender, eArgs) => {
                quitEvent.Set();
            };
            quitEvent.WaitOne();
        }
    }
}
