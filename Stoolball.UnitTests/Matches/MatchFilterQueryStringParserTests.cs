using System;
using System.Collections.Specialized;
using System.Web;
using Stoolball.Matches;
using Xunit;

namespace Stoolball.Web.Tests.Matches
{
    public class MatchFilterQueryStringParserTests
    {
        [Fact]
        public void Null_filter_throws_ArgumentNullException()
        {
            var parser = new MatchFilterQueryStringParser();

            Assert.Throws<ArgumentNullException>(() => _ = parser.ParseQueryString(null, new NameValueCollection()));
        }

        [Fact]
        public void Null_querystring_throws_ArgumentNullException()
        {
            var parser = new MatchFilterQueryStringParser();

            Assert.Throws<ArgumentNullException>(() => _ = parser.ParseQueryString(new MatchFilter(), null));
        }

        [Theory]
        [InlineData("?to=2021-12-31")]
        [InlineData("?from=")]
        [InlineData("?from=2021-02-31")]
        [InlineData("?from=invalid")]
        public void Missing_empty_or_invalid_FromDate_is_null(string queryString)
        {
            var parser = new MatchFilterQueryStringParser();

            var filter = parser.ParseQueryString(new MatchFilter(), HttpUtility.ParseQueryString(queryString));

            Assert.Null(filter.FromDate);
        }

        [Fact]
        public void FromDate_is_parsed()
        {
            var parser = new MatchFilterQueryStringParser();

            var filter = parser.ParseQueryString(new MatchFilter(), HttpUtility.ParseQueryString("?from=2020-07-10"));

            Assert.Equal(new DateTime(2020, 7, 10), filter.FromDate.Value.Date);
        }

        [Theory]
        [InlineData("?from=2021-12-31")]
        [InlineData("?to=")]
        [InlineData("?to=2021-02-31")]
        [InlineData("?to=invalid")]
        public void Missing_empty_or_invalid_UntilDate_is_null(string queryString)
        {
            var parser = new MatchFilterQueryStringParser();

            var filter = parser.ParseQueryString(new MatchFilter(), HttpUtility.ParseQueryString(queryString));

            Assert.Null(filter.UntilDate);
        }

        [Fact]
        public void UntilDate_is_parsed()
        {
            var parser = new MatchFilterQueryStringParser();

            var filter = parser.ParseQueryString(new MatchFilter(), HttpUtility.ParseQueryString("to=2022-06-09"));

            Assert.Equal(new DateTime(2022, 6, 9), filter.UntilDate.Value.Date);
        }

        [Fact]
        public void Query_is_parsed_and_trimmed()
        {
            var parser = new MatchFilterQueryStringParser();

            var filter = parser.ParseQueryString(new MatchFilter(), HttpUtility.ParseQueryString("q=%20hello%20world%20"));

            Assert.Equal("hello world", filter.Query);
        }
    }
}
