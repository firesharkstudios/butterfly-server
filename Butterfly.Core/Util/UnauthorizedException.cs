/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;

namespace Butterfly.Core.Util {
    public class UnauthorizedException : Exception {
        public UnauthorizedException() : base("Unauthorized") {
        }
    }

    public class PermissionDeniedException : Exception {
        public PermissionDeniedException() : base("Permission denied") {
        }
    }
}
