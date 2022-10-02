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
    public class ReadMostPlayerInningsTests
    {
        private readonly SqlServerTestDataFixture _databaseFixture;

        public ReadMostPlayerInningsTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_most_player_innings_returns_player()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            var results = await dataSource.ReadMostPlayerInnings(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                                            .SelectMany(m => m.MatchInnings)
                                                                            .SelectMany(mi => mi.PlayerInnings)
                                                                            .Any(pi => pi.Batter.Player.PlayerId == x.PlayerId && pi.RunsScored.HasValue));
            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                var result = results.SingleOrDefault(x => x.Result.Player.PlayerId == player.PlayerId);
                Assert.NotNull(result);

                Assert.Equal(player.PlayerRoute, result!.Result.Player.PlayerRoute);
                Assert.Equal(player.PlayerName(), result.Result.Player.PlayerName());
            }
        }

        [Fact]
        public async Task Read_most_player_innings_returns_teams()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            var results = await dataSource.ReadMostPlayerInnings(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                                            .SelectMany(m => m.MatchInnings)
                                                                            .SelectMany(mi => mi.PlayerInnings)
                                                                            .Any(pi => pi.Batter.Player.PlayerId == x.PlayerId && pi.RunsScored.HasValue));
            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                var result = results.SingleOrDefault(x => x.Result.Player.PlayerId == player.PlayerId);
                Assert.NotNull(result);

                foreach (var identity in player.PlayerIdentities)
                {
                    var resultIdentity = result!.Result.Player.PlayerIdentities.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId);
                    Assert.NotNull(resultIdentity);
                    Assert.Equal(identity.Team.TeamName, resultIdentity!.Team.TeamName);
                }
            }
        }

        [Fact]
        public async Task Read_most_player_innings_returns_statistics()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            await ActAndAssertStatistics(filter, dataSource, x => true, x => true, x => true, x => true).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_most_player_innings_supports_statistics_filtered_by_club_id()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                Club = _databaseFixture.TestData.TeamWithFullDetails!.Club
            };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND ClubId = @ClubId", new Dictionary<string, object> { { "ClubId", _databaseFixture.TestData.TeamWithFullDetails.Club.ClubId! } }));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            await ActAndAssertStatistics(filter, dataSource, x => true, i => i.BowlingTeam.Team.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId, i => i.BattingTeam.Team.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId, x => true).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_most_player_innings_supports_statistics_filtered_by_team_id()
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
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND TeamId = @TeamId", new Dictionary<string, object> { { "TeamId", _databaseFixture.TestData.TeamWithFullDetails!.TeamId! } }));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            await ActAndAssertStatistics(filter, dataSource, x => true, i => i.BowlingTeam.Team.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId, i => i.BattingTeam.Team.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId, x => true).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_most_player_innings_supports_statistics_filtered_by_match_location_id()
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
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND MatchLocationId = @MatchLocationId", new Dictionary<string, object> { { "MatchLocationId", _databaseFixture.TestData.MatchLocations.First().MatchLocationId! } }));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            await ActAndAssertStatistics(filter, dataSource, x => x.MatchLocation?.MatchLocationId == filter.MatchLocation.MatchLocationId, x => true, x => true, x => true).ConfigureAwait(false);
        }


        [Fact]
        public async Task Read_most_player_innings_supports_statistics_filtered_by_competition_id()
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
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND CompetitionId = @CompetitionId", new Dictionary<string, object> { { "CompetitionId", _databaseFixture.TestData.Competitions.First().CompetitionId! } }));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            await ActAndAssertStatistics(filter, dataSource, x => x.Season?.Competition?.CompetitionId == filter.Competition.CompetitionId, x => true, x => true, x => true).ConfigureAwait(false);
        }


        [Fact]
        public async Task Read_most_player_innings_supports_statistics_filtered_by_season_id()
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
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND SeasonId = @SeasonId", new Dictionary<string, object> { { "SeasonId", _databaseFixture.TestData.Competitions.First().Seasons.First().SeasonId! } }));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            await ActAndAssertStatistics(filter, dataSource, x => x.Season?.SeasonId == filter.Season.SeasonId, x => true, x => true, x => true).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_most_player_innings_supports_statistics_filtered_by_date()
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

            await ActAndAssertStatistics(filter, dataSource, x => x.StartTime >= filter.FromDate && x.StartTime <= filter.UntilDate, x => true, x => true, x => true).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_most_player_innings_supports_statistics_filtered_by_minimum_score()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                MinimumRunsScored = 18,
            };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(It.Is<StatisticsFilter>(x => x.MinimumRunsScored.HasValue))).Returns((" AND RunsScored >= @MinimumRunsScored", new Dictionary<string, object> { { "MinimumRunsScored", filter.MinimumRunsScored } }));
            queryBuilder.Setup(x => x.BuildWhereClause(It.Is<StatisticsFilter>(x => !x.MinimumRunsScored.HasValue))).Returns((string.Empty, new Dictionary<string, object>()));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            await ActAndAssertStatistics(filter, dataSource, x => true, x => true, x => true, x => x.RunsScored.HasValue && x.RunsScored >= filter.MinimumRunsScored).ConfigureAwait(false);
        }

        private async Task ActAndAssertStatistics(StatisticsFilter filter, SqlServerBestPlayerTotalStatisticsDataSource dataSource, Func<Stoolball.Matches.Match, bool> matchFilter, Func<MatchInnings, bool> bowlingInningsFilter, Func<MatchInnings, bool> battingInningsFilter, Func<PlayerInnings, bool> playerInningsFilter)
        {
            var results = await dataSource.ReadMostPlayerInnings(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Select(p => new BestStatistic
            {
                Player = p,
                TotalMatches = _databaseFixture.TestData.Matches
                            .Where(matchFilter)
                            .Count(m => m.MatchInnings.Where(battingInningsFilter).Any(mi => mi.PlayerInnings.Any(pi => pi.Batter.Player.PlayerId == p.PlayerId))
                                    || m.MatchInnings.Where(bowlingInningsFilter).Any(mi =>
                                        mi.PlayerInnings.Any(pi => pi.DismissedBy?.Player.PlayerId == p.PlayerId || pi.Bowler?.Player.PlayerId == p.PlayerId) ||
                                        mi.OversBowled.Any(o => o.Bowler.Player.PlayerId == p.PlayerId) ||
                                        mi.BowlingFigures.Any(bf => bf.Bowler.Player.PlayerId == p.PlayerId)
                                    ) || m.Awards.Any(aw => aw.PlayerIdentity.Player.PlayerId == p.PlayerId)),
                TotalInnings = _databaseFixture.TestData.Matches
                            .Where(matchFilter)
                            .SelectMany(m => m.MatchInnings)
                            .Where(battingInningsFilter)
                            .SelectMany(mi => mi.PlayerInnings)
                            .Count(pi => pi.Batter.Player.PlayerId == p.PlayerId && pi.RunsScored.HasValue),
                Total = _databaseFixture.TestData.Matches
                            .Where(matchFilter)
                            .SelectMany(m => m.MatchInnings)
                            .Where(battingInningsFilter)
                            .SelectMany(mi => mi.PlayerInnings)
                            .Count(pi => pi.Batter.Player.PlayerId == p.PlayerId && pi.RunsScored.HasValue && playerInningsFilter(pi))
            }).Where(x => x.Total > 0);

            foreach (var player in expected)
            {
                var result = results.SingleOrDefault(x => x.Result.Player.PlayerId == player.Player.PlayerId);
                Assert.NotNull(result);

                Assert.Equal(player.TotalMatches, result!.Result.TotalMatches);
                Assert.Equal(player.TotalInnings, result.Result.TotalInnings);
                Assert.Equal(player.Total, result.Result.Total);
            }
        }

        [Fact]
        public async Task Read_most_player_innings_supports_no_filter()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            var results = await dataSource.ReadMostPlayerInnings(null).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                                   .SelectMany(m => m.MatchInnings)
                                                                   .SelectMany(mi => mi.PlayerInnings)
                                                                   .Any(pi => pi.Batter.Player.PlayerId == x.PlayerId && pi.RunsScored.HasValue));

            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.Player.PlayerId == player.PlayerId));
            }
        }

        [Fact]
        public async Task Read_most_player_innings_supports_filter_by_club_id()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                Club = _databaseFixture.TestData.TeamWithFullDetails!.Club
            };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND ClubId = @ClubId", new Dictionary<string, object> { { "ClubId", _databaseFixture.TestData.TeamWithFullDetails.Club.ClubId! } }));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            var results = await dataSource.ReadMostPlayerInnings(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                                 .SelectMany(m => m.MatchInnings)
                                                                 .Where(i => i.BattingTeam.Team.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId)
                                                                 .SelectMany(mi => mi.PlayerInnings)
                                                                 .Any(pi => pi.Batter.Player.PlayerId == x.PlayerId && pi.RunsScored.HasValue));

            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.Player.PlayerId == player.PlayerId));
            }
        }

        [Fact]
        public async Task Read_most_player_innings_supports_filter_by_team_id()
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
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND TeamId = @TeamId", new Dictionary<string, object> { { "TeamId", _databaseFixture.TestData.TeamWithFullDetails!.TeamId! } }));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            var results = await dataSource.ReadMostPlayerInnings(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                                 .Where(x => x.Teams.Select(t => t.Team.TeamId).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamId))
                                                                 .SelectMany(m => m.MatchInnings)
                                                                 .Where(i => i.BattingTeam.Team.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId)
                                                                 .SelectMany(mi => mi.PlayerInnings)
                                                                 .Any(pi => pi.Batter.Player.PlayerId == x.PlayerId && pi.RunsScored.HasValue));

            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.Player.PlayerId == player.PlayerId));
            }
        }

        [Fact]
        public async Task Read_most_player_innings_supports_filter_by_match_location_id()
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
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND MatchLocationId = @MatchLocationId", new Dictionary<string, object> { { "MatchLocationId", _databaseFixture.TestData.MatchLocations.First().MatchLocationId! } }));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            var results = await dataSource.ReadMostPlayerInnings(It.IsAny<StatisticsFilter>()).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                     .Where(x => x.MatchLocation?.MatchLocationId == _databaseFixture.TestData.MatchLocations.First().MatchLocationId)
                                                     .SelectMany(m => m.MatchInnings)
                                                     .SelectMany(mi => mi.PlayerInnings)
                                                     .Any(pi => pi.Batter.Player.PlayerId == x.PlayerId && pi.RunsScored.HasValue));

            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.Player.PlayerId == player.PlayerId));
            }
        }

        [Fact]
        public async Task Read_most_player_innings_supports_filter_by_competition_id()
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
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND CompetitionId = @CompetitionId", new Dictionary<string, object> { { "CompetitionId", _databaseFixture.TestData.Competitions.First().CompetitionId! } }));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            var results = await dataSource.ReadMostPlayerInnings(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                     .Where(x => x.Season?.Competition?.CompetitionId == _databaseFixture.TestData.Competitions.First().CompetitionId)
                                                     .SelectMany(m => m.MatchInnings)
                                                     .SelectMany(mi => mi.PlayerInnings)
                                                     .Any(pi => pi.Batter.Player.PlayerId == x.PlayerId && pi.RunsScored.HasValue));

            var monkey = expected.Count() == results.Count();
            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.Player.PlayerId == player.PlayerId));
            }
        }

        [Fact]
        public async Task Read_most_player_innings_supports_filter_by_season_id()
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
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND SeasonId = @SeasonId", new Dictionary<string, object> { { "SeasonId", _databaseFixture.TestData.Competitions.First().Seasons.First().SeasonId! } }));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            var results = await dataSource.ReadMostPlayerInnings(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                     .Where(x => x.Season?.SeasonId == _databaseFixture.TestData.Competitions.First().Seasons.First().SeasonId)
                                                     .SelectMany(m => m.MatchInnings)
                                                     .SelectMany(mi => mi.PlayerInnings)
                                                     .Any(pi => pi.Batter.Player.PlayerId == x.PlayerId && pi.RunsScored.HasValue));

            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.Player.PlayerId == player.PlayerId));
            }
        }


        [Fact]
        public async Task Read_most_player_innings_supports_filter_by_date()
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

            var results = await dataSource.ReadMostPlayerInnings(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                     .Where(x => x.StartTime >= filter.FromDate && x.StartTime <= filter.UntilDate)
                                                     .SelectMany(m => m.MatchInnings)
                                                     .SelectMany(mi => mi.PlayerInnings)
                                                     .Any(pi => pi.Batter.Player.PlayerId == x.PlayerId && pi.RunsScored.HasValue));

            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.Player.PlayerId == player.PlayerId));
            }
        }


        [Fact]
        public async Task Read_most_player_innings_supports_filter_by_minimum_score()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                MinimumRunsScored = 15
            };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND RunsScored >= @MinimumRunsScored", new Dictionary<string, object> { { "MinimumRunsScored", filter.MinimumRunsScored } }));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            var results = await dataSource.ReadMostPlayerInnings(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                     .SelectMany(m => m.MatchInnings)
                                                     .SelectMany(mi => mi.PlayerInnings)
                                                     .Any(pi => pi.Batter.Player.PlayerId == x.PlayerId && pi.RunsScored.HasValue && pi.RunsScored >= filter.MinimumRunsScored));

            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.Player.PlayerId == player.PlayerId));
            }
        }

        [Fact]
        public async Task Read_most_player_innings_sorts_by_highest_total_first()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            var playerDataSource = new Mock<IPlayerDataSource>();
            playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);

            var results = await dataSource.ReadMostPlayerInnings(filter).ConfigureAwait(false);

            Assert.True(results.Count() > 0);
            int? previousTotal = int.MaxValue;
            foreach (var result in results)
            {
                Assert.True(result.Result.Total <= previousTotal);
                previousTotal = result.Result.Total;
            }
        }

        [Fact]
        public async Task Read_most_player_innings_pages_results()
        {
            const int pageSize = 10;
            var pageNumber = 1;

            var remaining = _databaseFixture.TestData.Matches
                            .SelectMany(x => x.MatchInnings)
                            .SelectMany(x => x.PlayerInnings)
                            .Where(x => x.RunsScored.HasValue)
                            .Select(x => x.Batter.Player.PlayerId)
                            .Distinct()
                            .Count();

            Assert.True(remaining > 0);
            while (remaining > 0)
            {
                var filter = new StatisticsFilter { Paging = new Paging { PageNumber = pageNumber, PageSize = pageSize } };
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
                var playerDataSource = new Mock<IPlayerDataSource>();
                playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
                var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object, playerDataSource.Object);
                var results = await dataSource.ReadMostPlayerInnings(filter).ConfigureAwait(false);

                var expected = pageSize > remaining ? remaining : pageSize;
                Assert.Equal(expected, results.Count());

                pageNumber++;
                remaining -= pageSize;
            }
        }
    }
}
