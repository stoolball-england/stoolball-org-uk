using System.Linq;
using Stoolball.Statistics;
using Stoolball.Testing.Factories;
using Stoolball.Testing.MatchDataProviders;
using Stoolball.Testing.TeamDataProviders;
using Xunit;

namespace Stoolball.Testing.UnitTests.MatchDataProviders
{
    public class RandomMatchDataProviderTests(TestServicesFixture _services) : IClassFixture<TestServicesFixture>
    {
        private RandomMatchDataProvider CreateProvider() =>
            new(_services.Get<Randomiser>(), _services.Get<MatchFactory>(), _services.Get<CommentFactory>(), _services.Get<IBowlingFiguresCalculator>());

        private TestData CreateTestDataWithTeams()
        {
            var testData = new TestData();
            var teamProvider = new RandomTeamsDataProvider(_services.Get<Randomiser>(), _services.Get<TeamFactory>(), _services.Get<PlayerFactory>());
            foreach (var (team, identities) in teamProvider.CreateTeams(testData))
            {
                testData.Teams.Add(team);
                testData.PlayerIdentities.AddRange(identities);
            }
            return testData;
        }

        [Fact]
        public void Throws_when_there_are_not_two_teams_with_players()
        {
            Assert.Throws<System.InvalidOperationException>(() => CreateProvider().CreateMatches(new TestData()));
        }

        [Fact]
        public void Runs_before_providers_with_the_default_order()
        {
            var defaultOrder = new MatchInThePastWithNoTeamsDataProvider(_services.Get<MatchFactory>()).Order;

            Assert.True(CreateProvider().Order < defaultOrder);
        }

        [Fact]
        public void Forty_matches_are_created_between_two_different_teams_in_the_test_data()
        {
            var testData = CreateTestDataWithTeams();

            var matches = CreateProvider().CreateMatches(testData).ToList();

            var teamIds = testData.Teams.Select(x => x.TeamId).ToList();
            Assert.Equal(40, matches.Count);
            Assert.All(matches, m =>
            {
                Assert.Equal(2, m.Teams.Select(x => x.Team!.TeamId).Distinct().Count());
                Assert.All(m.Teams, t => Assert.Contains(t.Team!.TeamId, teamIds));
            });
        }
    }
}
