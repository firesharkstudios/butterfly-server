using System;
using System.Collections.Generic;

namespace Butterfly.Core.Util {
    public class Throttle {

        protected readonly Dictionary<string, List<DateTime>> dateTimesByKey = new Dictionary<string, List<DateTime>>();

        public float seconds;
        public int count;

        public Throttle(float seconds, int limit) {
            this.seconds = seconds;
            this.count = limit;
        }

        public int Check(string key, int requestCount = 1) {
            List<DateTime> dateTimes;
            lock (this.dateTimesByKey) {
                if (this.dateTimesByKey.TryGetValue(key, out dateTimes)) {
                    DateTime min = DateTime.Now.AddSeconds(-this.seconds);
                    lock (dateTimes) {
                        int removeCount = dateTimes.Count;
                        for (int i = 0; i < dateTimes.Count; i++) {
                            if (dateTimes[i] > min) {
                                removeCount = i;
                                break;
                            }
                        }

                        if (removeCount > 0) dateTimes.RemoveRange(0, removeCount);
                    }
                }
                else {
                    dateTimes = new List<DateTime>();
                    this.dateTimesByKey.Add(key, dateTimes);
                }
            }

            int acceptedCount = Math.Min(requestCount, this.count - dateTimes.Count);
            if (acceptedCount < 0) acceptedCount = 0;

            for (int i=0; i<acceptedCount; i++) {
                dateTimes.Add(DateTime.Now);
            }

            return acceptedCount;
        }

    }
}
