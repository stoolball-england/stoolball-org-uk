using Stoolball.Web.Routing;
using System;
using Xunit;

namespace Stoolball.Web.Tests.Routing
{
    public class StoolballRouteContentFinderTests
    {
        [Theory]
        [InlineData("https://example.org/clubs")]
        [InlineData("https://example.org/clubs/")]
        [InlineData("https://example.org/clubs/example")]
        [InlineData("https://example.org/clubs/example-name/")]
        [InlineData("https://example.org/CLUBS/EXAMPLE")]
        public void Club_route_should_match(string route)
        {
            var requestUrl = new Uri(route);

            var result = StoolballRouteContentFinder.MatchStoolballRouteType(requestUrl);

            Assert.Equal(StoolballRouteType.Club, result);
        }

        [Theory]
        [InlineData("https://example.org/other")]
        [InlineData("https://example.org/clubs/example/invalid")]
        public void Other_route_should_not_match(string route)
        {
            var requestUrl = new Uri(route);

            var result = StoolballRouteContentFinder.MatchStoolballRouteType(requestUrl);

            Assert.Null(result);
        }
    }
}
