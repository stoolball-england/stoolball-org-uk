using Stoolball.Web.Routing;
using System;
using Xunit;

namespace Stoolball.Web.Tests.Routing
{
    public class StoolballRouteContentFinderTests
    {
        [Theory]
        [InlineData("https://example.org/teams/example123", StoolballRouteType.Team)]
        [InlineData("https://example.org/teams/example123/", StoolballRouteType.Team)]
        [InlineData("https://example.org/locations/example", StoolballRouteType.MatchLocation)]
        [InlineData("https://example.org/locations/EXAMPLE-location/", StoolballRouteType.MatchLocation)]
        [InlineData("https://example.org/clubs/example-name/", StoolballRouteType.Club)]
        [InlineData("https://example.org/CLUBS/EXAMPLE", StoolballRouteType.Club)]
        [InlineData("https://example.org/competitions/example", StoolballRouteType.Competition)]
        [InlineData("https://example.org/competitions/example/", StoolballRouteType.Competition)]
        [InlineData("https://example.org/competitions/example/2020", StoolballRouteType.Season)]
        [InlineData("https://example.org/competitions/example/2020/", StoolballRouteType.Season)]
        [InlineData("https://example.org/competitions/example/2020-21", StoolballRouteType.Season)]
        [InlineData("https://example.org/competitions/example/2020-21/", StoolballRouteType.Season)]
        public void Correct_route_should_match(string route, StoolballRouteType expectedType)
        {
            var requestUrl = new Uri(route);

            var result = StoolballRouteContentFinder.MatchStoolballRouteType(requestUrl);

            Assert.Equal(expectedType, result);
        }

        [Theory]
        [InlineData("https://example.org/clubs")]
        [InlineData("https://example.org/clubs/")]
        [InlineData("https://example.org/clubs/example/invalid")]
        [InlineData("https://example.org/teams")]
        [InlineData("https://example.org/teams/")]
        [InlineData("https://example.org/locations")]
        [InlineData("https://example.org/locations/")]
        [InlineData("https://example.org/competitions")]
        [InlineData("https://example.org/competitions/")]
        [InlineData("https://example.org/competitions/example/2020-")]
        [InlineData("https://example.org/other")]
        [InlineData("https://example.org/other/")]
        public void Other_route_should_not_match(string route)
        {
            var requestUrl = new Uri(route);

            var result = StoolballRouteContentFinder.MatchStoolballRouteType(requestUrl);

            Assert.Null(result);
        }
    }
}
