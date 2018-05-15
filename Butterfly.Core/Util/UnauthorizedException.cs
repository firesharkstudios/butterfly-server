using System;

namespace Butterfly.Util {
    public class UnauthorizedException : Exception {
        public UnauthorizedException() : base("Unauthorized") {
        }
    }

    public class PermissionDeniedException : Exception {
        public PermissionDeniedException() : base("Permission denied") {
        }
    }
}
