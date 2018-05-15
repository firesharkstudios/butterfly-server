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
