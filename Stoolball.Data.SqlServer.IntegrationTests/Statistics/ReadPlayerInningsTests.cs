﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stoolball.Statistics;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Statistics
{
    [Collection(IntegrationTestConstants.StatisticsDataSourceIntegrationTestCollection)]
    public class ReadPlayerInningsTests
    {
        private readonly SqlServerStatisticsDataSourceFixture _databaseFixture;

        public ReadPlayerInningsTests(SqlServerStatisticsDataSourceFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_total_player_innings_supports_no_filter()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var result = await dataSource.ReadTotalPlayerInnings(null).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).Count(x => x.RunsScored.HasValue);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_player_innings_supports_filter_by_player_id()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var result = await dataSource.ReadTotalPlayerInnings(new StatisticsFilter { PlayerIds = new List<Guid> { _databaseFixture.PlayerWithMultipleIdentities.PlayerId.Value } }).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.PlayerInnings)
                .Count(x => x.Batter.Player.PlayerId == _databaseFixture.PlayerWithMultipleIdentities.PlayerId && x.RunsScored.HasValue);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_player_innings_returns_player()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadPlayerInnings(new StatisticsFilter { PageSize = int.MaxValue }, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).Where(x => x.RunsScored.HasValue).ToList();
            foreach (var expectedInnings in expected)
            {
                var result = results.SingleOrDefault(x => x.PlayerInnings.PlayerInningsId == expectedInnings.PlayerInningsId);
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
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadPlayerInnings(new StatisticsFilter { PageSize = int.MaxValue }, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).Where(x => x.RunsScored.HasValue).ToList();
            foreach (var expectedInnings in expected)
            {
                var result = results.SingleOrDefault(x => x.PlayerInnings.PlayerInningsId == expectedInnings.PlayerInningsId);
                Assert.NotNull(result);

                Assert.Equal(expectedInnings.DismissalType, result.PlayerInnings.DismissalType);
                Assert.Equal(expectedInnings.RunsScored, result.PlayerInnings.RunsScored);
                Assert.Equal(expectedInnings.BallsFaced, result.PlayerInnings.BallsFaced);
            }
        }

        [Fact]
        public async Task Read_player_innings_returns_team()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadPlayerInnings(new StatisticsFilter { PageSize = int.MaxValue }, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).Where(x => x.RunsScored.HasValue).ToList();
            foreach (var expectedInnings in expected)
            {
                var result = results.SingleOrDefault(x => x.PlayerInnings.PlayerInningsId == expectedInnings.PlayerInningsId);
                Assert.NotNull(result);

                Assert.Equal(expectedInnings.Batter.Team.TeamId, result.Team.TeamId);
                Assert.Equal(expectedInnings.Batter.Team.TeamName, result.Team.TeamName);
            }
        }

        [Fact]
        public async Task Read_player_innings_returns_opposition_team()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadPlayerInnings(new StatisticsFilter { PageSize = int.MaxValue }, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var matchInnings = _databaseFixture.Matches.SelectMany(x => x.MatchInnings);
            foreach (var innings in matchInnings)
            {
                foreach (var playerInnings in innings.PlayerInnings.Where(x => x.RunsScored.HasValue))
                {
                    var result = results.SingleOrDefault(x => x.PlayerInnings.PlayerInningsId == playerInnings.PlayerInningsId);
                    Assert.NotNull(result);

                    Assert.Equal(innings.BowlingTeam.Team.TeamId, result.OppositionTeam.TeamId);
                    Assert.Equal(innings.BowlingTeam.Team.TeamName, result.OppositionTeam.TeamName);
                }
            }
        }

        [Fact]
        public async Task Read_player_innings_returns_match()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadPlayerInnings(new StatisticsFilter { PageSize = int.MaxValue }, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            foreach (var match in _databaseFixture.Matches)
            {
                foreach (var innings in match.MatchInnings)
                {
                    foreach (var playerInnings in innings.PlayerInnings.Where(x => x.RunsScored.HasValue))
                    {
                        var result = results.SingleOrDefault(x => x.PlayerInnings.PlayerInningsId == playerInnings.PlayerInningsId);
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
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadPlayerInnings(new StatisticsFilter { PageSize = int.MaxValue }, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).Where(x => x.RunsScored.HasValue).ToList();
            Assert.Equal(expected.Count, results.Count());
            foreach (var expectedInnings in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.PlayerInnings.PlayerInningsId == expectedInnings.PlayerInningsId));
            }
        }

        [Fact]
        public async Task Read_player_innings_supports_filter_by_player_id()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadPlayerInnings(new StatisticsFilter
            {
                PageSize = int.MaxValue,
                PlayerIds = new List<Guid> { _databaseFixture.PlayerWithMultipleIdentities.PlayerId.Value }
            },
            StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.PlayerInnings)
                .Where(x => x.Batter.Player.PlayerId == _databaseFixture.PlayerWithMultipleIdentities.PlayerId && x.RunsScored.HasValue).ToList();
            Assert.Equal(expected.Count, results.Count());
            foreach (var expectedInnings in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.PlayerInnings.PlayerInningsId == expectedInnings.PlayerInningsId));
            }
        }

        [Fact]
        public async Task Read_best_player_innings_sorts_by_highest_score_with_not_out_first()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadPlayerInnings(new StatisticsFilter { PageSize = int.MaxValue }, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var previousScore = int.MaxValue;
            var previousScoreWasOut = false;
            foreach (var result in results)
            {
                Assert.True(result.PlayerInnings.RunsScored.Value <= previousScore);

                if (result.PlayerInnings.RunsScored.Value == previousScore && previousScoreWasOut)
                {
                    Assert.Contains(result.PlayerInnings.DismissalType, StatisticsConstants.DISMISSALS_THAT_ARE_OUT);
                }

                previousScore = result.PlayerInnings.RunsScored.Value;
                previousScoreWasOut = StatisticsConstants.DISMISSALS_THAT_ARE_OUT.Contains(result.PlayerInnings.DismissalType);
            }
        }

        [Fact]
        public async Task Read_latest_player_innings_sorts_by_most_recent_first()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadPlayerInnings(new StatisticsFilter { PageSize = int.MaxValue }, StatisticsSortOrder.LatestFirst).ConfigureAwait(false);

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
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            const int pageSize = 10;
            var pageNumber = 1;
            var remaining = _databaseFixture.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).Count(x => x.RunsScored.HasValue);
            while (remaining > 0)
            {
                var results = await dataSource.ReadPlayerInnings(new StatisticsFilter { PageNumber = pageNumber, PageSize = pageSize }, StatisticsSortOrder.LatestFirst).ConfigureAwait(false);

                var expected = pageSize > remaining ? remaining : pageSize;
                Assert.Equal(expected, results.Count());

                pageNumber++;
                remaining -= pageSize;
            }
        }
    }
}