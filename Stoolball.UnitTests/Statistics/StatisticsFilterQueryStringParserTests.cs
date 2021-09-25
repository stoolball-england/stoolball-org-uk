using System;
using System.Collections.Specialized;
using System.Web;
using Stoolball.Statistics;
using Xunit;

namespace Stoolball.UnitTests.Statistics
{
    public class StatisticsFilterQueryStringParserTests
    {
        [Fact]
        public void Null_filter_throws_ArgumentNullException()
        {
            var parser = new StatisticsFilterQueryStringParser();

            Assert.Throws<ArgumentNullException>(() => _ = parser.ParseQueryString(null, new NameValueCollection()));
        }

        [Fact]
        public void Null_querystring_throws_ArgumentNullException()
        {
            var parser = new StatisticsFilterQueryStringParser();

            Assert.Throws<ArgumentNullException>(() => _ = parser.ParseQueryString(new StatisticsFilter(), null));
        }

        [Fact]
        public void Page_number_defaults_to_1()
        {
            var parser = new StatisticsFilterQueryStringParser();

            var result = parser.ParseQueryString(new StatisticsFilter(), new NameValueCollection());

            Assert.Equal(1, result.Paging.PageNumber);
        }

        [Fact]
        public void Page_number_is_parsed()
        {
            var parser = new StatisticsFilterQueryStringParser();

            var result = parser.ParseQueryString(new StatisticsFilter(), new NameValueCollection { { "page", "5" } });

            Assert.Equal(5, result.Paging.PageNumber);
        }

        [Theory]
        [InlineData("?to=2021-12-31")]
        [InlineData("?from=")]
        [InlineData("?from=2021-02-31")]
        [InlineData("?from=invalid")]
        public void Missing_empty_or_invalid_FromDate_is_null(string queryString)
        {
            var parser = new StatisticsFilterQueryStringParser();

            var filter = parser.ParseQueryString(new StatisticsFilter(), HttpUtility.ParseQueryString(queryString));

            Assert.Null(filter.FromDate);
        }

        [Fact]
        public void FromDate_is_parsed()
        {
            var parser = new StatisticsFilterQueryStringParser();

            var filter = parser.ParseQueryString(new StatisticsFilter(), HttpUtility.ParseQueryString("?from=2020-07-10"));

            Assert.Equal(new DateTime(2020, 7, 10), filter.FromDate.Value.Date);
        }

        [Theory]
        [InlineData("?from=2021-12-31")]
        [InlineData("?to=")]
        [InlineData("?to=2021-02-31")]
        [InlineData("?to=invalid")]
        public void Missing_empty_or_invalid_UntilDate_is_null(string queryString)
        {
            var parser = new StatisticsFilterQueryStringParser();

            var filter = parser.ParseQueryString(new StatisticsFilter(), HttpUtility.ParseQueryString(queryString));

            Assert.Null(filter.UntilDate);
        }

        [Fact]
        public void UntilDate_is_parsed()
        {
            var parser = new StatisticsFilterQueryStringParser();

            var filter = parser.ParseQueryString(new StatisticsFilter(), HttpUtility.ParseQueryString("to=2022-06-09"));

            Assert.Equal(new DateTime(2022, 6, 9), filter.UntilDate.Value.Date);
        }
    }
}
