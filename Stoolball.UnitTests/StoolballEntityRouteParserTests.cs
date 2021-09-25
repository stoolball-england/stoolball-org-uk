using System;
using Xunit;

namespace Stoolball.UnitTests
{
    public class StoolballEntityRouteParserTests
    {
        [Fact]
        public void Null_route_throws_ArgumentNullException()
        {
            var parser = new StoolballEntityRouteParser();

            Assert.Throws<ArgumentNullException>(() => parser.ParseRoute(null));
        }

        [Theory]
        [InlineData("/invalid/example", null)]
        [InlineData("/players/example", StoolballEntityType.Player)]
        [InlineData("/PLAYERS/example/path", StoolballEntityType.Player)]
        [InlineData("/clubs/example", StoolballEntityType.Club)]
        [InlineData("/CLubS/example/Path", StoolballEntityType.Club)]
        [InlineData("/teams/example", StoolballEntityType.Team)]
        [InlineData("/TEAMS/EXAMPLE-TEAM/123", StoolballEntityType.Team)]
        [InlineData("/locations/example", StoolballEntityType.MatchLocation)]
        [InlineData("/Locations/Example/", StoolballEntityType.MatchLocation)]
        [InlineData("/competitions/example", StoolballEntityType.Competition)]
        [InlineData("/CoMpeTITIons/example/competition-123", StoolballEntityType.Competition)]
        [InlineData("/competitions/example/2021", StoolballEntityType.Season)]
        [InlineData("/competitions/EXAMPLE/2021/page", StoolballEntityType.Season)]
        [InlineData("/competitions/example/2021-2022", StoolballEntityType.Season)]
        [InlineData("/competitions/Example-Competition/2021-2022/some/page", StoolballEntityType.Season)]
        public void Route_is_parsed_coorectly(string route, StoolballEntityType? expected)
        {
            var parser = new StoolballEntityRouteParser();

            var result = parser.ParseRoute(route);

            Assert.Equal(expected, result);
        }
    }
}
