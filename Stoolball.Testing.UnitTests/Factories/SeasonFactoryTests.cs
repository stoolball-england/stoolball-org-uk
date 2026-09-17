using System.Linq;
using Stoolball.Testing.Factories;
using Xunit;

namespace Stoolball.Testing.UnitTests.Factories
{
    public class SeasonFactoryTests(TestServicesFixture _services) : IClassFixture<TestServicesFixture>
    {
        [Fact]
        public void Season_with_full_details_has_two_teams_points_rules_and_a_points_adjustment()
        {
            var competition = _services.Get<CompetitionFactory>().CreateFaker().Generate();
            var team1 = _services.Get<TeamFactory>().CreateBasicTeamFaker().Generate();
            var team2 = _services.Get<TeamFactory>().CreateBasicTeamFaker().Generate();

            var season = _services.Get<SeasonFactory>().CreateSeasonWithFullDetails(competition, 2020, 2020, team1, team2);

            Assert.Equal(2, season.Teams.Count);
            Assert.Contains(season.Teams, x => x.Team == team1);
            Assert.Contains(season.Teams, x => x.Team == team2);
            Assert.All(season.Teams, x => Assert.Same(season, x.Season));
            Assert.NotEmpty(season.PointsRules);
            Assert.Single(season.PointsAdjustments);
            Assert.Contains(team1.Seasons, x => x.Season == season);
            Assert.Contains(team2.Seasons, x => x.Season == season);
        }
    }
}
