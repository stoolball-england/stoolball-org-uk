using System;
using System.Linq;
using Moq;
using Stoolball.Awards;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Testing;
using Stoolball.Testing.CompetitionDataProviders;
using Stoolball.Testing.Factories;
using Stoolball.Testing.MatchDataProviders;
using Stoolball.Testing.PlayerDataProviders;
using Stoolball.Testing.SchoolDataProviders;
using Xunit;

namespace Stoolball.UnitTests.Testing
{
    public class SeedDataGeneratorTests
    {
        // Run each test enough times to be confident random data generation always matches the test
        private const int _iterations = 10;
        private readonly Randomiser _randomiser = new(new Random());
        private readonly Award _playerOfTheMatchAward = new Award { AwardId = Guid.NewGuid(), AwardName = "Player of the match" };
        private SeedDataGenerator CreateGenerator()
        {
            var competitionFactory = Mock.Of<CompetitionFactory>();
            var teamFactory = Mock.Of<TeamFactory>();
            var matchLocationFactory = Mock.Of<MatchLocationFactory>();
            var oversetFactory = Mock.Of<OverSetFactory>();
            var seasonFactory = new Mock<SeasonFactory>(oversetFactory).Object;
            var memberFactory = Mock.Of<UmbracoMemberFactory>();
            var commentFactory = Mock.Of<CommentFactory>();
            var overFactory = new OverFactory(Mock.Of<IOversHelper>());
            var matchFactory = new MatchFactory(_randomiser, _playerOfTheMatchAward, oversetFactory);
            return new SeedDataGenerator(_randomiser, overFactory, Mock.Of<IBowlingFiguresCalculator>(), Mock.Of<IPlayerIdentityFinder>(), Mock.Of<IMatchFinder>(),
                            competitionFactory, seasonFactory, teamFactory, Mock.Of<ClubFactory>(),
                            new Mock<TournamentFactory>(competitionFactory, seasonFactory, teamFactory, matchLocationFactory, oversetFactory, memberFactory, commentFactory).Object,
                            matchLocationFactory,
                            Mock.Of<SchoolFactory>(), new PlayerFactory(),
                            oversetFactory, memberFactory, commentFactory, _playerOfTheMatchAward,
                            matchFactory,
                            Enumerable.Empty<BaseMatchDataProvider>(), Enumerable.Empty<BaseCompetitionDataProvider>(),
                            Enumerable.Empty<BasePlayerDataProvider>(), Enumerable.Empty<BaseSchoolDataProvider>());
        }

        [Fact]
        public void Five_wicket_haul_exists()
        {
            var generator = CreateGenerator();

            for (var i = 0; i < _iterations; i++)
            {
                var teams = generator.GenerateTeams();
                var innings = generator.GenerateMatchData(new TestData(), teams).SelectMany(x => x.MatchInnings);

                var inningsWithFiveWicketHaulExists = innings.Any(x => // return true for this MatchInnings if...
                            x.PlayerInnings.Where(pi => StatisticsConstants.DISMISSALS_CREDITED_TO_BOWLER.Contains(pi.DismissalType) && pi.Bowler != null) // for all wickets credited to a bowler...
                            .GroupBy(pi => pi.Bowler!.Player!.PlayerId) // when grouped by bowler...
                            .Any(dismissals => dismissals.Count() >= 5)); // any bowler has 5 or more

                Assert.True(inningsWithFiveWicketHaulExists);
            }
        }
    }
}
