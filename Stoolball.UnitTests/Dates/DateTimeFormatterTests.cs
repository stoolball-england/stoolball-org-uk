using Stoolball.Dates;
using System;
using Xunit;

namespace Stoolball.UnitTests.Dates
{
    public class DateTimeFormatterTests
    {
        [Fact]
        public void Relative_date_should_return_today_for_same_date()
        {
            var now = new DateTime(2020, 5, 1);
            var dateToFormat = now;
            var dateFormatter = new DateTimeFormatter();

            var result = dateFormatter.FormatDate(dateToFormat, now, false, true, false);

            Assert.Equal("today", result);
        }

        [Fact]
        public void Relative_date_should_return_tomorrow_for_tomorrow()
        {
            var now = new DateTime(2020, 5, 1);
            var dateToFormat = now.AddDays(1);
            var dateFormatter = new DateTimeFormatter();

            var result = dateFormatter.FormatDate(dateToFormat, now, false, true, false);

            Assert.Equal("tomorrow", result);
        }

        [Fact]
        public void Relative_date_should_remove_same_year()
        {
            var now = new DateTime(2020, 5, 1);
            var dateToFormat = now.AddMonths(1);
            var dateFormatter = new DateTimeFormatter();

            var result = dateFormatter.FormatDate(dateToFormat, now, false, true, false);

            Assert.Equal("1 June", result);
        }
    }
}
