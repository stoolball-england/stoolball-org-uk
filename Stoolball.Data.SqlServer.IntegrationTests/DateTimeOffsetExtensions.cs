using System;

namespace Stoolball.Data.SqlServer.IntegrationTests
{
    public static class DateTimeOffsetExtensions
    {
        public static DateTimeOffset AccurateToTheMinute(this DateTimeOffset dateTime)
        {
            return new DateTimeOffset(dateTime.Date.AddHours(dateTime.Hour).AddMinutes(dateTime.Minute), dateTime.Offset);
        }
    }
}
