using System.Linq;
using Stoolball.Testing.CompetitionDataProviders;
using Stoolball.Testing.Factories;
using Xunit;

namespace Stoolball.Testing.UnitTests.CompetitionDataProviders
{
    public class CompetitionWithOneSeasonWithPointsRulesDataProviderTests(TestServicesFixture _services) : IClassFixture<TestServicesFixture>
    {
        private CompetitionWithOneSeasonWithPointsRulesDataProvider CreateProvider() =>
            new(_services.Get<CompetitionFactory>(), _services.Get<SeasonFactory>(), _services.Get<TeamFactory>());

        [Fact]
        public void One_competition_is_created_with_one_season_in_2020()
        {
            var competition = Assert.Single(CreateProvider().CreateCompetitions(new TestData()));

            var season = Assert.Single(competition.Seasons);
            Assert.Equal(2020, season.FromYear);
            Assert.Equal(2020, season.UntilYear);
        }

        [Fact]
        public void Season_has_two_different_teams_and_points_rules()
        {
            var season = CreateProvider().CreateCompetitions(new TestData()).Single().Seasons.Single();

            Assert.Equal(2, season.Teams.Select(x => x.Team!.TeamId).Distinct().Count());
            Assert.NotEmpty(season.PointsRules);
        }
    }
}
