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
    public class ReadMostPlayerOfTheMatchAwardsTests
    {
        private readonly SqlServerTestDataFixture _databaseFixture;
        private readonly Mock<IStatisticsQueryBuilder> _queryBuilder = new();
        private readonly Mock<IPlayerDataSource> _playerDataSource = new();

        public ReadMostPlayerOfTheMatchAwardsTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_total_players_with_player_of_the_match_awards_supports_no_filter()
        {
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var result = await dataSource.ReadTotalPlayersWithPlayerOfTheMatchAwards(null).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches
                .SelectMany(x => x.Awards.Where(x => !string.IsNullOrEmpty(x.Award?.AwardName) && x.Award.AwardName.Equals(StatisticsConstants.PLAYER_OF_THE_MATCH_AWARD, StringComparison.OrdinalIgnoreCase)))
                .Select(x => x.PlayerIdentity?.Player?.PlayerId)
                .Distinct()
                .Count();
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_players_with_player_of_the_match_awards_supports_filter_by_club_id()
        {
            var filter = new StatisticsFilter { Club = _databaseFixture.TestData.TeamWithFullDetails!.Club };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND ClubId = @ClubId", new Dictionary<string, object> { { "ClubId", _databaseFixture.TestData.TeamWithFullDetails.Club!.ClubId! } }));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var result = await dataSource.ReadTotalPlayersWithPlayerOfTheMatchAwards(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches
              .Where(x => x.Teams.Select(t => t.Team?.TeamId).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamId))
              .SelectMany(x => x.Awards.Where(x => !string.IsNullOrEmpty(x.Award?.AwardName) && x.Award.AwardName.Equals(StatisticsConstants.PLAYER_OF_THE_MATCH_AWARD, StringComparison.OrdinalIgnoreCase) && x.PlayerIdentity?.Team?.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId))
              .Select(x => x.PlayerIdentity?.Player?.PlayerId)
              .Distinct()
              .Count();

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_players_with_player_of_the_match_awards_supports_filter_by_team_id()
        {
            var filter = new StatisticsFilter { Team = _databaseFixture.TestData.TeamWithFullDetails };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND TeamId = @TeamId", new Dictionary<string, object> { { "TeamId", _databaseFixture.TestData.TeamWithFullDetails!.TeamId! } }));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var result = await dataSource.ReadTotalPlayersWithPlayerOfTheMatchAwards(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches
              .Where(x => x.Teams.Select(t => t.Team?.TeamId).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamId))
              .SelectMany(x => x.Awards.Where(x => !string.IsNullOrEmpty(x.Award?.AwardName) && x.Award.AwardName.Equals(StatisticsConstants.PLAYER_OF_THE_MATCH_AWARD, StringComparison.OrdinalIgnoreCase) && x.PlayerIdentity?.Team?.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId))
              .Select(x => x.PlayerIdentity?.Player?.PlayerId)
              .Distinct()
              .Count();

            Assert.Equal(expected, result);
        }


        [Fact]
        public async Task Read_total_players_with_player_of_the_match_awards_supports_filter_by_team_route()
        {
            var filter = new StatisticsFilter { Team = _databaseFixture.TestData.TeamWithFullDetails };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND TeamRoute = @TeamRoute", new Dictionary<string, object> { { "TeamRoute", _databaseFixture.TestData.TeamWithFullDetails!.TeamRoute! } }));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var result = await dataSource.ReadTotalPlayersWithPlayerOfTheMatchAwards(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches
              .Where(x => x.Teams.Select(t => t.Team?.TeamRoute).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamRoute))
              .SelectMany(x => x.Awards.Where(x => !string.IsNullOrEmpty(x.Award?.AwardName) && x.Award.AwardName.Equals(StatisticsConstants.PLAYER_OF_THE_MATCH_AWARD, StringComparison.OrdinalIgnoreCase) && x.PlayerIdentity?.Team?.TeamRoute == _databaseFixture.TestData.TeamWithFullDetails.TeamRoute))
              .Select(x => x.PlayerIdentity?.Player?.PlayerId)
              .Distinct()
              .Count();

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_players_with_player_of_the_match_awards_supports_filter_by_match_location_id()
        {
            var filter = new StatisticsFilter { MatchLocation = _databaseFixture.TestData.MatchLocations.First() };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND MatchLocationId = @MatchLocationId", new Dictionary<string, object> { { "MatchLocationId", _databaseFixture.TestData.MatchLocations.First().MatchLocationId! } }));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var result = await dataSource.ReadTotalPlayersWithPlayerOfTheMatchAwards(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches
                .Where(x => x.MatchLocation?.MatchLocationId == _databaseFixture.TestData.MatchLocations.First().MatchLocationId)
                .SelectMany(x => x.Awards.Where(x => !string.IsNullOrEmpty(x.Award?.AwardName) && x.Award.AwardName.Equals(StatisticsConstants.PLAYER_OF_THE_MATCH_AWARD, StringComparison.OrdinalIgnoreCase)))
                .Select(x => x.PlayerIdentity?.Player?.PlayerId)
                .Distinct()
                .Count();
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_players_with_player_of_the_match_awards_supports_filter_by_competition_id()
        {
            var filter = new StatisticsFilter { Competition = _databaseFixture.TestData.Competitions.First() };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND CompetitionId = @CompetitionId", new Dictionary<string, object> { { "CompetitionId", _databaseFixture.TestData.Competitions.First().CompetitionId! } }));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var result = await dataSource.ReadTotalPlayersWithPlayerOfTheMatchAwards(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches
                .Where(x => x.Season?.Competition?.CompetitionId == _databaseFixture.TestData.Competitions.First().CompetitionId)
                .SelectMany(x => x.Awards.Where(x => !string.IsNullOrEmpty(x.Award?.AwardName) && x.Award.AwardName.Equals(StatisticsConstants.PLAYER_OF_THE_MATCH_AWARD, StringComparison.OrdinalIgnoreCase)))
                .Select(x => x.PlayerIdentity?.Player?.PlayerId)
                .Distinct()
                .Count();
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_players_with_player_of_the_match_awards_supports_filter_by_season_id()
        {
            var filter = new StatisticsFilter { Season = _databaseFixture.TestData.Competitions.First().Seasons.First() };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND SeasonId = @SeasonId", new Dictionary<string, object> { { "SeasonId", _databaseFixture.TestData.Competitions.First().Seasons.First().SeasonId! } }));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var result = await dataSource.ReadTotalPlayersWithPlayerOfTheMatchAwards(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches
                .Where(x => x.Season?.SeasonId == _databaseFixture.TestData.Competitions.First().Seasons.First().SeasonId)
                .SelectMany(x => x.Awards.Where(x => !string.IsNullOrEmpty(x.Award?.AwardName) && x.Award.AwardName.Equals(StatisticsConstants.PLAYER_OF_THE_MATCH_AWARD, StringComparison.OrdinalIgnoreCase)))
                .Select(x => x.PlayerIdentity?.Player?.PlayerId)
                .Distinct()
                .Count();
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_players_with_player_of_the_match_awards_supports_filter_by_date()
        {
            var dateRangeGenerator = new DateRangeGenerator();
            var (fromDate, untilDate) = dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);

            var filter = new StatisticsFilter { FromDate = fromDate, UntilDate = untilDate };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate", new Dictionary<string, object> { { "FromDate", filter.FromDate }, { "UntilDate", filter.UntilDate } }));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var result = await dataSource.ReadTotalPlayersWithPlayerOfTheMatchAwards(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches
                .Where(x => x.StartTime >= filter.FromDate && x.StartTime <= filter.UntilDate)
                .SelectMany(x => x.Awards.Where(x => !string.IsNullOrEmpty(x.Award?.AwardName) && x.Award.AwardName.Equals(StatisticsConstants.PLAYER_OF_THE_MATCH_AWARD, StringComparison.OrdinalIgnoreCase)))
                .Select(x => x.PlayerIdentity?.Player?.PlayerId)
                .Distinct()
                .Count();
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_most_player_of_the_match_awards_returns_player()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var results = await dataSource.ReadMostPlayerOfTheMatchAwards(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.SelectMany(x => x.Awards.Where(x => !string.IsNullOrEmpty(x.Award?.AwardName) && x.Award.AwardName.Equals(StatisticsConstants.PLAYER_OF_THE_MATCH_AWARD, StringComparison.OrdinalIgnoreCase)))
                                                            .Select(x => x.PlayerIdentity?.Player)
                                                            .Distinct(new PlayerEqualityComparer());
            foreach (var player in expected)
            {
                var result = results.SingleOrDefault(x => x.Result?.Player?.PlayerId == player?.PlayerId);
                Assert.NotNull(result);

                Assert.Equal(player.PlayerRoute, result!.Result?.Player?.PlayerRoute);
                Assert.Equal(player.PlayerName(), result.Result?.Player?.PlayerName());
            }
        }

        [Fact]
        public async Task Read_most_player_of_the_match_awards_returns_teams()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var results = await dataSource.ReadMostPlayerOfTheMatchAwards(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.SelectMany(x => x.Awards.Where(x => !string.IsNullOrEmpty(x.Award?.AwardName) && x.Award.AwardName.Equals(StatisticsConstants.PLAYER_OF_THE_MATCH_AWARD, StringComparison.OrdinalIgnoreCase)))
                                                                   .Select(x => x.PlayerIdentity?.Player)
                                                                   .Distinct(new PlayerEqualityComparer());
            foreach (var player in expected)
            {
                var result = results.SingleOrDefault(x => x.Result?.Player?.PlayerId == player?.PlayerId);
                Assert.NotNull(result);

                foreach (var identity in player!.PlayerIdentities)
                {
                    var resultIdentity = result!.Result?.Player?.PlayerIdentities.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId);
                    Assert.NotNull(resultIdentity);
                    Assert.Equal(identity.Team?.TeamName, resultIdentity!.Team?.TeamName);
                }
            }
        }

        [Fact]
        public async Task Read_most_player_of_the_match_awards_returns_statistics()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            await ActAndAssertStatistics(filter, dataSource, x => true, x => true, x => true, x => true).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_most_player_of_the_match_awards_supports_statistics_filtered_by_club_id()
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

            await ActAndAssertStatistics(filter, dataSource, x => x.Teams.Select(x => x.Team?.TeamId).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamId),
                i => i.BowlingTeam?.Team?.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId,
                i => i.BattingTeam?.Team?.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId,
                pi => pi.Team?.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_most_player_of_the_match_awards_supports_statistics_filtered_by_team_id()
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

            await ActAndAssertStatistics(filter, dataSource, x => x.Teams.Select(x => x.Team?.TeamId).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamId),
                i => i.BowlingTeam?.Team?.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId,
                i => i.BattingTeam?.Team?.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId,
                pi => pi.Team?.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_most_player_of_the_match_awards_supports_statistics_filtered_by_team_route()
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

            await ActAndAssertStatistics(filter, dataSource, x => x.Teams.Select(x => x.Team?.TeamId).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamId),
                i => i.BowlingTeam?.Team?.TeamRoute == _databaseFixture.TestData.TeamWithFullDetails.TeamRoute,
                i => i.BattingTeam?.Team?.TeamRoute == _databaseFixture.TestData.TeamWithFullDetails.TeamRoute,
                pi => pi.Team?.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_most_player_of_the_match_awards_supports_statistics_filtered_by_match_location_id()
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

            await ActAndAssertStatistics(filter, dataSource, x => x.MatchLocation?.MatchLocationId == filter.MatchLocation.MatchLocationId, x => true, x => true, x => true).ConfigureAwait(false);
        }


        [Fact]
        public async Task Read_most_player_of_the_match_awards_supports_statistics_filtered_by_competition_id()
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

            await ActAndAssertStatistics(filter, dataSource, x => x.Season?.Competition?.CompetitionId == filter.Competition.CompetitionId, x => true, x => true, x => true).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_most_player_of_the_match_awards_supports_statistics_filtered_by_season_id()
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

            await ActAndAssertStatistics(filter, dataSource, x => x.Season?.SeasonId == filter.Season.SeasonId, x => true, x => true, x => true).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_most_player_of_the_match_awards_supports_statistics_filtered_by_date()
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

            await ActAndAssertStatistics(filter, dataSource, x => x.StartTime >= filter.FromDate && x.StartTime <= filter.UntilDate, x => true, x => true, x => true).ConfigureAwait(false);
        }

        private async Task ActAndAssertStatistics(StatisticsFilter filter, SqlServerBestPlayerTotalStatisticsDataSource dataSource, Func<Stoolball.Matches.Match, bool> matchFilter, Func<MatchInnings, bool> bowlingInningsFilter, Func<MatchInnings, bool> battingInningsFilter, Func<PlayerIdentity, bool> playerIdentityFilter)
        {
            var results = await dataSource.ReadMostPlayerOfTheMatchAwards(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Players.Select(p => new BestStatistic
            {
                Player = p,
                TotalMatches = (int)_databaseFixture.TestData.Matches
                            .Where(matchFilter)
                            .Count(m => m.MatchInnings.Where(battingInningsFilter).Any(mi => mi.PlayerInnings.Any(pi => pi.Batter?.Player?.PlayerId == p.PlayerId))
                                    || m.MatchInnings.Where(bowlingInningsFilter).Any(mi =>
                                        mi.PlayerInnings.Any(pi => pi.DismissedBy?.Player?.PlayerId == p.PlayerId || pi.Bowler?.Player?.PlayerId == p.PlayerId) ||
                                        mi.OversBowled.Any(o => o.Bowler?.Player?.PlayerId == p.PlayerId) ||
                                        mi.BowlingFigures.Any(bf => bf.Bowler?.Player?.PlayerId == p.PlayerId)
                                    ) || m.Awards.Any(aw => aw.PlayerIdentity?.Player?.PlayerId == p.PlayerId)),
                Total = (int)_databaseFixture.TestData.Matches
                            .Where(matchFilter)
                            .SelectMany(x => x.Awards.Where(x => !string.IsNullOrEmpty(x.Award?.AwardName) && x.Award.AwardName.Equals(StatisticsConstants.PLAYER_OF_THE_MATCH_AWARD, StringComparison.OrdinalIgnoreCase) &&
                                             playerIdentityFilter(x.PlayerIdentity!) &&
                                             x.PlayerIdentity!.Player?.PlayerId == p.PlayerId))
                            .Count()
            }).Where(x => x.Total > 0);

            foreach (var player in expected)
            {
                var result = results.SingleOrDefault(x => x.Result?.Player?.PlayerId == player.Player?.PlayerId);
                Assert.NotNull(result);

                Assert.Equal(player.TotalMatches, result!.Result?.TotalMatches);
                Assert.Equal(player.Total, result.Result?.Total);
            }
        }

        [Fact]
        public async Task Read_most_player_of_the_match_awards_supports_no_filter()
        {
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var results = await dataSource.ReadMostPlayerOfTheMatchAwards(null).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.SelectMany(x => x.Awards.Where(x => !string.IsNullOrEmpty(x.Award?.AwardName) && x.Award.AwardName.Equals(StatisticsConstants.PLAYER_OF_THE_MATCH_AWARD, StringComparison.OrdinalIgnoreCase)))
                                                            .Select(x => x.PlayerIdentity.Player)
                                                            .Distinct(new PlayerEqualityComparer());

            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.Player.PlayerId == player!.PlayerId));
            }
        }

        [Fact]
        public async Task Read_most_player_of_the_match_awards_supports_filter_by_club_id()
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

            var results = (await dataSource.ReadMostPlayerOfTheMatchAwards(filter).ConfigureAwait(false)).ToList();

            var expected = _databaseFixture.TestData.Matches.Where(x => x.Teams.Select(t => t.Team.TeamId).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamId))
                                .SelectMany(x => x.Awards.Where(x => !string.IsNullOrEmpty(x.Award?.AwardName) && x.Award.AwardName.Equals(StatisticsConstants.PLAYER_OF_THE_MATCH_AWARD, StringComparison.OrdinalIgnoreCase)))
                                .Where(x => x.PlayerIdentity.Team.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId)
                                .Select(x => x.PlayerIdentity.Player)
                                .Distinct(new PlayerEqualityComparer())
                                .ToList();

            Assert.Equal(expected.Count, results.Count);
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.Player.PlayerId == player!.PlayerId));
            }
        }

        [Fact]
        public async Task Read_most_player_of_the_match_awards_supports_filter_by_team_id()
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

            var results = (await dataSource.ReadMostPlayerOfTheMatchAwards(filter).ConfigureAwait(false)).ToList();

            var expected = _databaseFixture.TestData.Matches.Where(x => x.Teams.Select(t => t.Team.TeamId).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamId))
                                .SelectMany(x => x.Awards.Where(x => !string.IsNullOrEmpty(x.Award?.AwardName) && x.Award.AwardName.Equals(StatisticsConstants.PLAYER_OF_THE_MATCH_AWARD, StringComparison.OrdinalIgnoreCase)))
                                .Where(x => x.PlayerIdentity.Team.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId)
                                .Select(x => x.PlayerIdentity.Player)
                                .Distinct(new PlayerEqualityComparer())
                                .ToList();

            Assert.Equal(expected.Count, results.Count);
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.Player.PlayerId == player!.PlayerId));
            }
        }


        [Fact]
        public async Task Read_most_player_of_the_match_awards_supports_filter_by_team_route()
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

            var results = (await dataSource.ReadMostPlayerOfTheMatchAwards(filter).ConfigureAwait(false)).ToList();

            var expected = _databaseFixture.TestData.Matches.Where(x => x.Teams.Select(t => t.Team.TeamRoute).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamRoute))
                                .SelectMany(x => x.Awards.Where(x => !string.IsNullOrEmpty(x.Award?.AwardName) && x.Award.AwardName.Equals(StatisticsConstants.PLAYER_OF_THE_MATCH_AWARD, StringComparison.OrdinalIgnoreCase)))
                                .Where(x => x.PlayerIdentity.Team.TeamRoute == _databaseFixture.TestData.TeamWithFullDetails.TeamRoute)
                                .Select(x => x.PlayerIdentity.Player)
                                .Distinct(new PlayerEqualityComparer())
                                .ToList();

            Assert.Equal(expected.Count, results.Count);
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.Player.PlayerId == player!.PlayerId));
            }
        }

        [Fact]
        public async Task Read_most_player_of_the_match_awards_supports_filter_by_match_location_id()
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

            var results = await dataSource.ReadMostPlayerOfTheMatchAwards(It.IsAny<StatisticsFilter>()).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.Where(x => x.MatchLocation?.MatchLocationId == filter.MatchLocation.MatchLocationId)
                                .SelectMany(x => x.Awards.Where(x => !string.IsNullOrEmpty(x.Award?.AwardName) && x.Award.AwardName.Equals(StatisticsConstants.PLAYER_OF_THE_MATCH_AWARD, StringComparison.OrdinalIgnoreCase)))
                                .Select(x => x.PlayerIdentity.Player)
                                .Distinct(new PlayerEqualityComparer());

            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.Player.PlayerId == player!.PlayerId));
            }
        }

        [Fact]
        public async Task Read_most_player_of_the_match_awards_supports_filter_by_competition_id()
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

            var results = await dataSource.ReadMostPlayerOfTheMatchAwards(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.Where(x => x.Season?.Competition?.CompetitionId == _databaseFixture.TestData.Competitions.First().CompetitionId)
                                                            .SelectMany(x => x.Awards.Where(x => !string.IsNullOrEmpty(x.Award?.AwardName) && x.Award.AwardName.Equals(StatisticsConstants.PLAYER_OF_THE_MATCH_AWARD, StringComparison.OrdinalIgnoreCase)))
                                                            .Select(x => x.PlayerIdentity.Player)
                                                            .Distinct(new PlayerEqualityComparer());

            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.Player.PlayerId == player!.PlayerId));
            }
        }

        [Fact]
        public async Task Read_most_player_of_the_match_awards_supports_filter_by_season_id()
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

            var results = await dataSource.ReadMostPlayerOfTheMatchAwards(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.Where(x => x.Season?.SeasonId == _databaseFixture.TestData.Competitions.First().Seasons.First().SeasonId)
                                                .SelectMany(x => x.Awards.Where(x => !string.IsNullOrEmpty(x.Award?.AwardName) && x.Award.AwardName.Equals(StatisticsConstants.PLAYER_OF_THE_MATCH_AWARD, StringComparison.OrdinalIgnoreCase)))
                                                .Select(x => x.PlayerIdentity.Player)
                                                .Distinct(new PlayerEqualityComparer());

            Assert.Equal(expected.Count(), results.Count());
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.Player.PlayerId == player!.PlayerId));
            }
        }

        [Fact]
        public async Task Read_most_player_of_the_match_awards_sorts_by_highest_total_first()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);

            var results = await dataSource.ReadMostPlayerOfTheMatchAwards(filter).ConfigureAwait(false);

            int? previousTotal = int.MaxValue;
            foreach (var result in results)
            {
                Assert.True(result.Result?.Total <= previousTotal);
                previousTotal = result.Result?.Total;
            }
        }

        [Fact]
        public async Task Read_most_player_of_the_match_awards_pages_results()
        {
            const int pageSize = 10;
            var pageNumber = 1;

            var remaining = _databaseFixture.TestData.Matches
                            .SelectMany(x => x.Awards.Where(x => !string.IsNullOrEmpty(x.Award?.AwardName) && x.Award.AwardName.Equals(StatisticsConstants.PLAYER_OF_THE_MATCH_AWARD, StringComparison.OrdinalIgnoreCase)))
                            .Select(x => x.PlayerIdentity?.Player?.PlayerId)
                            .Distinct()
                            .Count();
            while (remaining > 0)
            {
                var filter = new StatisticsFilter { Paging = new Paging { PageNumber = pageNumber, PageSize = pageSize } };
                _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
                _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));
                var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);
                var results = await dataSource.ReadMostPlayerOfTheMatchAwards(filter).ConfigureAwait(false);

                var expected = pageSize > remaining ? remaining : pageSize;
                Assert.Equal(expected, results.Count());

                pageNumber++;
                remaining -= pageSize;
            }
        }
    }
}
