using System;
using System.Globalization;

namespace Stoolball.Dates
{
    public static class DateTimeOffsetExtensions
    {
        /// <summary>
        /// Converts a regular DateTime to a RFC822 date string.
        /// </summary>
        /// <returns>The specified date formatted as a RFC822 date string.</returns>
        /// <remarks>From https://www.madskristensen.net/blog/convert-a-date-to-the-rfc822-standard-for-use-in-rss-feeds/</remarks>
        public static string ToRFC822(this DateTimeOffset date)
        {
            var offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).Hours;
            var timeZone = "+" + offset.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0');

            if (offset < 0)
            {
                offset = offset * -1;
                timeZone = "-" + offset.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0');
            }

            return date.ToString("ddd, dd MMM yyyy HH:mm:ss " + timeZone.PadRight(5, '0'), CultureInfo.InvariantCulture);
        }
    }
}
