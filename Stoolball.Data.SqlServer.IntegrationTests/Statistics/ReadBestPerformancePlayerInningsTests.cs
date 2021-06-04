using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Navigation;
using Stoolball.Statistics;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Statistics
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class ReadBestPerformancePlayerInningsTests
    {
        private readonly SqlServerTestDataFixture _databaseFixture;

        public ReadBestPerformancePlayerInningsTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_total_player_innings_supports_no_filter()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var result = await dataSource.ReadTotalPlayerInnings(null).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).Count(x => x.RunsScored.HasValue);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_player_innings_supports_filter_by_player_id()
        {
            var filter = new StatisticsFilter { Player = _databaseFixture.TestData.BowlerWithMultipleIdentities };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerId = @PlayerId", new Dictionary<string, object> { { "PlayerId", _databaseFixture.TestData.BowlerWithMultipleIdentities.PlayerId } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var result = await dataSource.ReadTotalPlayerInnings(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.PlayerInnings)
                .Count(x => x.Batter.Player.PlayerId == _databaseFixture.TestData.BowlerWithMultipleIdentities.PlayerId && x.RunsScored.HasValue);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_player_innings_supports_filter_by_club_id()
        {
            var filter = new StatisticsFilter { Club = _databaseFixture.TestData.TeamWithFullDetails.Club };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND ClubId = @ClubId", new Dictionary<string, object> { { "ClubId", _databaseFixture.TestData.TeamWithFullDetails.Club.ClubId } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var result = await dataSource.ReadTotalPlayerInnings(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.Where(x => x.Teams.Select(t => t.Team.TeamId).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamId.Value))
                .SelectMany(x => x.MatchInnings.Where(i => i.BattingTeam.Team.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId.Value))
                .SelectMany(x => x.PlayerInnings)
                .Count(x => x.RunsScored.HasValue);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_player_innings_supports_filter_by_team_id()
        {
            var filter = new StatisticsFilter { Team = _databaseFixture.TestData.TeamWithFullDetails };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND TeamId = @TeamId", new Dictionary<string, object> { { "TeamId", _databaseFixture.TestData.TeamWithFullDetails.TeamId } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var result = await dataSource.ReadTotalPlayerInnings(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.Where(x => x.Teams.Select(t => t.Team.TeamId).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamId.Value))
                .SelectMany(x => x.MatchInnings.Where(i => i.BattingTeam.Team.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId.Value))
                .SelectMany(x => x.PlayerInnings)
                .Count(x => x.RunsScored.HasValue);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_player_innings_supports_filter_by_match_location_id()
        {
            var filter = new StatisticsFilter { MatchLocation = _databaseFixture.TestData.MatchLocations.First() };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND MatchLocationId = @MatchLocationId", new Dictionary<string, object> { { "MatchLocationId", _databaseFixture.TestData.MatchLocations.First().MatchLocationId } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var result = await dataSource.ReadTotalPlayerInnings(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.Where(x => x.MatchLocation?.MatchLocationId == _databaseFixture.TestData.MatchLocations.First().MatchLocationId)
                .SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.PlayerInnings)
                .Count(x => x.RunsScored.HasValue);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_player_innings_supports_filter_by_competition_id()
        {
            var filter = new StatisticsFilter { Competition = _databaseFixture.TestData.Competitions.First() };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND CompetitionId = @CompetitionId", new Dictionary<string, object> { { "CompetitionId", _databaseFixture.TestData.Competitions.First().CompetitionId } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var result = await dataSource.ReadTotalPlayerInnings(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.Where(x => x.Season?.Competition?.CompetitionId == _databaseFixture.TestData.Competitions.First().CompetitionId)
                .SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.PlayerInnings)
                .Count(x => x.RunsScored.HasValue);
            Assert.Equal(expected, result);
        }


        [Fact]
        public async Task Read_total_player_innings_supports_filter_by_season_id()
        {
            var filter = new StatisticsFilter { Season = _databaseFixture.TestData.Competitions.First().Seasons.First() };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND SeasonId = @SeasonId", new Dictionary<string, object> { { "SeasonId", _databaseFixture.TestData.Competitions.First().Seasons.First().SeasonId } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var result = await dataSource.ReadTotalPlayerInnings(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.Where(x => x.Season?.SeasonId == _databaseFixture.TestData.Competitions.First().Seasons.First().SeasonId)
                .SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.PlayerInnings)
                .Count(x => x.RunsScored.HasValue);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_player_innings_returns_player()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var results = await dataSource.ReadPlayerInnings(filter, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).Where(x => x.RunsScored.HasValue).ToList();
            foreach (var expectedInnings in expected)
            {
                var result = results.SingleOrDefault(x => x.Result.PlayerInningsId == expectedInnings.PlayerInningsId);
                Assert.NotNull(result);

                Assert.Equal(expectedInnings.Batter.Player.PlayerId, result.Player.PlayerId);
                Assert.Equal(expectedInnings.Batter.Player.PlayerRoute, result.Player.PlayerRoute);
                Assert.Equal(expectedInnings.Batter.PlayerIdentityId, result.Player.PlayerIdentities.First().PlayerIdentityId);
                Assert.Equal(expectedInnings.Batter.PlayerIdentityName, result.Player.PlayerIdentities.First().PlayerIdentityName);
            }
        }

        [Fact]
        public async Task Read_player_innings_returns_innings()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var results = await dataSource.ReadPlayerInnings(filter, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).Where(x => x.RunsScored.HasValue).ToList();
            foreach (var expectedInnings in expected)
            {
                var result = results.SingleOrDefault(x => x.Result.PlayerInningsId == expectedInnings.PlayerInningsId);
                Assert.NotNull(result);

                Assert.Equal(expectedInnings.DismissalType, result.Result.DismissalType);
                Assert.Equal(expectedInnings.RunsScored, result.Result.RunsScored);
                Assert.Equal(expectedInnings.BallsFaced, result.Result.BallsFaced);
            }
        }

        [Fact]
        public async Task Read_player_innings_returns_team()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var results = await dataSource.ReadPlayerInnings(filter, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).Where(x => x.RunsScored.HasValue).ToList();
            foreach (var expectedInnings in expected)
            {
                var result = results.SingleOrDefault(x => x.Result.PlayerInningsId == expectedInnings.PlayerInningsId);
                Assert.NotNull(result);

                Assert.Equal(expectedInnings.Batter.Team.TeamId, result.Team.TeamId);
                Assert.Equal(expectedInnings.Batter.Team.TeamName, result.Team.TeamName);
            }
        }

        [Fact]
        public async Task Read_player_innings_returns_opposition_team()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var results = await dataSource.ReadPlayerInnings(filter, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var matchInnings = _databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings);
            foreach (var innings in matchInnings)
            {
                foreach (var playerInnings in innings.PlayerInnings.Where(x => x.RunsScored.HasValue))
                {
                    var result = results.SingleOrDefault(x => x.Result.PlayerInningsId == playerInnings.PlayerInningsId);
                    Assert.NotNull(result);

                    Assert.Equal(innings.BowlingTeam.Team.TeamId, result.OppositionTeam.TeamId);
                    Assert.Equal(innings.BowlingTeam.Team.TeamName, result.OppositionTeam.TeamName);
                }
            }
        }

        [Fact]
        public async Task Read_player_innings_returns_match()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var results = await dataSource.ReadPlayerInnings(filter, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            foreach (var match in _databaseFixture.TestData.Matches)
            {
                foreach (var innings in match.MatchInnings)
                {
                    foreach (var playerInnings in innings.PlayerInnings.Where(x => x.RunsScored.HasValue))
                    {
                        var result = results.SingleOrDefault(x => x.Result.PlayerInningsId == playerInnings.PlayerInningsId);
                        Assert.NotNull(result);

                        Assert.Equal(match.MatchRoute, result.Match.MatchRoute);
                        Assert.Equal(match.StartTime.AccurateToTheMinute(), result.Match.StartTime.AccurateToTheMinute());
                        Assert.Equal(match.MatchName, result.Match.MatchName);
                    }
                }
            }
        }

        [Fact]
        public async Task Read_player_innings_supports_no_filter()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var results = await dataSource.ReadPlayerInnings(filter, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).Where(x => x.RunsScored.HasValue).ToList();

            Assert.Equal(expected.Count, results.Count());
            foreach (var expectedInnings in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.PlayerInningsId == expectedInnings.PlayerInningsId));
            }
        }

        [Fact]
        public async Task Read_player_innings_supports_filter_by_player_id()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                Player = _databaseFixture.TestData.BowlerWithMultipleIdentities
            };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerId = @PlayerId", new Dictionary<string, object> { { "PlayerId", _databaseFixture.TestData.BowlerWithMultipleIdentities.PlayerId } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var results = await dataSource.ReadPlayerInnings(filter, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.PlayerInnings)
                .Where(x => x.Batter.Player.PlayerId == _databaseFixture.TestData.BowlerWithMultipleIdentities.PlayerId && x.RunsScored.HasValue).ToList();
            Assert.Equal(expected.Count, results.Count());
            foreach (var expectedInnings in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.PlayerInningsId == expectedInnings.PlayerInningsId));
            }
        }

        [Fact]
        public async Task Read_player_innings_supports_filter_by_club_id()
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
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND ClubId = @ClubId", new Dictionary<string, object> { { "ClubId", _databaseFixture.TestData.TeamWithFullDetails.Club.ClubId } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var results = await dataSource.ReadPlayerInnings(filter, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.Where(x => x.Teams.Select(t => t.Team.TeamId).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamId.Value))
                .SelectMany(x => x.MatchInnings.Where(i => i.BattingTeam.Team.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId.Value))
                .SelectMany(x => x.PlayerInnings)
                .Where(x => x.RunsScored.HasValue).ToList();
            Assert.Equal(expected.Count, results.Count());
            foreach (var expectedInnings in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.PlayerInningsId == expectedInnings.PlayerInningsId));
            }
        }

        [Fact]
        public async Task Read_player_innings_supports_filter_by_team_id()
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
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND TeamId = @TeamId", new Dictionary<string, object> { { "TeamId", _databaseFixture.TestData.TeamWithFullDetails.TeamId } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var results = await dataSource.ReadPlayerInnings(filter, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.Where(x => x.Teams.Select(t => t.Team.TeamId).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamId.Value))
                .SelectMany(x => x.MatchInnings.Where(i => i.BattingTeam.Team.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId.Value))
                .SelectMany(x => x.PlayerInnings)
                .Where(x => x.RunsScored.HasValue).ToList();
            Assert.Equal(expected.Count, results.Count());
            foreach (var expectedInnings in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.PlayerInningsId == expectedInnings.PlayerInningsId));
            }
        }

        [Fact]
        public async Task Read_player_innings_supports_filter_by_match_location_id()
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
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND MatchLocationId = @MatchLocationId", new Dictionary<string, object> { { "MatchLocationId", _databaseFixture.TestData.MatchLocations.First().MatchLocationId } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var results = await dataSource.ReadPlayerInnings(filter, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.Where(x => x.MatchLocation?.MatchLocationId == _databaseFixture.TestData.MatchLocations.First().MatchLocationId)
                .SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.PlayerInnings)
                .Where(x => x.RunsScored.HasValue).ToList();
            Assert.Equal(expected.Count, results.Count());
            foreach (var expectedInnings in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.PlayerInningsId == expectedInnings.PlayerInningsId));
            }
        }

        [Fact]
        public async Task Read_player_innings_supports_filter_by_competition_id()
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
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND CompetitionId = @CompetitionId", new Dictionary<string, object> { { "CompetitionId", _databaseFixture.TestData.Competitions.First().CompetitionId } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var results = await dataSource.ReadPlayerInnings(filter, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.Where(x => x.Season?.Competition?.CompetitionId == _databaseFixture.TestData.Competitions.First().CompetitionId)
                .SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.PlayerInnings)
                .Where(x => x.RunsScored.HasValue).ToList();
            Assert.Equal(expected.Count, results.Count());
            foreach (var expectedInnings in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.PlayerInningsId == expectedInnings.PlayerInningsId));
            }
        }

        [Fact]
        public async Task Read_player_innings_supports_filter_by_season_id()
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
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND SeasonId = @SeasonId", new Dictionary<string, object> { { "SeasonId", _databaseFixture.TestData.Competitions.First().Seasons.First().SeasonId } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var results = await dataSource.ReadPlayerInnings(filter, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.Where(x => x.Season?.SeasonId == _databaseFixture.TestData.Competitions.First().Seasons.First().SeasonId)
                .SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.PlayerInnings)
                .Where(x => x.RunsScored.HasValue).ToList();
            Assert.Equal(expected.Count, results.Count());
            foreach (var expectedInnings in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.PlayerInningsId == expectedInnings.PlayerInningsId));
            }
        }

        [Fact]
        public async Task Read_best_player_innings_sorts_by_highest_score_with_not_out_first()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var results = await dataSource.ReadPlayerInnings(filter, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var previousScore = int.MaxValue;
            var previousScoreWasOut = false;
            foreach (var result in results)
            {
                Assert.True(result.Result.RunsScored.Value <= previousScore);

                if (result.Result.RunsScored.Value == previousScore && previousScoreWasOut)
                {
                    Assert.Contains(result.Result.DismissalType, StatisticsConstants.DISMISSALS_THAT_ARE_OUT);
                }

                previousScore = result.Result.RunsScored.Value;
                previousScoreWasOut = StatisticsConstants.DISMISSALS_THAT_ARE_OUT.Contains(result.Result.DismissalType);
            }
        }

        [Fact]
        public async Task Read_latest_player_innings_sorts_by_most_recent_first()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var results = await dataSource.ReadPlayerInnings(filter, StatisticsSortOrder.LatestFirst).ConfigureAwait(false);

            var previousInningsStartTime = DateTimeOffset.MaxValue;
            foreach (var result in results)
            {
                Assert.True(result.Match.StartTime.AccurateToTheMinute() <= previousInningsStartTime);
                previousInningsStartTime = result.Match.StartTime.AccurateToTheMinute();
            }
        }

        [Fact]
        public async Task Read_player_innings_pages_results()
        {
            const int pageSize = 10;
            var pageNumber = 1;
            var remaining = _databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).Count(x => x.RunsScored.HasValue);
            while (remaining > 0)
            {
                var filter = new StatisticsFilter { Paging = new Paging { PageNumber = pageNumber, PageSize = pageSize } };
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));
                var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);
                var results = await dataSource.ReadPlayerInnings(filter, StatisticsSortOrder.LatestFirst).ConfigureAwait(false);

                var expected = pageSize > remaining ? remaining : pageSize;
                Assert.Equal(expected, results.Count());

                pageNumber++;
                remaining -= pageSize;
            }
        }
    }
}
