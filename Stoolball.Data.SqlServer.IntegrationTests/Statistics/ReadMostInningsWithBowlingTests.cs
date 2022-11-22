using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Statistics;
using Stoolball.Testing;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Statistics
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class ReadMostInningsWithBowlingTests
    {
        private readonly SqlServerTestDataFixture _databaseFixture;
        private readonly Mock<IStatisticsQueryBuilder> _queryBuilder = new();
        private readonly Mock<IPlayerDataSource> _playerDataSource = new();
        private readonly TestDataQueryHelper _queryHelper = new();

        public ReadMostInningsWithBowlingTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_most_innings_with_bowling_returns_player()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var results = await dataSource.ReadMostInningsWithBowling(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                                            .SelectMany(m => m.MatchInnings)
                                                                            .SelectMany(mi => mi.BowlingFigures)
                                                                            .Any(pi => pi.Bowler?.Player?.PlayerId == x.PlayerId));
            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                var result = results.SingleOrDefault(x => x.Result?.Player?.PlayerId == player.PlayerId);
                Assert.NotNull(result);

                Assert.Equal(player.PlayerRoute, result!.Result?.Player?.PlayerRoute);
                Assert.Equal(player.PlayerName(), result.Result?.Player?.PlayerName());
            }
        }

        [Fact]
        public async Task Read_most_innings_with_bowling_returns_teams()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var results = await dataSource.ReadMostInningsWithBowling(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                                            .SelectMany(m => m.MatchInnings)
                                                                            .SelectMany(mi => mi.BowlingFigures)
                                                                            .Any(pi => pi.Bowler?.Player?.PlayerId == x.PlayerId));
            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                var result = results.SingleOrDefault(x => x.Result?.Player?.PlayerId == player.PlayerId);
                Assert.NotNull(result);

                foreach (var identity in player.PlayerIdentities)
                {
                    var resultIdentity = result!.Result?.Player?.PlayerIdentities.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId);
                    Assert.NotNull(resultIdentity);
                    Assert.Equal(identity.Team?.TeamName, resultIdentity!.Team?.TeamName);
                }
            }
        }

        [Fact]
        public async Task Read_most_innings_with_bowling_returns_statistics()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            await ActAndAssertStatistics(filter, dataSource, x => true, x => true, x => true, x => true, x => true).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_most_innings_with_bowling_supports_statistics_filtered_by_club_id()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                Club = _databaseFixture.TestData.TeamWithFullDetails!.Club
            };
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND ClubId = @ClubId", new Dictionary<string, object> { { "ClubId", _databaseFixture.TestData.TeamWithFullDetails.Club!.ClubId! } }));
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var teamIdsForClub = filter.Club!.Teams.Select(x => x.TeamId);

            await ActAndAssertStatistics(filter, dataSource,
                x => true,
                i => teamIdsForClub.Contains(i.BattingTeam?.Team?.TeamId),
                i => teamIdsForClub.Contains(i.BowlingTeam?.Team?.TeamId),
                x => true,
                aw => teamIdsForClub.Contains(aw.PlayerIdentity?.Team?.TeamId)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_most_innings_with_bowling_supports_statistics_filtered_by_team_id()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                Team = _databaseFixture.TestData.TeamWithFullDetails
            };
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND TeamId = @TeamId", new Dictionary<string, object> { { "TeamId", _databaseFixture.TestData.TeamWithFullDetails!.TeamId! } }));
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            await ActAndAssertStatistics(filter, dataSource,
                x => true,
                i => i.BattingTeam?.Team?.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId,
                i => i.BowlingTeam?.Team?.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId,
                x => true,
                aw => aw.PlayerIdentity?.Team?.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_most_innings_with_bowling_supports_statistics_filtered_by_team_route()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                Team = _databaseFixture.TestData.TeamWithFullDetails
            };
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND TeamRoute = @TeamRoute", new Dictionary<string, object> { { "TeamRoute", _databaseFixture.TestData.TeamWithFullDetails!.TeamRoute! } }));
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            await ActAndAssertStatistics(filter, dataSource,
                x => true,
                i => i.BattingTeam?.Team?.TeamRoute == _databaseFixture.TestData.TeamWithFullDetails.TeamRoute,
                i => i.BowlingTeam?.Team?.TeamRoute == _databaseFixture.TestData.TeamWithFullDetails.TeamRoute,
                x => true,
                aw => aw.PlayerIdentity?.Team?.TeamRoute == _databaseFixture.TestData.TeamWithFullDetails.TeamRoute).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_most_innings_with_bowling_supports_statistics_filtered_by_match_location_id()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                MatchLocation = _databaseFixture.TestData.MatchLocations.First()
            };
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND MatchLocationId = @MatchLocationId", new Dictionary<string, object> { { "MatchLocationId", _databaseFixture.TestData.MatchLocations.First().MatchLocationId! } }));
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            await ActAndAssertStatistics(filter, dataSource,
                x => x.MatchLocation?.MatchLocationId == filter.MatchLocation.MatchLocationId,
                x => true,
                x => true,
                x => true,
                x => true).ConfigureAwait(false);
        }


        [Fact]
        public async Task Read_most_innings_with_bowling_supports_statistics_filtered_by_competition_id()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                Competition = _databaseFixture.TestData.Competitions.First()
            };
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND CompetitionId = @CompetitionId", new Dictionary<string, object> { { "CompetitionId", _databaseFixture.TestData.Competitions.First().CompetitionId! } }));
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            await ActAndAssertStatistics(filter, dataSource,
                x => x.Season?.Competition?.CompetitionId == filter.Competition.CompetitionId,
                x => true,
                x => true,
                x => true,
                x => true).ConfigureAwait(false);
        }


        [Fact]
        public async Task Read_most_innings_with_bowling_supports_statistics_filtered_by_season_id()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                Season = _databaseFixture.TestData.Competitions.First().Seasons.First()
            };
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND SeasonId = @SeasonId", new Dictionary<string, object> { { "SeasonId", _databaseFixture.TestData.Competitions.First().Seasons.First().SeasonId! } }));
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            await ActAndAssertStatistics(filter, dataSource,
                x => x.Season?.SeasonId == filter.Season.SeasonId,
                x => true,
                x => true,
                x => true,
                x => true).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_most_innings_with_bowling_supports_statistics_filtered_by_date()
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
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate", new Dictionary<string, object> { { "FromDate", filter.FromDate }, { "UntilDate", filter.UntilDate } }));
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            await ActAndAssertStatistics(filter, dataSource,
                x => x.StartTime >= filter.FromDate && x.StartTime <= filter.UntilDate,
                x => true,
                x => true,
                x => true,
                x => true).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_most_innings_with_bowling_supports_statistics_filtered_by_minimum_wickets()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                MinimumWicketsTaken = 5,
            };
            _queryBuilder.Setup(x => x.BuildWhereClause(It.Is<StatisticsFilter>(x => x.MinimumWicketsTaken.HasValue))).Returns((" AND Wickets >= @MinimumWicketsTaken", new Dictionary<string, object> { { "MinimumWicketsTaken", filter.MinimumWicketsTaken } }));
            _queryBuilder.Setup(x => x.BuildWhereClause(It.Is<StatisticsFilter>(x => !x.MinimumWicketsTaken.HasValue))).Returns((string.Empty, new Dictionary<string, object>()));
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            await ActAndAssertStatistics(filter, dataSource,
                x => true,
                x => true,
                x => true,
                x => x.Wickets >= filter.MinimumWicketsTaken,
                x => true).ConfigureAwait(false);
        }

        private async Task ActAndAssertStatistics(StatisticsFilter filter,
            SqlServerBestPlayerTotalStatisticsDataSource dataSource,
            Func<Stoolball.Matches.Match, bool> matchFilter,
            Func<MatchInnings, bool> battingInningsFilter,
            Func<MatchInnings, bool> bowlingInningsFilter,
            Func<BowlingFigures, bool> bowlingFiguresFilter,
            Func<MatchAward, bool> awardsFilter)
        {
            var results = await dataSource.ReadMostInningsWithBowling(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Select(p => new BestStatistic
            {
                Player = p,
                TotalMatches = _queryHelper.TotalMatchesForPlayer(p, _databaseFixture.TestData.Matches, matchFilter, bowlingInningsFilter, battingInningsFilter, awardsFilter),
                TotalInnings = _databaseFixture.TestData.Matches
                            .Where(matchFilter)
                            .SelectMany(m => m.MatchInnings)
                            .Where(bowlingInningsFilter)
                            .SelectMany(mi => mi.BowlingFigures)
                            .Count(bf => bf.Bowler?.Player?.PlayerId == p.PlayerId),
                Total = _databaseFixture.TestData.Matches
                            .Where(matchFilter)
                            .SelectMany(m => m.MatchInnings)
                            .Where(bowlingInningsFilter)
                            .SelectMany(mi => mi.BowlingFigures)
                            .Count(bf => bf.Bowler?.Player?.PlayerId == p.PlayerId && bowlingFiguresFilter(bf))
            }).Where(x => x.Total > 0);

            foreach (var player in expected)
            {
                var result = results.SingleOrDefault(x => x.Result?.Player?.PlayerId == player.Player?.PlayerId);
                Assert.NotNull(result);

                Assert.Equal(player.TotalMatches, result!.Result?.TotalMatches);
                Assert.Equal(player.TotalInnings, result.Result?.TotalInnings);
                Assert.Equal(player.Total, result.Result?.Total);
            }
        }

        [Fact]
        public async Task Read_most_innings_with_bowling_supports_no_filter()
        {
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var results = await dataSource.ReadMostInningsWithBowling(null).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                                   .SelectMany(m => m.MatchInnings)
                                                                   .SelectMany(mi => mi.BowlingFigures)
                                                                   .Any(bf => bf.Bowler?.Player?.PlayerId == x.PlayerId));

            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result?.Player?.PlayerId == player.PlayerId));
            }
        }

        [Fact]
        public async Task Read_most_innings_with_bowling_supports_filter_by_club_id()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                Club = _databaseFixture.TestData.TeamWithFullDetails!.Club
            };
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND ClubId = @ClubId", new Dictionary<string, object> { { "ClubId", _databaseFixture.TestData.TeamWithFullDetails.Club!.ClubId! } }));
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var results = await dataSource.ReadMostInningsWithBowling(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                                 .SelectMany(m => m.MatchInnings)
                                                                 .Where(i => i.BowlingTeam?.Team?.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId)
                                                                 .SelectMany(mi => mi.BowlingFigures)
                                                                 .Any(bf => bf.Bowler?.Player?.PlayerId == x.PlayerId));

            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result?.Player?.PlayerId == player.PlayerId));
            }
        }

        [Fact]
        public async Task Read_most_innings_with_bowling_supports_filter_by_team_id()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                Team = _databaseFixture.TestData.TeamWithFullDetails
            };
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND TeamId = @TeamId", new Dictionary<string, object> { { "TeamId", _databaseFixture.TestData.TeamWithFullDetails!.TeamId! } }));
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var results = await dataSource.ReadMostInningsWithBowling(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                                 .Where(x => x.Teams.Select(t => t.Team?.TeamId).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamId))
                                                                 .SelectMany(m => m.MatchInnings)
                                                                 .Where(i => i.BowlingTeam?.Team?.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId)
                                                                 .SelectMany(mi => mi.BowlingFigures)
                                                                 .Any(bf => bf.Bowler?.Player?.PlayerId == x.PlayerId));

            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result?.Player?.PlayerId == player.PlayerId));
            }
        }

        [Fact]
        public async Task Read_most_innings_with_bowling_supports_filter_by_team_route()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                Team = _databaseFixture.TestData.TeamWithFullDetails
            };
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND TeamRoute = @TeamRoute", new Dictionary<string, object> { { "TeamRoute", _databaseFixture.TestData.TeamWithFullDetails!.TeamRoute! } }));
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var results = await dataSource.ReadMostInningsWithBowling(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                                 .Where(x => x.Teams.Select(t => t.Team?.TeamRoute).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamRoute))
                                                                 .SelectMany(m => m.MatchInnings)
                                                                 .Where(i => i.BowlingTeam?.Team?.TeamRoute == _databaseFixture.TestData.TeamWithFullDetails.TeamRoute)
                                                                 .SelectMany(mi => mi.BowlingFigures)
                                                                 .Any(bf => bf.Bowler?.Player?.PlayerId == x.PlayerId));

            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result?.Player?.PlayerId == player.PlayerId));
            }
        }

        [Fact]
        public async Task Read_most_innings_with_bowling_supports_filter_by_match_location_id()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                MatchLocation = _databaseFixture.TestData.MatchLocations.First()
            };
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND MatchLocationId = @MatchLocationId", new Dictionary<string, object> { { "MatchLocationId", _databaseFixture.TestData.MatchLocations.First().MatchLocationId! } }));
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var results = await dataSource.ReadMostInningsWithBowling(It.IsAny<StatisticsFilter>()).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                     .Where(x => x.MatchLocation?.MatchLocationId == _databaseFixture.TestData.MatchLocations.First().MatchLocationId)
                                                     .SelectMany(m => m.MatchInnings)
                                                     .SelectMany(mi => mi.BowlingFigures)
                                                     .Any(bf => bf.Bowler?.Player?.PlayerId == x.PlayerId));

            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result?.Player?.PlayerId == player.PlayerId));
            }
        }

        [Fact]
        public async Task Read_most_innings_with_bowling_supports_filter_by_competition_id()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                Competition = _databaseFixture.TestData.Competitions.First()
            };
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND CompetitionId = @CompetitionId", new Dictionary<string, object> { { "CompetitionId", _databaseFixture.TestData.Competitions.First().CompetitionId! } }));
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var results = await dataSource.ReadMostInningsWithBowling(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                     .Where(x => x.Season?.Competition?.CompetitionId == _databaseFixture.TestData.Competitions.First().CompetitionId)
                                                     .SelectMany(m => m.MatchInnings)
                                                     .SelectMany(mi => mi.BowlingFigures)
                                                     .Any(bf => bf.Bowler?.Player?.PlayerId == x.PlayerId));

            var monkey = expected.Count() == results.Count();
            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result?.Player?.PlayerId == player.PlayerId));
            }
        }

        [Fact]
        public async Task Read_most_innings_with_bowling_supports_filter_by_season_id()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                Season = _databaseFixture.TestData.Competitions.First().Seasons.First()
            };
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND SeasonId = @SeasonId", new Dictionary<string, object> { { "SeasonId", _databaseFixture.TestData.Competitions.First().Seasons.First().SeasonId! } }));
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var results = await dataSource.ReadMostInningsWithBowling(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                     .Where(x => x.Season?.SeasonId == _databaseFixture.TestData.Competitions.First().Seasons.First().SeasonId)
                                                     .SelectMany(m => m.MatchInnings)
                                                     .SelectMany(mi => mi.BowlingFigures)
                                                     .Any(bf => bf.Bowler?.Player?.PlayerId == x.PlayerId));

            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result?.Player?.PlayerId == player.PlayerId));
            }
        }


        [Fact]
        public async Task Read_most_innings_with_bowling_supports_filter_by_date()
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
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate", new Dictionary<string, object> { { "FromDate", filter.FromDate }, { "UntilDate", filter.UntilDate } }));
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var results = await dataSource.ReadMostInningsWithBowling(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                     .Where(x => x.StartTime >= filter.FromDate && x.StartTime <= filter.UntilDate)
                                                     .SelectMany(m => m.MatchInnings)
                                                     .SelectMany(mi => mi.BowlingFigures)
                                                     .Any(bf => bf.Bowler?.Player?.PlayerId == x.PlayerId));

            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result?.Player?.PlayerId == player.PlayerId));
            }
        }


        [Fact]
        public async Task Read_most_innings_with_bowling_supports_filter_by_minimum_wickets_taken()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                MinimumWicketsTaken = 5
            };
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND Wickets >= @MinimumWicketsTaken", new Dictionary<string, object> { { "MinimumWicketsTaken", filter.MinimumWicketsTaken } }));
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var results = await dataSource.ReadMostInningsWithBowling(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                     .SelectMany(m => m.MatchInnings)
                                                     .SelectMany(mi => mi.BowlingFigures)
                                                     .Any(bf => bf.Bowler?.Player?.PlayerId == x.PlayerId && bf.Wickets >= filter.MinimumWicketsTaken));

            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result?.Player?.PlayerId == player.PlayerId));
            }
        }

        [Fact]
        public async Task Read_most_innings_with_bowling_sorts_by_highest_total_first()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var results = await dataSource.ReadMostInningsWithBowling(filter).ConfigureAwait(false);

            Assert.True(results.Count() > 0);
            int? previousTotal = int.MaxValue;
            foreach (var result in results)
            {
                Assert.True(result.Result?.Total <= previousTotal);
                previousTotal = result.Result?.Total;
            }
        }

        [Fact]
        public async Task Read_most_innings_with_bowling_pages_results()
        {
            const int pageSize = 10;
            var pageNumber = 1;

            var remaining = _databaseFixture.TestData.Matches
                            .SelectMany(x => x.MatchInnings)
                            .SelectMany(x => x.BowlingFigures)
                            .Select(x => x.Bowler?.Player?.PlayerId)
                            .Distinct()
                            .Count();

            Assert.True(remaining > 0);
            while (remaining > 0)
            {
                var filter = new StatisticsFilter { Paging = new Paging { PageNumber = pageNumber, PageSize = pageSize } };
                _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
                _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
                var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);
                var results = await dataSource.ReadMostInningsWithBowling(filter).ConfigureAwait(false);

                var expected = pageSize > remaining ? remaining : pageSize;
                Assert.Equal(expected, results.Count());

                pageNumber++;
                remaining -= pageSize;
            }
        }
    }
}
