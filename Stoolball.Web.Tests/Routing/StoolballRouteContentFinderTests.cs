using Stoolball.Web.Routing;
using System;
using Xunit;

namespace Stoolball.Web.Tests.Routing
{
    public class StoolballRouteContentFinderTests
    {
        [Theory]
        [InlineData("https://example.org/clubs", StoolballRouteType.Club)]
        [InlineData("https://example.org/teams/", StoolballRouteType.Team)]
        [InlineData("https://example.org/locations/example", StoolballRouteType.MatchLocation)]
        [InlineData("https://example.org/clubs/example-name/", StoolballRouteType.Club)]
        [InlineData("https://example.org/CLUBS/EXAMPLE", StoolballRouteType.Club)]
        public void Club_route_should_match(string route, StoolballRouteType expectedType)
        {
            var requestUrl = new Uri(route);

            var result = StoolballRouteContentFinder.MatchStoolballRouteType(requestUrl);

            Assert.Equal(expectedType, result);
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
