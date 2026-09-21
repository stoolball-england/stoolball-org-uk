using System;
using System.Linq;
using Stoolball.Testing.CompetitionDataProviders;
using Stoolball.Testing.Factories;
using Xunit;

namespace Stoolball.Testing.UnitTests.CompetitionDataProviders
{
    public class RandomCompetitionsDataProviderTests(TestServicesFixture _services) : IClassFixture<TestServicesFixture>
    {
        private RandomCompetitionsDataProvider CreateProvider() =>
            new(_services.Get<Randomiser>(), _services.Get<CompetitionFactory>(), _services.Get<SeasonFactory>());

        private TestData CreateTestDataWithTeams()
        {
            var testData = new TestData();
            testData.Teams.AddRange(_services.Get<TeamFactory>().CreateBasicTeamFaker().Generate(5));
            return testData;
        }

        [Fact]
        public void Ten_competitions_are_created_with_a_season_each()
        {
            var competitions = CreateProvider().CreateCompetitions(CreateTestDataWithTeams()).ToList();

            Assert.Equal(10, competitions.Count);
            Assert.All(competitions, c => Assert.NotEmpty(c.Seasons));
        }

        [Fact]
        public void Season_teams_come_from_the_test_data()
        {
            var testData = CreateTestDataWithTeams();
            var teamIds = testData.Teams.Select(x => x.TeamId).ToList();

            var seasonsWithTeams = CreateProvider().CreateCompetitions(testData).SelectMany(c => c.Seasons).Where(s => s.Teams.Any()).ToList();

            Assert.NotEmpty(seasonsWithTeams);
            Assert.All(seasonsWithTeams, s => Assert.All(s.Teams, t => Assert.Contains(t.Team!.TeamId, teamIds)));
        }

        [Fact]
        public void Throws_if_there_are_fewer_than_two_teams_in_the_test_data()
        {
            Assert.Throws<ArgumentException>(() => CreateProvider().CreateCompetitions(new TestData()).ToList());
        }
    }
}
