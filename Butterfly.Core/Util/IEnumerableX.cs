using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core.Util {
    public static class IEnumerableX {
        // Per https://stackoverflow.com/questions/3669970/compare-two-listt-objects-for-equality-ignoring-order
        public static bool HasSameElements<T>(this ICollection<T> me, ICollection<T> other) {
            var cnt = new Dictionary<T, int>();
            foreach (T s in me) {
                if (cnt.ContainsKey(s)) {
                    cnt[s]++;
                }
                else {
                    cnt.Add(s, 1);
                }
            }
            foreach (T s in other) {
                if (cnt.ContainsKey(s)) {
                    cnt[s]--;
                }
                else {
                    return false;
                }
            }
            return cnt.Values.All(c => c == 0);
        }
    }
}
