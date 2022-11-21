using System;

namespace Stoolball.Testing
{
    internal static class DateTimeOffsetExtensions
    {
        internal static DateTimeOffset AccurateToTheMinute(this DateTimeOffset dateTime)
        {
            return new DateTimeOffset(dateTime.Date.AddHours(dateTime.Hour).AddMinutes(dateTime.Minute), dateTime.Offset);
        }
    }
}
