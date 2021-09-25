using System.Collections.Specialized;
using Stoolball.Statistics;
using Xunit;

namespace Stoolball.UnitTests.Statistics
{
    public class StatisticsFilterQueryStringParserTests
    {
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
    }
}
