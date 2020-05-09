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
        [InlineData("https://example.org/teams/example123/matches", StoolballRouteType.MatchesForTeam)]
        [InlineData("https://example.org/teams/example123/MATCHES/", StoolballRouteType.MatchesForTeam)]
        [InlineData("https://example.org/tournaments/example123/teams/example-team", StoolballRouteType.TransientTeam)]
        [InlineData("https://example.org/tournaments/example123/teams/example-team/", StoolballRouteType.TransientTeam)]
        [InlineData("https://example.org/locations/example", StoolballRouteType.MatchLocation)]
        [InlineData("https://example.org/locations/EXAMPLE-location/", StoolballRouteType.MatchLocation)]
        [InlineData("https://example.org/clubs/example-name/", StoolballRouteType.Club)]
        [InlineData("https://example.org/CLUBS/EXAMPLE", StoolballRouteType.Club)]
        [InlineData("https://example.org/competitions/example", StoolballRouteType.Competition)]
        [InlineData("https://example.org/competitions/example/", StoolballRouteType.Competition)]
        [InlineData("https://example.org/competitions/example/2020", StoolballRouteType.Season)]
        [InlineData("https://example.org/competitions/example/2020/", StoolballRouteType.Season)]
        [InlineData("https://example.org/competitions/example/2020/matches", StoolballRouteType.MatchesForSeason)]
        [InlineData("https://example.org/competitions/example/2020/MATCHES/", StoolballRouteType.MatchesForSeason)]
        [InlineData("https://example.org/competitions/example/2020-21/MATCHES", StoolballRouteType.MatchesForSeason)]
        [InlineData("https://example.org/competitions/example/2020-21/mAtChEs/", StoolballRouteType.MatchesForSeason)]
        [InlineData("https://example.org/matches/example-match", StoolballRouteType.Match)]
        [InlineData("https://example.org/matches/example-MaTcH/", StoolballRouteType.Match)]
        [InlineData("https://example.org/tournaments/example-tournament/", StoolballRouteType.Tournament)]
        [InlineData("https://example.org/tournaments/123-TOURNAMENT/", StoolballRouteType.Tournament)]
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
        [InlineData("https://example.org/teams/example/invalid")]
        [InlineData("https://example.org/teams/example123/teams/example-team/")]
        [InlineData("https://example.org/tournaments/example123/invalid/example-team/")]
        [InlineData("https://example.org/locations")]
        [InlineData("https://example.org/locations/")]
        [InlineData("https://example.org/competitions")]
        [InlineData("https://example.org/competitions/")]
        [InlineData("https://example.org/competitions/example/2020-")]
        [InlineData("https://example.org/competitions/example/2020/invalid")]
        [InlineData("https://example.org/competitions/example/2020-21/invalid/")]
        [InlineData("https://example.org/matches")]
        [InlineData("https://example.org/matches/")]
        [InlineData("https://example.org/tournaments")]
        [InlineData("https://example.org/tournamnents/")]
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
