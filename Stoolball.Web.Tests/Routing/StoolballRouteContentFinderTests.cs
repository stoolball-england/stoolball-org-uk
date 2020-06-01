using Stoolball.Web.Routing;
using System;
using Xunit;

namespace Stoolball.Web.Tests.Routing
{
    public class StoolballRouteContentFinderTests
    {
        [Theory]
        [InlineData("https://example.org/teams", StoolballRouteType.Teams)]
        [InlineData("https://example.org/teams/", StoolballRouteType.Teams)]
        [InlineData("https://example.org/teams/add", StoolballRouteType.CreateTeam)]
        [InlineData("https://example.org/teams/Add/", StoolballRouteType.CreateTeam)]
        [InlineData("https://example.org/teams/example123", StoolballRouteType.Team)]
        [InlineData("https://example.org/teams/example123/", StoolballRouteType.Team)]
        [InlineData("https://example.org/teams/example123/EDiT", StoolballRouteType.EditTeam)]
        [InlineData("https://example.org/teams/example123/matches", StoolballRouteType.MatchesForTeam)]
        [InlineData("https://example.org/teams/example123/MATCHES/", StoolballRouteType.MatchesForTeam)]
        [InlineData("https://example.org/tournaments/example123/teams/example-team", StoolballRouteType.TransientTeam)]
        [InlineData("https://example.org/tournaments/example123/teams/example-team/", StoolballRouteType.TransientTeam)]
        [InlineData("https://example.org/tournaments/example123/teams/example-team/EDIT", StoolballRouteType.EditTransientTeam)]
        [InlineData("https://example.org/tournaments/example123/teams/example-TEAM/edit/", StoolballRouteType.EditTransientTeam)]
        [InlineData("https://example.org/locations", StoolballRouteType.MatchLocations)]
        [InlineData("https://example.org/locations/", StoolballRouteType.MatchLocations)]
        [InlineData("https://example.org/locations/add", StoolballRouteType.CreateMatchLocation)]
        [InlineData("https://example.org/locations/ADD/", StoolballRouteType.CreateMatchLocation)]
        [InlineData("https://example.org/locations/example", StoolballRouteType.MatchLocation)]
        [InlineData("https://example.org/locations/EXAMPLE-location/", StoolballRouteType.MatchLocation)]
        [InlineData("https://example.org/locations/EXAMPLE-location/edit", StoolballRouteType.MatchLocationActions)]
        [InlineData("https://example.org/locations/EXAMPLE-location/edit/location", StoolballRouteType.EditMatchLocation)]
        [InlineData("https://example.org/locations/EXAMPLE-location/EDIT/location/", StoolballRouteType.EditMatchLocation)]
        [InlineData("https://example.org/locations/EXAMPLE-location/delete", StoolballRouteType.DeleteMatchLocation)]
        [InlineData("https://example.org/clubs", StoolballRouteType.Clubs)]
        [InlineData("https://example.org/clubs/", StoolballRouteType.Clubs)]
        [InlineData("https://example.org/clubs/example-name/", StoolballRouteType.Club)]
        [InlineData("https://example.org/clubs/example-name/edit", StoolballRouteType.EditClub)]
        [InlineData("https://example.org/clubs/example-name/EDit/", StoolballRouteType.EditClub)]
        [InlineData("https://example.org/clubs/ADD", StoolballRouteType.CreateClub)]
        [InlineData("https://example.org/clubs/add/", StoolballRouteType.CreateClub)]
        [InlineData("https://example.org/CLUBS/EXAMPLE", StoolballRouteType.Club)]
        [InlineData("https://example.org/competitions/add", StoolballRouteType.CreateCompetition)]
        [InlineData("https://example.org/COMPETITIONS/add/", StoolballRouteType.CreateCompetition)]
        [InlineData("https://example.org/competitions/example", StoolballRouteType.Competition)]
        [InlineData("https://example.org/competitions/example/", StoolballRouteType.Competition)]
        [InlineData("https://example.org/competitions/example/Add", StoolballRouteType.CreateSeason)]
        [InlineData("https://example.org/competitions/example/ADD/", StoolballRouteType.CreateSeason)]
        [InlineData("https://example.org/competitions/example/EDIT", StoolballRouteType.CompetitionActions)]
        [InlineData("https://example.org/competitions/example/edit/", StoolballRouteType.CompetitionActions)]
        [InlineData("https://example.org/competitions/example/EDIT/competition", StoolballRouteType.EditCompetition)]
        [InlineData("https://example.org/competitions/example/edit/COMPETITION", StoolballRouteType.EditCompetition)]
        [InlineData("https://example.org/competitions/example/2020", StoolballRouteType.Season)]
        [InlineData("https://example.org/competitions/example/2020/", StoolballRouteType.Season)]
        [InlineData("https://example.org/competitions/example/2020/matches", StoolballRouteType.MatchesForSeason)]
        [InlineData("https://example.org/competitions/example/2020/MATCHES/", StoolballRouteType.MatchesForSeason)]
        [InlineData("https://example.org/competitions/example/2020-21/MATCHES", StoolballRouteType.MatchesForSeason)]
        [InlineData("https://example.org/competitions/example/2020-21/mAtChEs/", StoolballRouteType.MatchesForSeason)]
        [InlineData("https://example.org/competitions/example/2020/edit", StoolballRouteType.SeasonActions)]
        [InlineData("https://example.org/competitions/EXAMPLE/2020/EDIT/", StoolballRouteType.SeasonActions)]
        [InlineData("https://example.org/competitions/example/2020-21/edit", StoolballRouteType.SeasonActions)]
        [InlineData("https://example.org/competitions/EXAMPLE/2020-21/EDIT/", StoolballRouteType.SeasonActions)]
        [InlineData("https://example.org/competitions/example/2020/edit/season", StoolballRouteType.EditSeason)]
        [InlineData("https://example.org/competitions/EXAMPLE/2020/EDIT/SEASON/", StoolballRouteType.EditSeason)]
        [InlineData("https://example.org/competitions/example/2020-21/edit/season", StoolballRouteType.EditSeason)]
        [InlineData("https://example.org/competitions/EXAMPLE/2020-21/EDIT/Season/", StoolballRouteType.EditSeason)]
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
        [InlineData("https://example.org/clubs/example/invalid")]
        [InlineData("https://example.org/teams/example/invalid")]
        [InlineData("https://example.org/teams/example123/teams/example-team/")]
        [InlineData("https://example.org/tournaments/example123/invalid/example-team/")]
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
