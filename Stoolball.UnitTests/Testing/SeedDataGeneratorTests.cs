using System;
using System.Linq;
using Moq;
using Stoolball.Awards;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Testing;
using Stoolball.Testing.Factories;
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
            var seasonFactory = Mock.Of<SeasonFactory>();
            var teamFactory = Mock.Of<TeamFactory>();
            var matchLocationFactory = Mock.Of<MatchLocationFactory>();
            var oversetFactory = Mock.Of<OverSetFactory>();
            var memberFactory = Mock.Of<UmbracoMemberFactory>();
            var commentFactory = Mock.Of<CommentFactory>();
            return new SeedDataGenerator(_randomiser, Mock.Of<IOversHelper>(), Mock.Of<IBowlingFiguresCalculator>(), Mock.Of<IPlayerIdentityFinder>(), Mock.Of<IMatchFinder>(),
                            competitionFactory, seasonFactory, teamFactory, Mock.Of<ClubFactory>(),
                            new Mock<TournamentFactory>(competitionFactory, seasonFactory, teamFactory, matchLocationFactory, oversetFactory, memberFactory, commentFactory).Object,
                            matchLocationFactory,
                            Mock.Of<SchoolFactory>(), new PlayerFactory(),
                            oversetFactory, memberFactory, commentFactory, _playerOfTheMatchAward);
        }

        [Fact]
        public void Over_exists_with_only_a_bowler_name()
        {
            var generator = CreateGenerator();

            for (var i = 0; i < _iterations; i++)
            {
                var overs = generator.CreateOversBowled(generator.GenerateTeams()[0].identities, []);

                Assert.Contains(overs, x => x.Bowler != null && x.BallsBowled == null && x.NoBalls == null && x.Wides == null && x.RunsConceded == null);
            }
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
