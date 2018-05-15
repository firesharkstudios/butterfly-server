using System;

namespace Butterfly.Core.Util {
    public static class EnvironmentX {
        public static bool IsRunningOnMono() {
            return Type.GetType("Mono.Runtime") != null;
        }
    }
}
