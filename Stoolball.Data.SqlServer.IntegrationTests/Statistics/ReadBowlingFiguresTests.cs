﻿using System;
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
    public class ReadBowlingFiguresTests
    {
        private readonly SqlServerTestDataFixture _databaseFixture;
        private readonly Mock<IStatisticsQueryBuilder> queryBuilder = new();

        public ReadBowlingFiguresTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_total_bowling_figures_supports_no_filter()
        {
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var result = await dataSource.ReadTotalBowlingFigures(null).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.BowlingFigures.Count;
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_bowling_figures_supports_filter_by_player_id()
        {
            var filter = new StatisticsFilter { Player = _databaseFixture.TestData.BowlerWithMultipleIdentities };
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns(("AND PlayerId = @PlayerId", new Dictionary<string, object> { { "PlayerId", _databaseFixture.TestData.BowlerWithMultipleIdentities!.PlayerId! } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var result = await dataSource.ReadTotalBowlingFigures(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.BowlingFigures
                .Count(x => x.Bowler?.Player?.PlayerId == _databaseFixture.TestData.BowlerWithMultipleIdentities.PlayerId);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_bowling_figures_supports_filter_by_club_id()
        {
            var filter = new StatisticsFilter { Club = _databaseFixture.TestData.TeamWithFullDetails!.Club };
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns(("AND ClubId = @ClubId", new Dictionary<string, object> { { "ClubId", _databaseFixture.TestData.TeamWithFullDetails.Club!.ClubId! } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var result = await dataSource.ReadTotalBowlingFigures(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.Where(x => x.Teams.Select(t => t.Team?.TeamId).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamId))
                .SelectMany(x => x.MatchInnings.Where(i => i.BowlingTeam?.Team?.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId))
                .SelectMany(x => x.BowlingFigures)
                .Count();
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_bowling_figures_supports_filter_by_team_id()
        {
            var filter = new StatisticsFilter { Team = _databaseFixture.TestData.TeamWithFullDetails };
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns(("AND TeamId = @TeamId", new Dictionary<string, object> { { "TeamId", _databaseFixture.TestData.TeamWithFullDetails!.TeamId! } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var result = await dataSource.ReadTotalBowlingFigures(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.Where(x => x.Teams.Select(t => t.Team?.TeamId).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamId))
                .SelectMany(x => x.MatchInnings.Where(i => i.BowlingTeam?.Team?.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId))
                .SelectMany(x => x.BowlingFigures)
                .Count();
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_bowling_figures_supports_filter_by_match_location_id()
        {
            var filter = new StatisticsFilter { MatchLocation = _databaseFixture.TestData.MatchLocations.First() };
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns(("AND MatchLocationId = @MatchLocationId", new Dictionary<string, object> { { "MatchLocationId", _databaseFixture.TestData.MatchLocations.First().MatchLocationId! } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var result = await dataSource.ReadTotalBowlingFigures(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.Where(x => x.MatchLocation?.MatchLocationId == _databaseFixture.TestData.MatchLocations.First().MatchLocationId)
                .SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.BowlingFigures)
                .Count();
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_bowling_figures_supports_filter_by_competition_id()
        {
            var filter = new StatisticsFilter { Competition = _databaseFixture.TestData.Competitions.First() };
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns(("AND CompetitionId = @CompetitionId", new Dictionary<string, object> { { "CompetitionId", _databaseFixture.TestData.Competitions.First().CompetitionId! } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var result = await dataSource.ReadTotalBowlingFigures(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.Where(x => x.Season?.Competition?.CompetitionId == _databaseFixture.TestData.Competitions.First().CompetitionId)
                .SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.BowlingFigures)
                .Count();
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_bowling_figures_supports_filter_by_season_id()
        {
            var filter = new StatisticsFilter { Season = _databaseFixture.TestData.Competitions.First().Seasons.First() };
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns(("AND SeasonId = @SeasonId", new Dictionary<string, object> { { "SeasonId", _databaseFixture.TestData.Competitions.First().Seasons.First().SeasonId! } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var result = await dataSource.ReadTotalBowlingFigures(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.Where(x => x.Season?.SeasonId == _databaseFixture.TestData.Competitions.First().Seasons.First().SeasonId)
                .SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.BowlingFigures)
                .Count();
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_bowling_figures_supports_filter_by_date()
        {
            var dateRangeGenerator = new DateRangeGenerator();
            var (fromDate, untilDate) = dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);

            var filter = new StatisticsFilter { FromDate = fromDate, UntilDate = untilDate };
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate", new Dictionary<string, object> { { "FromDate", filter.FromDate }, { "UntilDate", filter.UntilDate } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var result = await dataSource.ReadTotalBowlingFigures(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.Where(x => x.StartTime >= filter.FromDate && x.StartTime <= filter.UntilDate)
                .SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.BowlingFigures)
                .Count();
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_bowling_figures_returns_player()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var results = await dataSource.ReadBowlingFigures(filter, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.BowlingFigures;
            foreach (var expectedBowler in expected)
            {
                var result = results.SingleOrDefault(x => x.Result?.BowlingFiguresId == expectedBowler.BowlingFiguresId);
                Assert.NotNull(result);

                Assert.Equal(expectedBowler.Bowler?.Player?.PlayerId, result!.Player?.PlayerId);
                Assert.Equal(expectedBowler.Bowler?.Player?.PlayerRoute, result.Player?.PlayerRoute);
                Assert.Equal(expectedBowler.Bowler?.PlayerIdentityId, result.Player?.PlayerIdentities.First().PlayerIdentityId);
                Assert.Equal(expectedBowler.Bowler?.PlayerIdentityName, result.Player?.PlayerIdentities.First().PlayerIdentityName);
            }
        }

        [Fact]
        public async Task Read_bowling_figures_returns_figures()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var results = await dataSource.ReadBowlingFigures(filter, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.BowlingFigures;
            foreach (var expectedBowler in expected)
            {
                var result = results.SingleOrDefault(x => x.Result?.BowlingFiguresId == expectedBowler.BowlingFiguresId);
                Assert.NotNull(result);

                Assert.Equal(expectedBowler.Overs, result!.Result?.Overs);
                Assert.Equal(expectedBowler.Maidens, result.Result?.Maidens);
                Assert.Equal(expectedBowler.RunsConceded, result.Result?.RunsConceded);
                Assert.Equal(expectedBowler.Wickets, result.Result?.Wickets);
            }
        }

        [Fact]
        public async Task Read_bowling_figures_returns_team()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var results = await dataSource.ReadBowlingFigures(filter, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.BowlingFigures;
            foreach (var expectedBowler in expected)
            {
                var result = results.SingleOrDefault(x => x.Result?.BowlingFiguresId == expectedBowler.BowlingFiguresId);
                Assert.NotNull(result);

                Assert.Equal(expectedBowler.Bowler?.Team?.TeamId, result!.Team?.TeamId);
                Assert.Equal(expectedBowler.Bowler?.Team?.TeamName, result.Team?.TeamName);
            }
        }

        [Fact]
        public async Task Read_bowling_figures_returns_opposition_team()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var results = await dataSource.ReadBowlingFigures(filter, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            foreach (var innings in _databaseFixture.TestData.MatchInnings)
            {
                foreach (var expectedBowler in innings.BowlingFigures)
                {
                    var result = results.SingleOrDefault(x => x.Result?.BowlingFiguresId == expectedBowler.BowlingFiguresId);
                    Assert.NotNull(result);

                    Assert.Equal(innings.BattingTeam?.Team?.TeamId, result!.OppositionTeam?.TeamId);
                    Assert.Equal(innings.BattingTeam?.Team?.TeamName, result.OppositionTeam?.TeamName);
                }
            }
        }

        [Fact]
        public async Task Read_bowling_figures_returns_match()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var results = await dataSource.ReadBowlingFigures(filter, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            foreach (var match in _databaseFixture.TestData.Matches)
            {
                foreach (var innings in match.MatchInnings)
                {
                    foreach (var expectedBowler in innings.BowlingFigures)
                    {
                        var result = results.SingleOrDefault(x => x.Result?.BowlingFiguresId == expectedBowler.BowlingFiguresId);
                        Assert.NotNull(result);

                        Assert.Equal(match.MatchRoute, result!.Match?.MatchRoute);
                        Assert.Equal(match.StartTime, result.Match?.StartTime);
                        Assert.Equal(match.MatchName, result.Match?.MatchName);
                    }
                }
            }
        }

        [Fact]
        public async Task Read_bowling_figures_supports_no_filter()
        {
            var filter = new StatisticsFilter();
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));

            await ActAndAssertReadBowlingFigures(filter,
                x => true,
                x => true,
                x => true
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_figures_supports_filter_by_player_id()
        {
            var filter = new StatisticsFilter
            {
                Player = _databaseFixture.TestData.BowlerWithMultipleIdentities
            };
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns(("AND PlayerId = @PlayerId", new Dictionary<string, object> { { "PlayerId", _databaseFixture.TestData.BowlerWithMultipleIdentities!.PlayerId! } }));

            await ActAndAssertReadBowlingFigures(filter,
                x => true,
                x => true,
                x => x.Bowler?.Player?.PlayerId == _databaseFixture.TestData.BowlerWithMultipleIdentities.PlayerId
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_figures_supports_filter_by_club_id()
        {
            var filter = new StatisticsFilter
            {
                Club = _databaseFixture.TestData.TeamWithFullDetails!.Club
            };
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns(("AND ClubId = @ClubId", new Dictionary<string, object> { { "ClubId", _databaseFixture.TestData.TeamWithFullDetails.Club!.ClubId! } }));

            await ActAndAssertReadBowlingFigures(filter,
                x => x.Teams.Select(t => t.Team?.TeamId).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamId),
                i => i.BowlingTeam?.Team?.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId,
                x => true
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_figures_supports_filter_by_team_id()
        {
            var filter = new StatisticsFilter
            {
                Team = _databaseFixture.TestData.TeamWithFullDetails
            };
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns(("AND TeamId = @TeamId", new Dictionary<string, object> { { "TeamId", _databaseFixture.TestData.TeamWithFullDetails!.TeamId! } }));

            await ActAndAssertReadBowlingFigures(filter,
                x => x.Teams.Select(t => t.Team?.TeamId).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamId),
                i => i.BowlingTeam?.Team?.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId,
                x => true
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_figures_supports_filter_by_team_route()
        {
            var filter = new StatisticsFilter
            {
                Team = _databaseFixture.TestData.TeamWithFullDetails
            };
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns(("AND TeamRoute = @TeamRoute", new Dictionary<string, object> { { "TeamRoute", _databaseFixture.TestData.TeamWithFullDetails!.TeamRoute! } }));

            await ActAndAssertReadBowlingFigures(filter,
                x => x.Teams.Select(t => t.Team?.TeamRoute).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamRoute),
                i => i.BowlingTeam?.Team?.TeamRoute == _databaseFixture.TestData.TeamWithFullDetails.TeamRoute,
                x => true
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_figures_supports_filter_by_match_location_id()
        {
            var filter = new StatisticsFilter
            {
                MatchLocation = _databaseFixture.TestData.MatchLocations.First()
            };
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns(("AND MatchLocationId = @MatchLocationId", new Dictionary<string, object> { { "MatchLocationId", _databaseFixture.TestData.MatchLocations.First().MatchLocationId! } }));

            await ActAndAssertReadBowlingFigures(filter,
                    x => x.MatchLocation?.MatchLocationId == _databaseFixture.TestData.MatchLocations.First().MatchLocationId,
                    x => true,
                    x => true
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_figures_supports_filter_by_competition_id()
        {
            var filter = new StatisticsFilter
            {
                Competition = _databaseFixture.TestData.Competitions.First()
            };
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns(("AND CompetitionId = @CompetitionId", new Dictionary<string, object> { { "CompetitionId", _databaseFixture.TestData.Competitions.First().CompetitionId! } }));

            await ActAndAssertReadBowlingFigures(filter,
                x => x.Season?.Competition?.CompetitionId == _databaseFixture.TestData.Competitions.First().CompetitionId,
                x => true,
                x => true
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_figures_supports_filter_by_season_id()
        {
            var filter = new StatisticsFilter
            {
                Season = _databaseFixture.TestData.Competitions.First().Seasons.First()
            };
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns(("AND SeasonId = @SeasonId", new Dictionary<string, object> { { "SeasonId", _databaseFixture.TestData.Competitions.First().Seasons.First().SeasonId! } }));

            await ActAndAssertReadBowlingFigures(filter,
                   x => x.Season?.SeasonId == _databaseFixture.TestData.Competitions.First().Seasons.First().SeasonId,
                   x => true,
                   x => true
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_figures_supports_filter_by_date()
        {
            var dateRangeGenerator = new DateRangeGenerator();
            var (fromDate, untilDate) = dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);

            var filter = new StatisticsFilter
            {
                FromDate = fromDate,
                UntilDate = untilDate
            };
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate", new Dictionary<string, object> { { "FromDate", filter.FromDate }, { "UntilDate", filter.UntilDate } }));

            await ActAndAssertReadBowlingFigures(filter,
                x => x.StartTime >= filter.FromDate && x.StartTime <= filter.UntilDate,
                x => true,
                x => true
            ).ConfigureAwait(false);
        }

        private async Task ActAndAssertReadBowlingFigures(StatisticsFilter filter, Func<Stoolball.Matches.Match, bool> matchFilter, Func<MatchInnings, bool> matchInningsFilter, Func<BowlingFigures, bool> bowlingFiguresFilter)
        {
            filter.Paging = new Paging { PageSize = int.MaxValue };

            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var results = await dataSource.ReadBowlingFigures(filter, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.Where(matchFilter)
                .SelectMany(x => x.MatchInnings).Where(matchInningsFilter)
                .SelectMany(x => x.BowlingFigures).Where(bowlingFiguresFilter)
                .ToList();

            Assert.Equal(expected.Count, results.Count());
            foreach (var expectedFigures in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result?.BowlingFiguresId == expectedFigures.BowlingFiguresId));
            }
        }

        [Fact]
        public async Task Read_best_bowling_figures_sorts_by_most_wickets_for_fewest_runs_with_null_runs_last()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var results = await dataSource.ReadBowlingFigures(filter, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            int? previousWickets = int.MaxValue;
            int? previousRunsConceded = int.MinValue;
            foreach (var result in results)
            {
                Assert.True(result.Result?.Wickets <= previousWickets);

                if (result.Result?.Wickets == previousWickets)
                {
                    Assert.True((result.Result!.RunsConceded.HasValue && previousRunsConceded.HasValue && result.Result?.RunsConceded >= previousRunsConceded) || !result.Result!.RunsConceded.HasValue);
                }

                previousWickets = result.Result?.Wickets;
                previousRunsConceded = result.Result?.RunsConceded;
            }
        }

        [Fact]
        public async Task Read_latest_bowling_figures_sorts_by_most_recent_first()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var results = await dataSource.ReadBowlingFigures(filter, StatisticsSortOrder.LatestFirst).ConfigureAwait(false);

            DateTimeOffset? previousStartTime = DateTimeOffset.MaxValue;
            foreach (var result in results)
            {
                Assert.True(result.Match?.StartTime <= previousStartTime);
                previousStartTime = result.Match?.StartTime;
            }
        }

        [Fact]
        public async Task Read_bowling_figures_pages_results()
        {
            const int pageSize = 10;
            var pageNumber = 1;
            var remaining = _databaseFixture.TestData.BowlingFigures.Count;
            while (remaining > 0)
            {
                var filter = new StatisticsFilter { Paging = new Paging { PageNumber = pageNumber, PageSize = pageSize } };
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
                var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);
                var results = await dataSource.ReadBowlingFigures(filter, StatisticsSortOrder.LatestFirst).ConfigureAwait(false);

                var expected = pageSize > remaining ? remaining : pageSize;
                Assert.Equal(expected, results.Count());

                pageNumber++;
                remaining -= pageSize;
            }
        }
    }
}
