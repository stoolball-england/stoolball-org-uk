﻿using System;
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
    public class ReadMostCatchesTests
    {
        private readonly SqlServerTestDataFixture _databaseFixture;
        private readonly Mock<IStatisticsQueryBuilder> _queryBuilder = new();
        private readonly Mock<IPlayerDataSource> _playerDataSource = new();
        private readonly TestDataQueryHelper _queryHelper = new();

        public ReadMostCatchesTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_total_players_with_catches_supports_no_filter()
        {
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var result = await dataSource.ReadTotalPlayersWithCatches(null).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches
                .SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.PlayerInnings)
                .Where(x => (x.DismissalType == DismissalType.Caught && x.DismissedBy != null) || (x.DismissalType == DismissalType.CaughtAndBowled && x.Bowler != null))
                .Select(x => x.DismissalType == DismissalType.Caught ? x.DismissedBy.Player.PlayerId : x.Bowler.Player.PlayerId)
                .Distinct()
                .Count();
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_players_with_catches_supports_filter_by_club_id()
        {
            var filter = new StatisticsFilter { Club = _databaseFixture.TestData.TeamWithFullDetails!.Club };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND ClubId = @ClubId", new Dictionary<string, object> { { "ClubId", _databaseFixture.TestData.TeamWithFullDetails.Club.ClubId! } }));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var result = await dataSource.ReadTotalPlayersWithCatches(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches
                .Where(x => x.Teams.Select(t => t.Team.TeamId).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamId))
                .SelectMany(x => x.MatchInnings)
                .Where(i => i.BowlingTeam?.Team?.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId)
                .SelectMany(x => x.PlayerInnings)
                .Where(x => (x.DismissalType == DismissalType.Caught && x.DismissedBy != null) || (x.DismissalType == DismissalType.CaughtAndBowled && x.Bowler != null))
                .Select(x => x.DismissalType == DismissalType.Caught ? x.DismissedBy.Player.PlayerId : x.Bowler.Player.PlayerId)
                .Distinct()
                .Count();
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_players_with_catches_supports_filter_by_team_id()
        {
            var filter = new StatisticsFilter { Team = _databaseFixture.TestData.TeamWithFullDetails };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND TeamId = @TeamId", new Dictionary<string, object> { { "TeamId", _databaseFixture.TestData.TeamWithFullDetails!.TeamId! } }));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var result = await dataSource.ReadTotalPlayersWithCatches(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches
                .Where(x => x.Teams.Select(t => t.Team.TeamId).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamId))
                .SelectMany(x => x.MatchInnings)
                .Where(i => i.BowlingTeam?.Team?.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId)
                .SelectMany(x => x.PlayerInnings)
                .Where(x => (x.DismissalType == DismissalType.Caught && x.DismissedBy != null) || (x.DismissalType == DismissalType.CaughtAndBowled && x.Bowler != null))
                .Select(x => x.DismissalType == DismissalType.Caught ? x.DismissedBy.Player.PlayerId : x.Bowler.Player.PlayerId)
                .Distinct()
                .Count();
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_players_with_catches_supports_filter_by_team_route()
        {
            var filter = new StatisticsFilter { Team = _databaseFixture.TestData.TeamWithFullDetails };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND TeamRoute = @TeamRoute", new Dictionary<string, object> { { "TeamRoute", _databaseFixture.TestData.TeamWithFullDetails!.TeamRoute! } }));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var result = await dataSource.ReadTotalPlayersWithCatches(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches
                .Where(x => x.Teams.Select(t => t.Team.TeamRoute).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamRoute))
                .SelectMany(x => x.MatchInnings)
                .Where(i => i.BowlingTeam?.Team?.TeamRoute == _databaseFixture.TestData.TeamWithFullDetails.TeamRoute)
                .SelectMany(x => x.PlayerInnings)
                .Where(x => (x.DismissalType == DismissalType.Caught && x.DismissedBy != null) || (x.DismissalType == DismissalType.CaughtAndBowled && x.Bowler != null))
                .Select(x => x.DismissalType == DismissalType.Caught ? x.DismissedBy.Player.PlayerId : x.Bowler.Player.PlayerId)
                .Distinct()
                .Count();
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_players_with_catches_supports_filter_by_match_location_id()
        {
            var filter = new StatisticsFilter { MatchLocation = _databaseFixture.TestData.MatchLocations.First() };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND MatchLocationId = @MatchLocationId", new Dictionary<string, object> { { "MatchLocationId", _databaseFixture.TestData.MatchLocations.First().MatchLocationId! } }));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var result = await dataSource.ReadTotalPlayersWithCatches(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches
                .Where(x => x.MatchLocation?.MatchLocationId == _databaseFixture.TestData.MatchLocations.First().MatchLocationId)
                .SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.PlayerInnings)
                .Where(x => (x.DismissalType == DismissalType.Caught && x.DismissedBy != null) || (x.DismissalType == DismissalType.CaughtAndBowled && x.Bowler != null))
                .Select(x => x.DismissalType == DismissalType.Caught ? x.DismissedBy.Player.PlayerId : x.Bowler.Player.PlayerId)
                .Distinct()
                .Count();
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_players_with_catches_supports_filter_by_competition_id()
        {
            var filter = new StatisticsFilter { Competition = _databaseFixture.TestData.Competitions.First() };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND CompetitionId = @CompetitionId", new Dictionary<string, object> { { "CompetitionId", _databaseFixture.TestData.Competitions.First().CompetitionId! } }));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var result = await dataSource.ReadTotalPlayersWithCatches(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches
                .Where(x => x.Season?.Competition?.CompetitionId == _databaseFixture.TestData.Competitions.First().CompetitionId)
                .SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.PlayerInnings)
                .Where(x => (x.DismissalType == DismissalType.Caught && x.DismissedBy != null) || (x.DismissalType == DismissalType.CaughtAndBowled && x.Bowler != null))
                .Select(x => x.DismissalType == DismissalType.Caught ? x.DismissedBy.Player.PlayerId : x.Bowler.Player.PlayerId)
                .Distinct()
                .Count();
            Assert.Equal(expected, result);
        }


        [Fact]
        public async Task Read_total_players_with_catches_supports_filter_by_season_id()
        {
            var filter = new StatisticsFilter { Season = _databaseFixture.TestData.Competitions.First().Seasons.First() };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND SeasonId = @SeasonId", new Dictionary<string, object> { { "SeasonId", _databaseFixture.TestData.Competitions.First().Seasons.First().SeasonId! } }));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var result = await dataSource.ReadTotalPlayersWithCatches(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches
                .Where(x => x.Season?.SeasonId == _databaseFixture.TestData.Competitions.First().Seasons.First().SeasonId)
                .SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.PlayerInnings)
                .Where(x => (x.DismissalType == DismissalType.Caught && x.DismissedBy != null) || (x.DismissalType == DismissalType.CaughtAndBowled && x.Bowler != null))
                .Select(x => x.DismissalType == DismissalType.Caught ? x.DismissedBy.Player.PlayerId : x.Bowler.Player.PlayerId)
                .Distinct()
                .Count();
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_players_with_catches_supports_filter_by_date()
        {
            var dateRangeGenerator = new DateRangeGenerator();
            var (fromDate, untilDate) = dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);

            var filter = new StatisticsFilter { FromDate = fromDate, UntilDate = untilDate };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate", new Dictionary<string, object> { { "FromDate", filter.FromDate }, { "UntilDate", filter.UntilDate } }));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var result = await dataSource.ReadTotalPlayersWithCatches(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches
                .Where(x => x.StartTime >= filter.FromDate && x.StartTime <= filter.UntilDate)
                .SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.PlayerInnings)
                .Where(x => (x.DismissalType == DismissalType.Caught && x.DismissedBy != null) || (x.DismissalType == DismissalType.CaughtAndBowled && x.Bowler != null))
                .Select(x => x.DismissalType == DismissalType.Caught ? x.DismissedBy.Player.PlayerId : x.Bowler.Player.PlayerId)
                .Distinct()
                .Count();
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_most_catches_returns_player()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var results = await dataSource.ReadMostCatches(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                                            .SelectMany(m => m.MatchInnings)
                                                                            .SelectMany(x => x.PlayerInnings)
                                                                            .Where(pi => (pi.DismissalType == DismissalType.Caught && pi.DismissedBy?.Player.PlayerId == x.PlayerId) ||
                                                                                         (pi.DismissalType == DismissalType.CaughtAndBowled && pi.Bowler?.Player.PlayerId == x.PlayerId))
                                                                            .Any());
            foreach (var player in expected)
            {
                var result = results.SingleOrDefault(x => x.Result.Player.PlayerId == player.PlayerId);
                Assert.NotNull(result);

                Assert.Equal(player.PlayerRoute, result!.Result.Player.PlayerRoute);
                Assert.Equal(player.PlayerName(), result.Result.Player.PlayerName());
            }
        }

        [Fact]
        public async Task Read_most_catches_returns_teams()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var results = await dataSource.ReadMostCatches(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                                            .SelectMany(m => m.MatchInnings)
                                                                            .SelectMany(x => x.PlayerInnings)
                                                                            .Where(pi => (pi.DismissalType == DismissalType.Caught && pi.DismissedBy?.Player.PlayerId == x.PlayerId) ||
                                                                                         (pi.DismissalType == DismissalType.CaughtAndBowled && pi.Bowler?.Player.PlayerId == x.PlayerId))
                                                                            .Any());
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
        public async Task Read_most_catches_returns_statistics()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            await ActAndAssertStatistics(filter, dataSource, x => true, x => true, x => true, x => true).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_most_catches_supports_statistics_filtered_by_club_id()
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
                i => teamIdsForClub.Contains(i.BowlingTeam?.Team?.TeamId),
                i => teamIdsForClub.Contains(i.BattingTeam?.Team?.TeamId),
                aw => teamIdsForClub.Contains(aw.PlayerIdentity?.Team?.TeamId)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_most_catches_supports_statistics_filtered_by_team_id()
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
                i => i.BowlingTeam?.Team?.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId,
                i => i.BattingTeam?.Team?.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId,
                aw => aw.PlayerIdentity?.Team?.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_most_catches_supports_statistics_filtered_by_team_route()
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
                i => i.BowlingTeam?.Team?.TeamRoute == _databaseFixture.TestData.TeamWithFullDetails.TeamRoute,
                i => i.BattingTeam?.Team?.TeamRoute == _databaseFixture.TestData.TeamWithFullDetails.TeamRoute,
                aw => aw.PlayerIdentity?.Team?.TeamRoute == _databaseFixture.TestData.TeamWithFullDetails.TeamRoute).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_most_catches_supports_statistics_filtered_by_match_location_id()
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
                x => true).ConfigureAwait(false);
        }


        [Fact]
        public async Task Read_most_catches_supports_statistics_filtered_by_competition_id()
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
                x => true).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_most_catches_supports_statistics_filtered_by_season_id()
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
                x => true).ConfigureAwait(false);
        }


        [Fact]
        public async Task Read_most_catches_supports_statistics_filtered_by_date()
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
                x => true).ConfigureAwait(false);
        }

        private async Task ActAndAssertStatistics(StatisticsFilter filter, SqlServerBestPlayerTotalStatisticsDataSource dataSource, Func<Stoolball.Matches.Match, bool> matchFilter, Func<MatchInnings, bool> bowlingInningsFilter, Func<MatchInnings, bool> battingInningsFilter, Func<MatchAward, bool> awardsFilter)
        {
            var results = await dataSource.ReadMostCatches(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Select(p => new BestStatistic
            {
                Player = p,
                TotalMatches = _queryHelper.TotalMatchesForPlayer(p, _databaseFixture.TestData.Matches, matchFilter, bowlingInningsFilter, battingInningsFilter, awardsFilter),
                Total = (int)_databaseFixture.TestData.Matches
                            .Where(matchFilter)
                            .SelectMany(m => m.MatchInnings)
                            .Where(bowlingInningsFilter)
                            .SelectMany(mi => mi.PlayerInnings)
                            .Where(pi => (pi.DismissalType == DismissalType.Caught && pi.DismissedBy?.Player?.PlayerId == p.PlayerId) ||
                                            (pi.DismissalType == DismissalType.CaughtAndBowled && pi.Bowler?.Player?.PlayerId == p.PlayerId))
                            .Count(),
            }).Where(x => x.Total > 0);

            foreach (var player in expected)
            {
                var result = results.SingleOrDefault(x => x.Result?.Player?.PlayerId == player?.Player?.PlayerId);
                Assert.NotNull(result);

                Assert.Equal(player.TotalMatches, result!.Result?.TotalMatches);
                Assert.Null(result.Result?.TotalInnings);
                Assert.Equal(player.Total, result.Result?.Total);
            }
        }

        [Fact]
        public async Task Read_most_catches_supports_no_filter()
        {
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var results = await dataSource.ReadMostCatches(null).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                                   .SelectMany(m => m.MatchInnings)
                                                                   .SelectMany(mi => mi.PlayerInnings)
                                                                   .Where(pi => (pi.DismissalType == DismissalType.Caught && pi.DismissedBy?.Player?.PlayerId == x.PlayerId) ||
                                                                                (pi.DismissalType == DismissalType.CaughtAndBowled && pi.Bowler?.Player?.PlayerId == x.PlayerId))
                                                                   .Any());

            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result?.Player?.PlayerId == player.PlayerId));
            }
        }

        [Fact]
        public async Task Read_most_catches_supports_filter_by_club_id()
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

            var results = await dataSource.ReadMostCatches(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                                    .SelectMany(m => m.MatchInnings)
                                                                    .Where(i => i.BowlingTeam?.Team?.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId)
                                                                    .SelectMany(mi => mi.PlayerInnings)
                                                                    .Where(pi => (pi.DismissalType == DismissalType.Caught && pi.DismissedBy?.Player?.PlayerId == x.PlayerId) ||
                                                                                (pi.DismissalType == DismissalType.CaughtAndBowled && pi.Bowler?.Player?.PlayerId == x.PlayerId))
                                                                    .Any());

            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result?.Player?.PlayerId == player.PlayerId));
            }
        }

        [Fact]
        public async Task Read_most_catches_supports_filter_by_team_id()
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

            var results = await dataSource.ReadMostCatches(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                                    .Where(x => x.Teams.Select(t => t.Team?.TeamId).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamId))
                                                                    .SelectMany(m => m.MatchInnings)
                                                                    .Where(i => i.BowlingTeam?.Team?.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId)
                                                                    .SelectMany(mi => mi.PlayerInnings)
                                                                    .Where(pi => (pi.DismissalType == DismissalType.Caught && pi.DismissedBy?.Player?.PlayerId == x.PlayerId) ||
                                                                                (pi.DismissalType == DismissalType.CaughtAndBowled && pi.Bowler?.Player?.PlayerId == x.PlayerId))
                                                                    .Any());

            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result?.Player?.PlayerId == player.PlayerId));
            }
        }

        [Fact]
        public async Task Read_most_catches_supports_filter_by_match_location_id()
        {
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var foundAtLeastOne = false;
            foreach (var location in _databaseFixture.TestData.MatchLocations)
            {
                var filter = new StatisticsFilter
                {
                    Paging = new Paging
                    {
                        PageSize = int.MaxValue
                    },
                    MatchLocation = location
                };
                _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND MatchLocationId = @MatchLocationId", new Dictionary<string, object> { { "MatchLocationId", filter.MatchLocation.MatchLocationId! } }));

                var results = await dataSource.ReadMostCatches(filter).ConfigureAwait(false);

                var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                                            .Where(x => x.MatchLocation?.MatchLocationId == filter.MatchLocation.MatchLocationId)
                                                                            .SelectMany(m => m.MatchInnings)
                                                                            .SelectMany(mi => mi.PlayerInnings)
                                                                            .Any(pi => (pi.DismissalType == DismissalType.Caught && pi.DismissedBy?.Player?.PlayerId == x.PlayerId) ||
                                                                                        (pi.DismissalType == DismissalType.CaughtAndBowled && pi.Bowler?.Player?.PlayerId == x.PlayerId)));
                foundAtLeastOne = foundAtLeastOne || expected.Any();

                Assert.Equal(expected.Count(), results.Count());
                foreach (var player in expected)
                {
                    Assert.NotNull(results.SingleOrDefault(x => x.Result?.Player?.PlayerId == player.PlayerId));
                }
            }

            Assert.True(foundAtLeastOne);
        }

        [Fact]
        public async Task Read_most_catches_supports_filter_by_competition_id()
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

            var results = await dataSource.ReadMostCatches(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                                        .Where(x => x.Season?.Competition?.CompetitionId == _databaseFixture.TestData.Competitions.First().CompetitionId)
                                                                        .SelectMany(m => m.MatchInnings)
                                                                        .SelectMany(mi => mi.PlayerInnings)
                                                                        .Where(pi => (pi.DismissalType == DismissalType.Caught && pi.DismissedBy?.Player?.PlayerId == x.PlayerId) ||
                                                                                    (pi.DismissalType == DismissalType.CaughtAndBowled && pi.Bowler?.Player?.PlayerId == x.PlayerId))
                                                                        .Any());

            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result?.Player?.PlayerId == player.PlayerId));
            }
        }

        [Fact]
        public async Task Read_most_catches_supports_filter_by_season_id()
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

            var results = await dataSource.ReadMostCatches(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                        .Where(x => x.Season?.SeasonId == _databaseFixture.TestData.Competitions.First().Seasons.First().SeasonId)
                                                        .SelectMany(m => m.MatchInnings)
                                                        .SelectMany(mi => mi.PlayerInnings)
                                                        .Where(pi => (pi.DismissalType == DismissalType.Caught && pi.DismissedBy?.Player?.PlayerId == x.PlayerId) ||
                                                                    (pi.DismissalType == DismissalType.CaughtAndBowled && pi.Bowler?.Player?.PlayerId == x.PlayerId))
                                                        .Any());

            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result?.Player?.PlayerId == player.PlayerId));
            }
        }

        [Fact]
        public async Task Read_most_catches_supports_filter_by_date()
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

            var results = await dataSource.ReadMostCatches(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Where(x => _databaseFixture.TestData.Matches
                                                        .Where(x => x.StartTime >= filter.FromDate && x.StartTime <= filter.UntilDate)
                                                        .SelectMany(m => m.MatchInnings)
                                                        .SelectMany(mi => mi.PlayerInnings)
                                                        .Where(pi => (pi.DismissalType == DismissalType.Caught && pi.DismissedBy?.Player?.PlayerId == x.PlayerId) ||
                                                                    (pi.DismissalType == DismissalType.CaughtAndBowled && pi.Bowler?.Player?.PlayerId == x.PlayerId))
                                                        .Any());

            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result?.Player?.PlayerId == player.PlayerId));
            }
        }

        [Fact]
        public async Task Read_most_catches_sorts_by_highest_total_first()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var results = await dataSource.ReadMostCatches(filter).ConfigureAwait(false);

            int? previousTotal = int.MaxValue;
            foreach (var result in results)
            {
                Assert.True(result.Result?.Total <= previousTotal);
                previousTotal = result.Result?.Total;
            }
        }

        [Fact]
        public async Task Read_most_catches_pages_results()
        {
            const int pageSize = 10;
            var pageNumber = 1;

            var remaining = _databaseFixture.TestData.Matches
                            .SelectMany(x => x.MatchInnings)
                            .SelectMany(x => x.PlayerInnings)
                            .Where(pi => (pi.DismissalType == DismissalType.Caught && pi.DismissedBy != null) ||
                                            (pi.DismissalType == DismissalType.CaughtAndBowled && pi.Bowler != null))
                            .Select(x => x.DismissalType == DismissalType.Caught ? x.DismissedBy?.Player?.PlayerId : x.Bowler?.Player?.PlayerId)
                            .Distinct()
                            .Count();
            while (remaining > 0)
            {
                var filter = new StatisticsFilter { Paging = new Paging { PageNumber = pageNumber, PageSize = pageSize } };
                _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
                _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
                var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);
                var results = await dataSource.ReadMostCatches(filter).ConfigureAwait(false);

                var expected = pageSize > remaining ? remaining : pageSize;
                Assert.Equal(expected, results.Count());

                pageNumber++;
                remaining -= pageSize;
            }
        }

        [Fact]
        public async Task Read_most_catches_returns_results_equal_to_max_with_max_results_filter()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue }, MaxResultsAllowingExtraResultsIfValuesAreEqual = 5 };
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var results = (await dataSource.ReadMostCatches(filter).ConfigureAwait(false)).ToList();

            // The test data is altered to ensure the 5th and 6th results are the same
            Assert.True(results.Count > 5);

            var fifthValue = results[4].Result?.Total;
            for (var i = 4; i < results.Count; i++)
            {
                Assert.Equal(fifthValue, results[i].Result?.Total);
            }
        }
    }
}
