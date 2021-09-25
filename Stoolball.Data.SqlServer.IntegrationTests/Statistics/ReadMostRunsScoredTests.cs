using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Statistics;
using Stoolball.Testing;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Statistics
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class ReadMostRunsScoredTests
    {
        private readonly SqlServerTestDataFixture _databaseFixture;

        public ReadMostRunsScoredTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_total_players_with_runs_scored_supports_no_filter()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, Mock.Of<IPlayerDataSource>());

            var result = await dataSource.ReadTotalPlayersWithRunsScored(null).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches
                .SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.PlayerInnings)
                .Where(x => x.RunsScored.HasValue && x.RunsScored > 0)
                .Select(x => x.Batter.Player.PlayerId)
                .Distinct()
                .Count();
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_players_with_runs_scored_supports_filter_by_club_id()
        {
            var filter = new StatisticsFilter { Club = _databaseFixture.TestData.TeamWithFullDetails.Club };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND ClubId = @ClubId", new Dictionary<string, object> { { "ClubId", _databaseFixture.TestData.TeamWithFullDetails.Club.ClubId } }));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, Mock.Of<IPlayerDataSource>());

            var result = await dataSource.ReadTotalPlayersWithRunsScored(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches
              .Where(x => x.Teams.Select(t => t.Team.TeamId).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamId.Value))
              .SelectMany(x => x.MatchInnings)
              .Where(i => i.BattingTeam.Team.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId.Value)
              .SelectMany(x => x.PlayerInnings)
              .Where(x => x.RunsScored.HasValue && x.RunsScored > 0)
              .Select(x => x.Batter.Player.PlayerId)
              .Distinct()
              .Count();
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_players_with_runs_scored_supports_filter_by_team_id()
        {
            var filter = new StatisticsFilter { Team = _databaseFixture.TestData.TeamWithFullDetails };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND TeamId = @TeamId", new Dictionary<string, object> { { "TeamId", _databaseFixture.TestData.TeamWithFullDetails.TeamId } }));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, Mock.Of<IPlayerDataSource>());

            var result = await dataSource.ReadTotalPlayersWithRunsScored(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches
              .Where(x => x.Teams.Select(t => t.Team.TeamId).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamId.Value))
              .SelectMany(x => x.MatchInnings)
              .Where(i => i.BattingTeam.Team.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId.Value)
              .SelectMany(x => x.PlayerInnings)
              .Where(x => x.RunsScored.HasValue && x.RunsScored > 0)
              .Select(x => x.Batter.Player.PlayerId)
              .Distinct()
              .Count();
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_players_with_runs_scored_supports_filter_by_match_location_id()
        {
            var filter = new StatisticsFilter { MatchLocation = _databaseFixture.TestData.MatchLocations.First() };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND MatchLocationId = @MatchLocationId", new Dictionary<string, object> { { "MatchLocationId", _databaseFixture.TestData.MatchLocations.First().MatchLocationId } }));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, Mock.Of<IPlayerDataSource>());

            var result = await dataSource.ReadTotalPlayersWithRunsScored(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches
                .Where(x => x.MatchLocation?.MatchLocationId == _databaseFixture.TestData.MatchLocations.First().MatchLocationId)
                .SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.PlayerInnings)
                .Where(x => x.RunsScored.HasValue && x.RunsScored > 0)
                .Select(x => x.Batter.Player.PlayerId)
                .Distinct()
                .Count();
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_players_with_runs_scored_supports_filter_by_competition_id()
        {
            var filter = new StatisticsFilter { Competition = _databaseFixture.TestData.Competitions.First() };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND CompetitionId = @CompetitionId", new Dictionary<string, object> { { "CompetitionId", _databaseFixture.TestData.Competitions.First().CompetitionId } }));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, Mock.Of<IPlayerDataSource>());

            var result = await dataSource.ReadTotalPlayersWithRunsScored(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches
              .Where(x => x.Season?.Competition?.CompetitionId == _databaseFixture.TestData.Competitions.First().CompetitionId)
              .SelectMany(x => x.MatchInnings)
              .SelectMany(x => x.PlayerInnings)
              .Where(x => x.RunsScored.HasValue && x.RunsScored > 0)
              .Select(x => x.Batter.Player.PlayerId)
              .Distinct()
              .Count();
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_players_with_runs_scored_supports_filter_by_season_id()
        {
            var filter = new StatisticsFilter { Season = _databaseFixture.TestData.Competitions.First().Seasons.First() };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND SeasonId = @SeasonId", new Dictionary<string, object> { { "SeasonId", _databaseFixture.TestData.Competitions.First().Seasons.First().SeasonId } }));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, Mock.Of<IPlayerDataSource>());

            var result = await dataSource.ReadTotalPlayersWithRunsScored(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches
              .Where(x => x.Season?.SeasonId == _databaseFixture.TestData.Competitions.First().Seasons.First().SeasonId)
              .SelectMany(x => x.MatchInnings)
              .SelectMany(x => x.PlayerInnings)
              .Where(x => x.RunsScored.HasValue && x.RunsScored > 0)
              .Select(x => x.Batter.Player.PlayerId)
              .Distinct()
              .Count();
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_players_with_runs_scored_supports_filter_by_date()
        {
            var dateRangeGenerator = new DateRangeGenerator();
            var (fromDate, untilDate) = dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);

            var filter = new StatisticsFilter { FromDate = fromDate, UntilDate = untilDate };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate", new Dictionary<string, object> { { "FromDate", filter.FromDate }, { "UntilDate", filter.UntilDate } }));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, Mock.Of<IPlayerDataSource>());

            var result = await dataSource.ReadTotalPlayersWithRunsScored(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches
              .Where(x => x.StartTime >= filter.FromDate && x.StartTime <= filter.UntilDate)
              .SelectMany(x => x.MatchInnings)
              .SelectMany(x => x.PlayerInnings)
              .Where(x => x.RunsScored.HasValue && x.RunsScored > 0)
              .Select(x => x.Batter.Player.PlayerId)
              .Distinct()
              .Count();
            Assert.Equal(expected, result);
        }


        [Fact]
        public async Task Read_most_runs_scored_returns_player()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            var results = await dataSource.ReadMostRunsScored(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                                            .SelectMany(m => m.MatchInnings)
                                                                            .SelectMany(mi => mi.PlayerInnings)
                                                                            .Where(pi => pi.Batter.Player.PlayerId == x.PlayerId && pi.RunsScored.HasValue)
                                                                            .Sum(pi => pi.RunsScored) > 0);
            foreach (var player in expected)
            {
                var result = results.SingleOrDefault(x => x.Result.Player.PlayerId == player.PlayerId);
                Assert.NotNull(result);

                Assert.Equal(player.PlayerRoute, result.Result.Player.PlayerRoute);
                Assert.Equal(player.PlayerName(), result.Result.Player.PlayerName());
            }
        }

        [Fact]
        public async Task Read_most_runs_scored_returns_teams()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            var results = await dataSource.ReadMostRunsScored(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                                            .SelectMany(m => m.MatchInnings)
                                                                            .SelectMany(mi => mi.PlayerInnings)
                                                                            .Where(pi => pi.Batter.Player.PlayerId == x.PlayerId && pi.RunsScored.HasValue)
                                                                            .Sum(pi => pi.RunsScored) > 0);
            foreach (var player in expected)
            {
                var result = results.SingleOrDefault(x => x.Result.Player.PlayerId == player.PlayerId);
                Assert.NotNull(result);

                foreach (var identity in player.PlayerIdentities)
                {
                    var resultIdentity = result.Result.Player.PlayerIdentities.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId);
                    Assert.NotNull(resultIdentity);
                    Assert.Equal(identity.Team.TeamName, resultIdentity.Team.TeamName);
                }
            }
        }

        [Fact]
        public async Task Read_most_runs_scored_returns_statistics()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            await ActAndAssertStatistics(filter, dataSource, x => true, x => true).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_most_runs_scored_supports_statistics_filtered_by_club_id()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                Club = _databaseFixture.TestData.TeamWithFullDetails.Club
            };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND ClubId = @ClubId", new Dictionary<string, object> { { "ClubId", _databaseFixture.TestData.TeamWithFullDetails.Club.ClubId } }));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            await ActAndAssertStatistics(filter, dataSource, x => true, i => i.BattingTeam.Team.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId.Value).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_most_runs_scored_supports_statistics_filtered_by_team_id()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                Team = _databaseFixture.TestData.TeamWithFullDetails
            };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND TeamId = @TeamId", new Dictionary<string, object> { { "TeamId", _databaseFixture.TestData.TeamWithFullDetails.TeamId } }));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            await ActAndAssertStatistics(filter, dataSource, x => true, i => i.BattingTeam.Team.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId.Value).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_most_runs_scored_supports_statistics_filtered_by_match_location_id()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                MatchLocation = _databaseFixture.TestData.MatchLocations.First()
            };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND MatchLocationId = @MatchLocationId", new Dictionary<string, object> { { "MatchLocationId", _databaseFixture.TestData.MatchLocations.First().MatchLocationId } }));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            await ActAndAssertStatistics(filter, dataSource, x => x.MatchLocation?.MatchLocationId == filter.MatchLocation.MatchLocationId, x => true).ConfigureAwait(false);
        }


        [Fact]
        public async Task Read_most_runs_scored_supports_statistics_filtered_by_competition_id()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                Competition = _databaseFixture.TestData.Competitions.First()
            };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND CompetitionId = @CompetitionId", new Dictionary<string, object> { { "CompetitionId", _databaseFixture.TestData.Competitions.First().CompetitionId } }));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            await ActAndAssertStatistics(filter, dataSource, x => x.Season?.Competition?.CompetitionId == filter.Competition.CompetitionId, x => true).ConfigureAwait(false);
        }


        [Fact]
        public async Task Read_most_runs_scored_supports_statistics_filtered_by_season_id()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                Season = _databaseFixture.TestData.Competitions.First().Seasons.First()
            };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND SeasonId = @SeasonId", new Dictionary<string, object> { { "SeasonId", _databaseFixture.TestData.Competitions.First().Seasons.First().SeasonId } }));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            await ActAndAssertStatistics(filter, dataSource, x => x.Season?.SeasonId == filter.Season.SeasonId, x => true).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_most_runs_scored_supports_statistics_filtered_by_date()
        {
            var dateRangeGenerator = new DateRangeGenerator();
            var (fromDate, untilDate) = dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);

            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                FromDate = fromDate,
                UntilDate = untilDate
            };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate", new Dictionary<string, object> { { "FromDate", filter.FromDate }, { "UntilDate", filter.UntilDate } }));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            await ActAndAssertStatistics(filter, dataSource, x => x.StartTime >= filter.FromDate && x.StartTime <= filter.UntilDate, x => true).ConfigureAwait(false);
        }

        private async Task ActAndAssertStatistics(StatisticsFilter filter, SqlServerBestPlayerTotalStatisticsDataSource dataSource, Func<Stoolball.Matches.Match, bool> matchFilter, Func<MatchInnings, bool> matchInningsFilter)
        {
            var results = await dataSource.ReadMostRunsScored(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Select(p => new BestStatistic
            {
                Player = p,
                TotalMatches = (int)_databaseFixture.TestData.Matches
                            .Where(matchFilter)
                            .Count(m => m.MatchInnings.Where(matchInningsFilter).Any(mi =>
                                    mi.PlayerInnings.Any(pi => pi.Batter.Player.PlayerId == p.PlayerId || pi.DismissedBy?.Player.PlayerId == p.PlayerId || pi.Bowler?.Player.PlayerId == p.PlayerId) ||
                                    mi.OversBowled.Any(o => o.Bowler.Player.PlayerId == p.PlayerId) ||
                                    mi.BowlingFigures.Any(bf => bf.Bowler.Player.PlayerId == p.PlayerId)
                                    ) || m.Awards.Any(aw => aw.PlayerIdentity.Player.PlayerId == p.PlayerId)),
                TotalInnings = (int)_databaseFixture.TestData.Matches
                            .Where(matchFilter)
                            .SelectMany(m => m.MatchInnings)
                            .Where(matchInningsFilter)
                            .SelectMany(mi => mi.PlayerInnings)
                            .Count(pi => pi.Batter.Player.PlayerId == p.PlayerId && pi.RunsScored.HasValue),
                Total = (int)_databaseFixture.TestData.Matches
                            .Where(matchFilter)
                            .SelectMany(m => m.MatchInnings)
                            .Where(matchInningsFilter)
                            .SelectMany(mi => mi.PlayerInnings)
                            .Where(pi => pi.Batter.Player.PlayerId == p.PlayerId && pi.RunsScored.HasValue)
                            .Sum(pi => pi.RunsScored),
                Average = (_databaseFixture.TestData.Matches
                            .Where(matchFilter)
                            .SelectMany(m => m.MatchInnings)
                            .Where(matchInningsFilter)
                            .SelectMany(mi => mi.PlayerInnings)
                            .Any(pi => pi.Batter.Player.PlayerId == p.PlayerId && StatisticsConstants.DISMISSALS_THAT_ARE_OUT.Contains(pi.DismissalType) && pi.RunsScored.HasValue) ?
                                ((decimal)_databaseFixture.TestData.Matches
                                .Where(matchFilter)
                                .SelectMany(m => m.MatchInnings)
                                .Where(matchInningsFilter)
                                .SelectMany(mi => mi.PlayerInnings)
                                .Where(pi => pi.Batter.Player.PlayerId == p.PlayerId && pi.RunsScored.HasValue)
                                .Sum(pi => pi.RunsScored))
                                /
                                _databaseFixture.TestData.Matches
                                .Where(matchFilter)
                                .SelectMany(m => m.MatchInnings)
                                .Where(matchInningsFilter)
                                .SelectMany(mi => mi.PlayerInnings)
                                .Count(pi => pi.Batter.Player.PlayerId == p.PlayerId && StatisticsConstants.DISMISSALS_THAT_ARE_OUT.Contains(pi.DismissalType) && pi.RunsScored.HasValue)
                            : (decimal?)null)
            }).Where(x => x.Total > 0);

            foreach (var player in expected)
            {
                var result = results.SingleOrDefault(x => x.Result.Player.PlayerId == player.Player.PlayerId);
                Assert.NotNull(result);

                Assert.Equal(player.TotalMatches, result.Result.TotalMatches);
                Assert.Equal(player.TotalInnings, result.Result.TotalInnings);
                Assert.Equal(player.Total, result.Result.Total);
                if (player.Average.HasValue)
                {
                    Assert.Equal(player.Average.Value.AccurateToTwoDecimalPlaces(), result.Result.Average.Value.AccurateToTwoDecimalPlaces());
                }
                else
                {
                    Assert.Null(result.Result.Average);
                }
            }
        }

        [Fact]
        public async Task Read_most_runs_scored_supports_no_filter()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            var results = await dataSource.ReadMostRunsScored(null).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                                   .SelectMany(m => m.MatchInnings)
                                                                   .SelectMany(mi => mi.PlayerInnings)
                                                                   .Where(pi => pi.Batter.Player.PlayerId == x.PlayerId && pi.RunsScored.HasValue)
                                                                   .Sum(pi => pi.RunsScored) > 0);

            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.Player.PlayerId == player.PlayerId));
            }
        }

        [Fact]
        public async Task Read_most_runs_scored_supports_filter_by_club_id()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                Club = _databaseFixture.TestData.TeamWithFullDetails.Club
            };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND ClubId = @ClubId", new Dictionary<string, object> { { "ClubId", _databaseFixture.TestData.TeamWithFullDetails.Club.ClubId } }));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            var results = await dataSource.ReadMostRunsScored(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                                 .SelectMany(m => m.MatchInnings)
                                                                 .Where(i => i.BattingTeam.Team.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId.Value)
                                                                 .SelectMany(mi => mi.PlayerInnings)
                                                                 .Where(pi => pi.Batter.Player.PlayerId == x.PlayerId && pi.RunsScored.HasValue)
                                                                 .Sum(pi => pi.RunsScored) > 0);

            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.Player.PlayerId == player.PlayerId));
            }
        }

        [Fact]
        public async Task Read_most_runs_scored_supports_filter_by_team_id()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                Team = _databaseFixture.TestData.TeamWithFullDetails
            };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND TeamId = @TeamId", new Dictionary<string, object> { { "TeamId", _databaseFixture.TestData.TeamWithFullDetails.TeamId } }));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            var results = await dataSource.ReadMostRunsScored(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                                 .Where(x => x.Teams.Select(t => t.Team.TeamId).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamId.Value))
                                                                 .SelectMany(m => m.MatchInnings)
                                                                 .Where(i => i.BattingTeam.Team.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId.Value)
                                                                 .SelectMany(mi => mi.PlayerInnings)
                                                                 .Where(pi => pi.Batter.Player.PlayerId == x.PlayerId && pi.RunsScored.HasValue)
                                                                 .Sum(pi => pi.RunsScored) > 0);

            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.Player.PlayerId == player.PlayerId));
            }
        }

        [Fact]
        public async Task Read_most_runs_scored_supports_filter_by_match_location_id()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                MatchLocation = _databaseFixture.TestData.MatchLocations.First()
            };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND MatchLocationId = @MatchLocationId", new Dictionary<string, object> { { "MatchLocationId", _databaseFixture.TestData.MatchLocations.First().MatchLocationId } }));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            var results = await dataSource.ReadMostRunsScored(It.IsAny<StatisticsFilter>()).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                     .Where(x => x.MatchLocation?.MatchLocationId == _databaseFixture.TestData.MatchLocations.First().MatchLocationId)
                                                     .SelectMany(m => m.MatchInnings)
                                                     .SelectMany(mi => mi.PlayerInnings)
                                                     .Where(pi => pi.Batter.Player.PlayerId == x.PlayerId && pi.RunsScored.HasValue)
                                                     .Sum(pi => pi.RunsScored) > 0);

            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.Player.PlayerId == player.PlayerId));
            }
        }

        [Fact]
        public async Task Read_most_runs_scored_supports_filter_by_competition_id()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                Competition = _databaseFixture.TestData.Competitions.First()
            };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND CompetitionId = @CompetitionId", new Dictionary<string, object> { { "CompetitionId", _databaseFixture.TestData.Competitions.First().CompetitionId } }));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            var results = await dataSource.ReadMostRunsScored(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                     .Where(x => x.Season?.Competition?.CompetitionId == _databaseFixture.TestData.Competitions.First().CompetitionId)
                                                     .SelectMany(m => m.MatchInnings)
                                                     .SelectMany(mi => mi.PlayerInnings)
                                                     .Where(pi => pi.Batter.Player.PlayerId == x.PlayerId && pi.RunsScored.HasValue)
                                                     .Sum(pi => pi.RunsScored) > 0);

            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.Player.PlayerId == player.PlayerId));
            }
        }

        [Fact]
        public async Task Read_most_runs_scored_supports_filter_by_season_id()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                Season = _databaseFixture.TestData.Competitions.First().Seasons.First()
            };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND SeasonId = @SeasonId", new Dictionary<string, object> { { "SeasonId", _databaseFixture.TestData.Competitions.First().Seasons.First().SeasonId } }));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            var results = await dataSource.ReadMostRunsScored(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                     .Where(x => x.Season?.SeasonId == _databaseFixture.TestData.Competitions.First().Seasons.First().SeasonId)
                                                     .SelectMany(m => m.MatchInnings)
                                                     .SelectMany(mi => mi.PlayerInnings)
                                                     .Where(pi => pi.Batter.Player.PlayerId == x.PlayerId && pi.RunsScored.HasValue)
                                                     .Sum(pi => pi.RunsScored) > 0);

            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.Player.PlayerId == player.PlayerId));
            }
        }

        [Fact]
        public async Task Read_most_runs_sorts_by_highest_total_first()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            var results = await dataSource.ReadMostRunsScored(filter).ConfigureAwait(false);

            var previousTotal = int.MaxValue;
            foreach (var result in results)
            {
                Assert.True(result.Result.Total <= previousTotal);
                previousTotal = result.Result.Total.Value;
            }
        }

        [Fact]
        public async Task Read_most_runs_scored_pages_results()
        {
            const int pageSize = 10;
            var pageNumber = 1;

            var remaining = _databaseFixture.TestData.Matches
                            .SelectMany(x => x.MatchInnings)
                            .SelectMany(x => x.PlayerInnings)
                            .Where(x => x.RunsScored.HasValue && x.RunsScored > 0)
                            .Select(x => x.Batter.Player.PlayerId)
                            .Distinct()
                            .Count();
            while (remaining > 0)
            {
                var filter = new StatisticsFilter { Paging = new Paging { PageNumber = pageNumber, PageSize = pageSize } };
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
                var playerDataSource = new Mock<IPlayerDataSource>();
                playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
                var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);
                var results = await dataSource.ReadMostRunsScored(filter).ConfigureAwait(false);

                var expected = pageSize > remaining ? remaining : pageSize;
                Assert.Equal(expected, results.Count());

                pageNumber++;
                remaining -= pageSize;
            }
        }

        [Fact]
        public async Task Read_most_runs_returns_results_equal_to_max_with_max_results_filter()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue }, MaxResultsAllowingExtraResultsIfValuesAreEqual = 5 };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            var results = (await dataSource.ReadMostRunsScored(filter).ConfigureAwait(false)).ToList();

            // The test data is altered to ensure the 5th and 6th results are the same
            Assert.True(results.Count > 5);

            var fifthValue = results[4].Result.Total;
            for (var i = 4; i < results.Count; i++)
            {
                Assert.Equal(fifthValue, results[i].Result.Total);
            }
        }
    }
}
