using System;

namespace Stoolball.Testing
{
    public static class DateTimeOffsetExtensions
    {
        public static DateTimeOffset AccurateToTheMinute(this DateTimeOffset dateTime)
        {
            return new DateTimeOffset(dateTime.Date.AddHours(dateTime.Hour).AddMinutes(dateTime.Minute), dateTime.Offset);
        }
    }
}
