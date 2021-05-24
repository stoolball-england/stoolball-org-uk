using System.Linq;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Testing;

namespace Stoolball.Data.SqlServer.IntegrationTests.Fixtures
{
    public sealed class SqlServerStatisticsMaxResultsDataSourceFixture : BaseSqlServerFixture
    {
        public TestData TestData { get; set; } = new TestData();
        public Player PlayerWithFifthAndSixthInningsTheSame { get; private set; }
        public Player PlayerWithFifthAndSixthBowlingFiguresTheSame { get; private set; }

        public SqlServerStatisticsMaxResultsDataSourceFixture() : base("StoolballStatisticsMaxResultsDataSourceIntegrationTests")
        {
            // Populate seed data so that there's a consistent baseline for each test run
            var oversHelper = new OversHelper();
            var bowlingFiguresCalculator = new BowlingFiguresCalculator(oversHelper);
            var playerIdentityFinder = new PlayerIdentityFinder();
            var seedDataGenerator = new SeedDataGenerator(oversHelper, bowlingFiguresCalculator, playerIdentityFinder);
            TestData = seedDataGenerator.GenerateTestData();
            ModifyTestData();

            using (var connection = ConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();

                var repo = new SqlServerIntegrationTestsRepository(connection);
                repo.CreateTestData(TestData);
            }
        }

        /// <remarks>
        /// This method potentially substitutes a bowler from another match as a wicket-taker - this will disrupt other statistics records,
        /// which is why this must be a separate fixture for the tests of this feature.
        /// </remarks>
        private void ModifyTestData()
        {
            // Find any player with at least six innings, and make the sixth best score the same as the fifth so that we can test retrieving a top five + any equal results
            var inningsForPlayerWithAtLeast6Scores = TestData.Matches
                             .SelectMany(x => x.MatchInnings) // for each innings of each match...
                             .SelectMany(x => x.PlayerInnings.Where(i => i.DismissalType != DismissalType.DidNotBat && i.DismissalType != DismissalType.TimedOut && i.RunsScored.HasValue)) // get the player innings where the player got to bat...
                             .GroupBy(x => x.Batter.Player.PlayerId) // separate them into a group of innings for each player...
                             .First(x => x.Count() > 5) // get the first player that had more than 5 such innings...
                             .Select(x => x) // get those innings out of the IGrouping structure...
                             .OrderByDescending(x => x.RunsScored) // sort the best scores first, because that's what the query we're testing will do...
                             .ToList(); // make it possible to access innings by index

            // Make the sixth innings the same as the fifth, including anything that might affect the out/not out status.
            inningsForPlayerWithAtLeast6Scores[5].DismissalType = inningsForPlayerWithAtLeast6Scores[4].DismissalType;
            inningsForPlayerWithAtLeast6Scores[5].DismissedBy = inningsForPlayerWithAtLeast6Scores[4].DismissedBy; // This can end up being a player who's not in the teams in the match!
            inningsForPlayerWithAtLeast6Scores[5].Bowler = inningsForPlayerWithAtLeast6Scores[4].Bowler; // This can end up being a player who's not in the teams in the match!
            inningsForPlayerWithAtLeast6Scores[5].RunsScored = inningsForPlayerWithAtLeast6Scores[4].RunsScored;

            // The assertion expects the fifth and sixth innings to be the same, but to be different that any that come before or after in the
            // result set. So make sure those others are different.

            // Step 1: Make room below if required
            if (inningsForPlayerWithAtLeast6Scores.Count > 6 && inningsForPlayerWithAtLeast6Scores[5].RunsScored == 0)
            {
                inningsForPlayerWithAtLeast6Scores[4].RunsScored++;
                inningsForPlayerWithAtLeast6Scores[5].RunsScored++;
            }

            // Step 2: Ensure earlier scores are higher
            for (var i = 0; i < 4; i++)
            {
                if (inningsForPlayerWithAtLeast6Scores[i].RunsScored == inningsForPlayerWithAtLeast6Scores[4].RunsScored)
                {
                    inningsForPlayerWithAtLeast6Scores[i].RunsScored++;
                }
            }

            // Step 3: Ensure later scores are lower, but not below 0
            for (var i = 6; i < inningsForPlayerWithAtLeast6Scores.Count; i++)
            {
                if (inningsForPlayerWithAtLeast6Scores[i].RunsScored > 0)
                {
                    inningsForPlayerWithAtLeast6Scores[i].RunsScored--;
                }
                else
                {
                    inningsForPlayerWithAtLeast6Scores[i].RunsScored = 0;
                }
            }

            PlayerWithFifthAndSixthInningsTheSame = inningsForPlayerWithAtLeast6Scores.First().Batter.Player;

            // Find any player with at least six sets of bowling figures, and make the sixth set the same as the fifth so that we can test retrieving a top five + any equal results
            var inningsForPlayerWithAtLeast6BowlingFigures = TestData.Matches
                             .SelectMany(x => x.MatchInnings) // for each innings of each match...
                             .SelectMany(x => x.BowlingFigures) // get the bowling figures...
                             .GroupBy(x => x.Bowler.Player.PlayerId) // separate them into a group of figures for each player...
                             .First(x => x.Count() > 5) // get the first player that had more than 5 sets of bowling figures...
                             .Select(x => x) // get those figures out of the IGrouping structure...
                             .OrderByDescending(x => x.Wickets).ThenByDescending(x => x.RunsConceded.HasValue).ThenBy(x => x.RunsConceded) // sort the best scores first, because that's what the query we're testing will do...
                             .ToList(); // make it possible to access figures by index

            // Make the sixth set of figures the same as the fifth.
            // If there are more, make sure they're worse so we know what to assert.
            inningsForPlayerWithAtLeast6BowlingFigures[5].Overs = inningsForPlayerWithAtLeast6BowlingFigures[4].Overs;
            inningsForPlayerWithAtLeast6BowlingFigures[5].Maidens = inningsForPlayerWithAtLeast6BowlingFigures[4].Maidens;
            inningsForPlayerWithAtLeast6BowlingFigures[5].RunsConceded = inningsForPlayerWithAtLeast6BowlingFigures[4].RunsConceded;
            inningsForPlayerWithAtLeast6BowlingFigures[5].Wickets = inningsForPlayerWithAtLeast6BowlingFigures[4].Wickets;
            for (var i = 6; i < inningsForPlayerWithAtLeast6BowlingFigures.Count; i++)
            {
                inningsForPlayerWithAtLeast6BowlingFigures[i].RunsConceded++;
            }

            PlayerWithFifthAndSixthBowlingFiguresTheSame = inningsForPlayerWithAtLeast6BowlingFigures.First().Bowler.Player;
        }
    }
}
