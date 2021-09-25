using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Testing;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Statistics
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class ReadPlayerBowlingSummaryTests
    {
        private readonly SqlServerTestDataFixture _databaseFixture;
        private readonly DateRangeGenerator _dateRangeGenerator = new DateRangeGenerator();

        public ReadPlayerBowlingSummaryTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_bowling_statistics_throws_ArgumentNullException_with_no_filter()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await dataSource.ReadBowlingStatistics(null).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_statistics_throws_ArgumentException_with_no_player()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            await Assert.ThrowsAsync<ArgumentException>(async () => await dataSource.ReadBowlingStatistics(new StatisticsFilter()).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_statistics_throws_ArgumentException_with_no_player_id()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            await Assert.ThrowsAsync<ArgumentException>(async () => await dataSource.ReadBowlingStatistics(new StatisticsFilter { Player = new Player() }).ConfigureAwait(false)).ConfigureAwait(false);
        }

        private async Task TestTotalInnings(StatisticsFilter filter, string whereClause, Dictionary<string, object> parameters, IEnumerable<Stoolball.Matches.Match> matches)
        {

            foreach (var player in _databaseFixture.TestData.Players)
            {
                filter.Player = player;
                parameters.Remove("PlayerId");
                parameters.Add("PlayerId", player.PlayerId);
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerId = @PlayerId" + whereClause, parameters));
                var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

                var result = await dataSource.ReadBowlingStatistics(filter).ConfigureAwait(false);

                Assert.NotNull(result);
                Assert.Equal(matches.SelectMany(x => x.MatchInnings).Count(x =>
                        x.OversBowled.Any(o => o.Bowler.Player.PlayerId == player.PlayerId) ||
                        x.PlayerInnings.Any(pi => pi.Bowler?.Player.PlayerId == player.PlayerId)
                    ), result.TotalInnings);
            }
        }

        private async Task TestTotalInningsWithRunsScored(StatisticsFilter filter, string whereClause, Dictionary<string, object> parameters, IEnumerable<Stoolball.Matches.Match> matches)
        {

            foreach (var player in _databaseFixture.TestData.Players)
            {
                filter.Player = player;
                parameters.Remove("PlayerId");
                parameters.Add("PlayerId", player.PlayerId);
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerId = @PlayerId" + whereClause, parameters));
                var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

                var result = await dataSource.ReadBowlingStatistics(filter).ConfigureAwait(false);

                Assert.NotNull(result);
                Assert.Equal(matches.SelectMany(x => x.MatchInnings).Count(x =>
                        x.OversBowled.Any(o => o.Bowler.Player.PlayerId == player.PlayerId && o.RunsConceded.HasValue)),
                        result.TotalInningsWithRunsConceded);
            }
        }

        private async Task TestTotalInningsWithBallsBowled(StatisticsFilter filter, string whereClause, Dictionary<string, object> parameters, IEnumerable<Stoolball.Matches.Match> matches)
        {

            foreach (var player in _databaseFixture.TestData.Players)
            {
                filter.Player = player;
                parameters.Remove("PlayerId");
                parameters.Add("PlayerId", player.PlayerId);
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerId = @PlayerId" + whereClause, parameters));
                var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

                var result = await dataSource.ReadBowlingStatistics(filter).ConfigureAwait(false);

                Assert.NotNull(result);
                Assert.Equal(matches.SelectMany(x => x.MatchInnings).Count(x =>
                        x.BowlingFigures.Any(o => o.Bowler.Player.PlayerId == player.PlayerId && o.Overs.HasValue)),
                        result.TotalInningsWithBallsBowled);
            }
        }

        private async Task TestTotalOvers(StatisticsFilter filter, string whereClause, Dictionary<string, object> parameters, IEnumerable<Stoolball.Matches.Match> matches)
        {
            var oversHelper = new OversHelper();

            foreach (var player in _databaseFixture.TestData.Players)
            {
                filter.Player = player;
                parameters.Remove("PlayerId");
                parameters.Add("PlayerId", player.PlayerId);
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerId = @PlayerId" + whereClause, parameters));
                var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

                var result = await dataSource.ReadBowlingStatistics(filter).ConfigureAwait(false);

                var ballsBowled = matches.SelectMany(x => x.MatchInnings)
                        .SelectMany(x => x.BowlingFigures.Where(o => o.Bowler.Player.PlayerId == player.PlayerId && o.Overs.HasValue))
                        .Sum(o => oversHelper.OversToBallsBowled(o.Overs.Value));

                Assert.NotNull(result);
                Assert.Equal(oversHelper.BallsBowledToOvers(ballsBowled), result.TotalOvers);
            }
        }

        private async Task TestTotalMaidens(StatisticsFilter filter, string whereClause, Dictionary<string, object> parameters, IEnumerable<Stoolball.Matches.Match> matches)
        {

            foreach (var player in _databaseFixture.TestData.Players)
            {
                filter.Player = player;
                parameters.Remove("PlayerId");
                parameters.Add("PlayerId", player.PlayerId);
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerId = @PlayerId" + whereClause, parameters));
                var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

                var result = await dataSource.ReadBowlingStatistics(filter).ConfigureAwait(false);

                var maidens = matches.SelectMany(x => x.MatchInnings)
                        .SelectMany(x => x.BowlingFigures.Where(o => o.Bowler.Player.PlayerId == player.PlayerId && o.Maidens.HasValue))
                        .Sum(x => x.Maidens);

                Assert.NotNull(result);
                Assert.Equal(maidens, result.TotalMaidens);
            }
        }

        private async Task TestTotalRunsConceded(StatisticsFilter filter, string whereClause, Dictionary<string, object> parameters, IEnumerable<Stoolball.Matches.Match> matches)
        {

            foreach (var player in _databaseFixture.TestData.Players)
            {
                filter.Player = player;
                parameters.Remove("PlayerId");
                parameters.Add("PlayerId", player.PlayerId);
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerId = @PlayerId" + whereClause, parameters));
                var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

                var result = await dataSource.ReadBowlingStatistics(filter).ConfigureAwait(false);

                var runsConceded = matches.SelectMany(x => x.MatchInnings)
                       .SelectMany(x => x.BowlingFigures.Where(o => o.Bowler.Player.PlayerId == player.PlayerId && o.RunsConceded.HasValue))
                       .Sum(x => x.RunsConceded);

                Assert.NotNull(result);
                Assert.Equal(runsConceded, result.TotalRunsConceded);
            }
        }

        private async Task TestTotalWickets(StatisticsFilter filter, string whereClause, Dictionary<string, object> parameters, IEnumerable<Stoolball.Matches.Match> matches)
        {

            foreach (var player in _databaseFixture.TestData.Players)
            {
                filter.Player = player;
                parameters.Remove("PlayerId");
                parameters.Add("PlayerId", player.PlayerId);
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerId = @PlayerId" + whereClause, parameters));
                var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

                var result = await dataSource.ReadBowlingStatistics(filter).ConfigureAwait(false);

                var wickets = matches.SelectMany(x => x.MatchInnings)
                       .SelectMany(x => x.BowlingFigures.Where(o => o.Bowler.Player.PlayerId == player.PlayerId))
                       .Sum(x => x.Wickets);

                Assert.NotNull(result);
                Assert.Equal(wickets, result.TotalWickets);
            }
        }

        private async Task TestFiveWicketInnings(StatisticsFilter filter, string whereClause, Dictionary<string, object> parameters, IEnumerable<Stoolball.Matches.Match> matches)
        {
            foreach (var player in _databaseFixture.TestData.Players)
            {
                filter.Player = player;
                parameters.Remove("PlayerId");
                parameters.Add("PlayerId", player.PlayerId);
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerId = @PlayerId" + whereClause, parameters));
                var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

                var result = await dataSource.ReadBowlingStatistics(filter).ConfigureAwait(false);

                var wicketHauls = matches.SelectMany(x => x.MatchInnings)
                        .SelectMany(x => x.BowlingFigures.Where(o => o.Bowler.Player.PlayerId == player.PlayerId))
                        .Count(x => x.Wickets >= 5);

                Assert.NotNull(result);
                Assert.Equal(wicketHauls, result.FiveWicketInnings);
            }
        }

        private async Task TestBestBowling(StatisticsFilter filter, string whereClause, Dictionary<string, object> parameters, IEnumerable<Stoolball.Matches.Match> matches)
        {

            foreach (var player in _databaseFixture.TestData.Players)
            {
                filter.Player = player;
                parameters.Remove("PlayerId");
                parameters.Add("PlayerId", player.PlayerId);
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerId = @PlayerId" + whereClause, parameters));
                var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

                var result = await dataSource.ReadBowlingStatistics(filter).ConfigureAwait(false);

                var best = matches.SelectMany(x => x.MatchInnings) // get all innings...
                .SelectMany(x => x.BowlingFigures.Where(o => o.Bowler.Player.PlayerId == player.PlayerId)) // ...where this player bowled, and select their bowling figures
                .GroupBy(x => x.MatchInnings.MatchInningsId, x => x, (matchInningsId, bowlingFigures) => new BowlingFigures // combine any multiple identities for the player into new bowling figures
                {
                    Wickets = bowlingFigures.Sum(bf => bf.Wickets),
                    RunsConceded = bowlingFigures.Any(bf => bf.RunsConceded.HasValue) ? bowlingFigures.Sum(bf => bf.RunsConceded) : null
                })
                .OrderByDescending(x => x.Wickets).ThenByDescending(x => x.RunsConceded.HasValue ? 1 : 0).ThenBy(x => x.RunsConceded) // then sort them by best bowling first
                .FirstOrDefault(); // and select the top one, if there is one

                Assert.NotNull(result);
                if (best != null)
                {
                    Assert.NotNull(result.BestInningsWickets);
                    Assert.Equal(best.Wickets, result.BestInningsWickets.Value);

                    if (best.RunsConceded.HasValue)
                    {
                        Assert.Equal(best.RunsConceded, result.BestInningsRunsConceded.Value);
                    }
                    else
                    {
                        Assert.Null(result.BestInningsRunsConceded);
                    }
                }
                else
                {
                    Assert.Null(result.BestInningsWickets);
                    Assert.Null(result.BestInningsRunsConceded);
                }
            }
        }

        private async Task TestEconomy(StatisticsFilter filter, string whereClause, Dictionary<string, object> parameters, IEnumerable<Stoolball.Matches.Match> matches)
        {
            var oversHelper = new OversHelper();

            foreach (var player in _databaseFixture.TestData.Players)
            {
                filter.Player = player;
                parameters.Remove("PlayerId");
                parameters.Add("PlayerId", player.PlayerId);
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerId = @PlayerId" + whereClause, parameters));
                var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

                var result = await dataSource.ReadBowlingStatistics(filter).ConfigureAwait(false);

                var dataForEconomy = matches.SelectMany(x => x.MatchInnings)
                     .SelectMany(x => x.BowlingFigures.Where(o => o.Bowler.Player.PlayerId == player.PlayerId && o.RunsConceded.HasValue));
                var expectedEconomy = dataForEconomy.Sum(x => x.Overs) > 0 ? (decimal)dataForEconomy.Sum(x => x.RunsConceded.Value) / dataForEconomy.Sum(x => (decimal)oversHelper.OversToBallsBowled(x.Overs.Value) / StatisticsConstants.BALLS_PER_OVER) : (decimal?)null;

                Assert.NotNull(result);
                if (expectedEconomy.HasValue)
                {
                    Assert.NotNull(result.Economy);
                    Assert.Equal(expectedEconomy.Value.AccurateToTwoDecimalPlaces(), result.Economy.Value.AccurateToTwoDecimalPlaces());
                }
                else
                {
                    Assert.Null(result.Economy);
                }
            }
        }

        private async Task TestStrikeRate(StatisticsFilter filter, string whereClause, Dictionary<string, object> parameters, IEnumerable<Stoolball.Matches.Match> matches)
        {
            var oversHelper = new OversHelper();

            foreach (var player in _databaseFixture.TestData.Players)
            {
                filter.Player = player;
                parameters.Remove("PlayerId");
                parameters.Add("PlayerId", player.PlayerId);
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerId = @PlayerId" + whereClause, parameters));
                var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

                var result = await dataSource.ReadBowlingStatistics(filter).ConfigureAwait(false);

                var dataForStrikeRate = matches.SelectMany(x => x.MatchInnings)
                        .SelectMany(x => x.BowlingFigures.Where(o => o.Bowler.Player.PlayerId == player.PlayerId && o.Overs.HasValue));
                var expectedStrikeRate = dataForStrikeRate.Sum(x => x.Wickets) > 0 ? (decimal)dataForStrikeRate.Sum(x => oversHelper.OversToBallsBowled(x.Overs.Value)) / dataForStrikeRate.Sum(x => x.Wickets) : (decimal?)null;

                Assert.NotNull(result);
                if (expectedStrikeRate.HasValue)
                {
                    Assert.NotNull(result.StrikeRate);
                    Assert.Equal(expectedStrikeRate.Value.AccurateToTwoDecimalPlaces(), result.StrikeRate.Value.AccurateToTwoDecimalPlaces());
                }
                else
                {
                    Assert.Null(result.StrikeRate);
                }
            }
        }

        private async Task TestAverage(StatisticsFilter filter, string whereClause, Dictionary<string, object> parameters, IEnumerable<Stoolball.Matches.Match> matches)
        {

            foreach (var player in _databaseFixture.TestData.Players)
            {
                filter.Player = player;
                parameters.Remove("PlayerId");
                parameters.Add("PlayerId", player.PlayerId);
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerId = @PlayerId" + whereClause, parameters));
                var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

                var result = await dataSource.ReadBowlingStatistics(filter).ConfigureAwait(false);

                var dataForAverage = matches.SelectMany(x => x.MatchInnings)
                        .SelectMany(x => x.BowlingFigures.Where(o => o.Bowler.Player.PlayerId == player.PlayerId && o.RunsConceded.HasValue));
                var expectedAverage = dataForAverage.Sum(x => x.Wickets) > 0 ? (decimal)dataForAverage.Sum(x => x.RunsConceded) / dataForAverage.Sum(x => x.Wickets) : (decimal?)null;

                Assert.NotNull(result);
                if (expectedAverage.HasValue)
                {
                    Assert.NotNull(result.Average);
                    Assert.Equal(expectedAverage.Value.AccurateToTwoDecimalPlaces(), result.Average.Value.AccurateToTwoDecimalPlaces());
                }
                else
                {
                    Assert.Null(result.Average);
                }
            }
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_TotalInnings_supporting_unfiltered_matches()
        {
            await TestTotalInnings(new StatisticsFilter(),
                string.Empty,
                new Dictionary<string, object>(),
                _databaseFixture.TestData.Matches).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_TotalInningsWithRunsConceded_supporting_unfiltered_matches()
        {
            await TestTotalInningsWithRunsScored(new StatisticsFilter(),
                string.Empty,
                new Dictionary<string, object>(),
                _databaseFixture.TestData.Matches).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_TotalInningsWithBallsBowled_supporting_unfiltered_matches()
        {
            await TestTotalInningsWithBallsBowled(new StatisticsFilter(),
                string.Empty,
                new Dictionary<string, object>(),
                _databaseFixture.TestData.Matches).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_TotalOvers_supporting_unfiltered_matches()
        {
            await TestTotalOvers(new StatisticsFilter(),
                string.Empty,
                new Dictionary<string, object>(),
                _databaseFixture.TestData.Matches).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_TotalMaidens_supporting_unfiltered_matches()
        {
            await TestTotalMaidens(new StatisticsFilter(),
                string.Empty,
                new Dictionary<string, object>(),
                _databaseFixture.TestData.Matches).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_TotalRunsConceded_supporting_unfiltered_matches()
        {
            await TestTotalRunsConceded(new StatisticsFilter(),
                string.Empty,
                new Dictionary<string, object>(),
                _databaseFixture.TestData.Matches).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_TotalWickets_supporting_unfiltered_matches()
        {
            await TestTotalWickets(new StatisticsFilter(),
                string.Empty,
                new Dictionary<string, object>(),
                _databaseFixture.TestData.Matches).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_FiveWicketInnings_supporting_unfiltered_matches()
        {
            await TestFiveWicketInnings(new StatisticsFilter(),
                string.Empty,
                new Dictionary<string, object>(),
                _databaseFixture.TestData.Matches).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_BestBowling_supporting_unfiltered_matches()
        {
            await TestBestBowling(new StatisticsFilter(),
                string.Empty,
                new Dictionary<string, object>(),
                _databaseFixture.TestData.Matches).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_Economy_supporting_unfiltered_matches()
        {
            await TestEconomy(new StatisticsFilter(),
                string.Empty,
                new Dictionary<string, object>(),
                _databaseFixture.TestData.Matches).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_StrikeRate_supporting_unfiltered_matches()
        {
            await TestStrikeRate(new StatisticsFilter(),
                string.Empty,
                new Dictionary<string, object>(),
                _databaseFixture.TestData.Matches).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_Average_supporting_unfiltered_matches()
        {
            await TestAverage(new StatisticsFilter(),
                string.Empty,
                new Dictionary<string, object>(),
                _databaseFixture.TestData.Matches).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_TotalInnings_supporting_filter_by_date()
        {
            var (fromDate, untilDate) = _dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);
            await TestTotalInnings(new StatisticsFilter { FromDate = fromDate, UntilDate = untilDate },
               " AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate",
               new Dictionary<string, object> { { "FromDate", fromDate }, { "UntilDate", untilDate } },
               _databaseFixture.TestData.Matches.Where(x => x.StartTime >= fromDate && x.StartTime <= untilDate)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_TotalInningsWithRunsConceded_supporting_filter_by_date()
        {
            var (fromDate, untilDate) = _dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);
            await TestTotalInningsWithRunsScored(new StatisticsFilter { FromDate = fromDate, UntilDate = untilDate },
               " AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate",
               new Dictionary<string, object> { { "FromDate", fromDate }, { "UntilDate", untilDate } },
               _databaseFixture.TestData.Matches.Where(x => x.StartTime >= fromDate && x.StartTime <= untilDate)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_TotalInningsWithBallsBowled_supporting_filter_by_date()
        {
            var (fromDate, untilDate) = _dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);
            await TestTotalInningsWithBallsBowled(new StatisticsFilter { FromDate = fromDate, UntilDate = untilDate },
               " AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate",
               new Dictionary<string, object> { { "FromDate", fromDate }, { "UntilDate", untilDate } },
               _databaseFixture.TestData.Matches.Where(x => x.StartTime >= fromDate && x.StartTime <= untilDate)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_TotalOvers_supporting_filter_by_date()
        {
            var (fromDate, untilDate) = _dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);
            await TestTotalOvers(new StatisticsFilter { FromDate = fromDate, UntilDate = untilDate },
               " AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate",
               new Dictionary<string, object> { { "FromDate", fromDate }, { "UntilDate", untilDate } },
               _databaseFixture.TestData.Matches.Where(x => x.StartTime >= fromDate && x.StartTime <= untilDate)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_TotalMaidens_supporting_filter_by_date()
        {
            var (fromDate, untilDate) = _dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);
            await TestTotalMaidens(new StatisticsFilter { FromDate = fromDate, UntilDate = untilDate },
               " AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate",
               new Dictionary<string, object> { { "FromDate", fromDate }, { "UntilDate", untilDate } },
               _databaseFixture.TestData.Matches.Where(x => x.StartTime >= fromDate && x.StartTime <= untilDate)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_TotalRunsConceded_supporting_filter_by_date()
        {
            var (fromDate, untilDate) = _dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);
            await TestTotalRunsConceded(new StatisticsFilter { FromDate = fromDate, UntilDate = untilDate },
               " AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate",
               new Dictionary<string, object> { { "FromDate", fromDate }, { "UntilDate", untilDate } },
               _databaseFixture.TestData.Matches.Where(x => x.StartTime >= fromDate && x.StartTime <= untilDate)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_TotalWickets_supporting_filter_by_date()
        {
            var (fromDate, untilDate) = _dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);
            await TestTotalWickets(new StatisticsFilter { FromDate = fromDate, UntilDate = untilDate },
               " AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate",
               new Dictionary<string, object> { { "FromDate", fromDate }, { "UntilDate", untilDate } },
               _databaseFixture.TestData.Matches.Where(x => x.StartTime >= fromDate && x.StartTime <= untilDate)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_FiveWicketInnings_supporting_filter_by_date()
        {
            var (fromDate, untilDate) = _dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);
            await TestFiveWicketInnings(new StatisticsFilter { FromDate = fromDate, UntilDate = untilDate },
               " AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate",
               new Dictionary<string, object> { { "FromDate", fromDate }, { "UntilDate", untilDate } },
               _databaseFixture.TestData.Matches.Where(x => x.StartTime >= fromDate && x.StartTime <= untilDate)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_BestBowling_supporting_filter_by_date()
        {
            var (fromDate, untilDate) = _dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);
            await TestBestBowling(new StatisticsFilter { FromDate = fromDate, UntilDate = untilDate },
               " AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate",
               new Dictionary<string, object> { { "FromDate", fromDate }, { "UntilDate", untilDate } },
               _databaseFixture.TestData.Matches.Where(x => x.StartTime >= fromDate && x.StartTime <= untilDate)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_Economy_supporting_filter_by_date()
        {
            var (fromDate, untilDate) = _dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);
            await TestEconomy(new StatisticsFilter { FromDate = fromDate, UntilDate = untilDate },
               " AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate",
               new Dictionary<string, object> { { "FromDate", fromDate }, { "UntilDate", untilDate } },
               _databaseFixture.TestData.Matches.Where(x => x.StartTime >= fromDate && x.StartTime <= untilDate)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_StrikeRate_supporting_filter_by_date()
        {
            var (fromDate, untilDate) = _dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);
            await TestStrikeRate(new StatisticsFilter { FromDate = fromDate, UntilDate = untilDate },
               " AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate",
               new Dictionary<string, object> { { "FromDate", fromDate }, { "UntilDate", untilDate } },
               _databaseFixture.TestData.Matches.Where(x => x.StartTime >= fromDate && x.StartTime <= untilDate)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_Average_supporting_filter_by_date()
        {
            var (fromDate, untilDate) = _dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);
            await TestAverage(new StatisticsFilter { FromDate = fromDate, UntilDate = untilDate },
               " AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate",
               new Dictionary<string, object> { { "FromDate", fromDate }, { "UntilDate", untilDate } },
               _databaseFixture.TestData.Matches.Where(x => x.StartTime >= fromDate && x.StartTime <= untilDate)).ConfigureAwait(false);
        }
    }
}
