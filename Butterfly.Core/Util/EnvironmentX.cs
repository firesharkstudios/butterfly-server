using System;

namespace Butterfly.Util {
    public static class EnvironmentX {
        public static bool IsRunningOnMono() {
            return Type.GetType("Mono.Runtime") != null;
        }
    }
}
