using System;

namespace Butterfly.Core.Util {
    public static class DateTimeX {

        private static readonly DateTime EPOCH = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long ToUnixTimestamp(this DateTime target) {
            DateTime targetUtc = TimeZoneInfo.ConvertTimeToUtc(target);
            return (long)(targetUtc - EPOCH).TotalSeconds;
        }

        public static DateTime FromUnixTimestamp(long timestamp) {
            DateTime dateTime = EPOCH;
            return TimeZoneInfo.ConvertTimeFromUtc(dateTime.AddSeconds(timestamp), TimeZoneInfo.Local);
        }

    }
}