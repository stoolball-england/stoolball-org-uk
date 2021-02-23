using System;

namespace Stoolball.Data.SqlServer.IntegrationTests
{
    public static class DateTimeOffsetExtensions
    {
        public static DateTime AccurateToTheMinute(this DateTimeOffset dateTime)
        {
            return dateTime.Date.AddHours(dateTime.Hour).AddMinutes(dateTime.Minute);
        }
    }
}
