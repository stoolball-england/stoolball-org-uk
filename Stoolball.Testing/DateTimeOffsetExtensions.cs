using System;

namespace Stoolball.Testing
{
    internal static class DateTimeOffsetExtensions
    {
        internal static DateTimeOffset UtcToUkTime(this DateTimeOffset dateTime)
        {
            // create a date accurate to the minute, otherwise integration tests can fail due to fractions of a second which are never seen in real data
            return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(new DateTimeOffset(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0, TimeSpan.Zero), Constants.UkTimeZone());
        }

        internal static DateTimeOffset AccurateToTheMinute(this DateTimeOffset dateTime)
        {
            return new DateTimeOffset(dateTime.Date.AddHours(dateTime.Hour).AddMinutes(dateTime.Minute), dateTime.Offset);
        }
    }
}
