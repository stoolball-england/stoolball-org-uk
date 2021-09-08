using System;
using System.Globalization;
using Moq;
using Stoolball.Dates;
using Stoolball.Matches;
using Xunit;

namespace Stoolball.UnitTests.Matches
{
    public class MatchFilterHumanizerTests
    {
        [Fact]
        public void FromDate_is_humanized()
        {
            var filter = new MatchFilter { FromDate = DateTimeOffset.Now };
            var dateTimeFormatter = new Mock<IDateTimeFormatter>();
            dateTimeFormatter.Setup(x => x.FormatDate(filter.FromDate.Value, false, true, false)).Returns(filter.FromDate.Value.ToString("d", CultureInfo.CurrentCulture));
            var humanizer = new MatchFilterHumanizer(dateTimeFormatter.Object);

            var result = humanizer.Humanize(filter);

            dateTimeFormatter.Verify(x => x.FormatDate(filter.FromDate.Value, false, true, false), Times.Once);
            Assert.Equal(result, " from " + filter.FromDate.Value.ToString("d", CultureInfo.CurrentCulture) + " onwards");
        }

        [Fact]
        public void UntilDate_is_humanized()
        {
            var filter = new MatchFilter { UntilDate = DateTimeOffset.Now };
            var dateTimeFormatter = new Mock<IDateTimeFormatter>();
            dateTimeFormatter.Setup(x => x.FormatDate(filter.UntilDate.Value, false, true, false)).Returns(filter.UntilDate.Value.ToString("d", CultureInfo.CurrentCulture));
            var humanizer = new MatchFilterHumanizer(dateTimeFormatter.Object);

            var result = humanizer.Humanize(filter);

            dateTimeFormatter.Verify(x => x.FormatDate(filter.UntilDate.Value, false, true, false), Times.Once);
            Assert.Equal(result, " up to " + filter.UntilDate.Value.ToString("d", CultureInfo.CurrentCulture));
        }

        [Fact]
        public void FromDate_and_UntilDate_are_humanized()
        {
            var filter = new MatchFilter { FromDate = DateTimeOffset.Now, UntilDate = DateTimeOffset.Now.AddDays(1) };
            var dateTimeFormatter = new Mock<IDateTimeFormatter>();
            dateTimeFormatter.Setup(x => x.FormatDate(filter.FromDate.Value, false, true, false)).Returns(filter.FromDate.Value.ToString("d", CultureInfo.CurrentCulture));
            dateTimeFormatter.Setup(x => x.FormatDate(filter.UntilDate.Value, false, true, false)).Returns(filter.UntilDate.Value.ToString("d", CultureInfo.CurrentCulture));
            var humanizer = new MatchFilterHumanizer(dateTimeFormatter.Object);

            var result = humanizer.Humanize(filter);

            dateTimeFormatter.Verify(x => x.FormatDate(filter.FromDate.Value, false, true, false), Times.Once);
            dateTimeFormatter.Verify(x => x.FormatDate(filter.UntilDate.Value, false, true, false), Times.Once);
            Assert.Equal(result, " from " + filter.FromDate.Value.ToString("d", CultureInfo.CurrentCulture) + " up to " + filter.UntilDate.Value.ToString("d", CultureInfo.CurrentCulture));
        }
    }
}
