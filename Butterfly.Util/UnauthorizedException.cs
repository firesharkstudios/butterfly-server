using System;

namespace Butterfly.Util {
    public class UnauthorizedException : Exception {
        public UnauthorizedException() : base("Unauthorized") {
        }
    }

}
