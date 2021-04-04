using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stoolball.Matches;
using Stoolball.Navigation;
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

            var result = await dataSource.ReadTotalPlayerInnings(new StatisticsFilter { Player = _databaseFixture.BowlerWithMultipleIdentities }).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.PlayerInnings)
                .Count(x => x.Batter.Player.PlayerId == _databaseFixture.BowlerWithMultipleIdentities.PlayerId && x.RunsScored.HasValue);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_player_innings_supports_filter_by_club_id()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var result = await dataSource.ReadTotalPlayerInnings(new StatisticsFilter { Club = _databaseFixture.TeamWithClub.Club }).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.Where(x => x.Teams.Select(t => t.Team.TeamId).Contains(_databaseFixture.TeamWithClub.TeamId.Value))
                .SelectMany(x => x.MatchInnings.Where(i => i.BattingTeam.Team.TeamId == _databaseFixture.TeamWithClub.TeamId.Value))
                .SelectMany(x => x.PlayerInnings)
                .Count(x => x.RunsScored.HasValue);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_player_innings_supports_filter_by_team_id()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var result = await dataSource.ReadTotalPlayerInnings(new StatisticsFilter { Team = _databaseFixture.TeamWithClub }).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.Where(x => x.Teams.Select(t => t.Team.TeamId).Contains(_databaseFixture.TeamWithClub.TeamId.Value))
                .SelectMany(x => x.MatchInnings.Where(i => i.BattingTeam.Team.TeamId == _databaseFixture.TeamWithClub.TeamId.Value))
                .SelectMany(x => x.PlayerInnings)
                .Count(x => x.RunsScored.HasValue);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_player_innings_supports_filter_by_match_location_id()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var result = await dataSource.ReadTotalPlayerInnings(new StatisticsFilter { MatchLocation = _databaseFixture.MatchLocations.First() }).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.Where(x => x.MatchLocation?.MatchLocationId == _databaseFixture.MatchLocations.First().MatchLocationId)
                .SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.PlayerInnings)
                .Count(x => x.RunsScored.HasValue);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_player_innings_supports_filter_by_competition_id()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var result = await dataSource.ReadTotalPlayerInnings(new StatisticsFilter { Competition = _databaseFixture.Competitions.First() }).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.Where(x => x.Season?.Competition?.CompetitionId == _databaseFixture.Competitions.First().CompetitionId)
                .SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.PlayerInnings)
                .Count(x => x.RunsScored.HasValue);
            Assert.Equal(expected, result);
        }


        [Fact]
        public async Task Read_total_player_innings_supports_filter_by_season_id()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var result = await dataSource.ReadTotalPlayerInnings(new StatisticsFilter { Season = _databaseFixture.Competitions.First().Seasons.First() }).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.Where(x => x.Season?.SeasonId == _databaseFixture.Competitions.First().Seasons.First().SeasonId)
                .SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.PlayerInnings)
                .Count(x => x.RunsScored.HasValue);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_player_innings_returns_player()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadPlayerInnings(new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } }, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).Where(x => x.RunsScored.HasValue).ToList();
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
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadPlayerInnings(new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } }, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).Where(x => x.RunsScored.HasValue).ToList();
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
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadPlayerInnings(new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } }, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).Where(x => x.RunsScored.HasValue).ToList();
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
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadPlayerInnings(new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } }, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var matchInnings = _databaseFixture.Matches.SelectMany(x => x.MatchInnings);
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
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadPlayerInnings(new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } }, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            foreach (var match in _databaseFixture.Matches)
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
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadPlayerInnings(new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } }, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).Where(x => x.RunsScored.HasValue).ToList();

            var mystery = expected.Where(x => !results.Select(y => y.Result.PlayerInningsId).ToList().Contains(x.PlayerInningsId)).ToList();

            Assert.Equal(expected.Count, results.Count());
            foreach (var expectedInnings in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.PlayerInningsId == expectedInnings.PlayerInningsId));
            }
        }

        [Fact]
        public async Task Read_player_innings_supports_filter_by_player_id()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadPlayerInnings(new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                Player = _databaseFixture.BowlerWithMultipleIdentities
            },
            StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.PlayerInnings)
                .Where(x => x.Batter.Player.PlayerId == _databaseFixture.BowlerWithMultipleIdentities.PlayerId && x.RunsScored.HasValue).ToList();
            Assert.Equal(expected.Count, results.Count());
            foreach (var expectedInnings in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.PlayerInningsId == expectedInnings.PlayerInningsId));
            }
        }

        [Fact]
        public async Task Read_player_innings_supports_filter_by_club_id()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadPlayerInnings(new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                Club = _databaseFixture.TeamWithClub.Club
            },
            StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.Where(x => x.Teams.Select(t => t.Team.TeamId).Contains(_databaseFixture.TeamWithClub.TeamId.Value))
                .SelectMany(x => x.MatchInnings.Where(i => i.BattingTeam.Team.TeamId == _databaseFixture.TeamWithClub.TeamId.Value))
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
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadPlayerInnings(new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                Team = _databaseFixture.TeamWithClub
            },
            StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.Where(x => x.Teams.Select(t => t.Team.TeamId).Contains(_databaseFixture.TeamWithClub.TeamId.Value))
                .SelectMany(x => x.MatchInnings.Where(i => i.BattingTeam.Team.TeamId == _databaseFixture.TeamWithClub.TeamId.Value))
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
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadPlayerInnings(new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                MatchLocation = _databaseFixture.MatchLocations.First()
            },
            StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.Where(x => x.MatchLocation?.MatchLocationId == _databaseFixture.MatchLocations.First().MatchLocationId)
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
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadPlayerInnings(new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                Competition = _databaseFixture.Competitions.First()
            },
            StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.Where(x => x.Season?.Competition?.CompetitionId == _databaseFixture.Competitions.First().CompetitionId)
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
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadPlayerInnings(new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                Season = _databaseFixture.Competitions.First().Seasons.First()
            },
            StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.Where(x => x.Season?.SeasonId == _databaseFixture.Competitions.First().Seasons.First().SeasonId)
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
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadPlayerInnings(new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } }, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

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
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadPlayerInnings(new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } }, StatisticsSortOrder.LatestFirst).ConfigureAwait(false);

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
                var results = await dataSource.ReadPlayerInnings(new StatisticsFilter { Paging = new Paging { PageNumber = pageNumber, PageSize = pageSize } }, StatisticsSortOrder.LatestFirst).ConfigureAwait(false);

                var expected = pageSize > remaining ? remaining : pageSize;
                Assert.Equal(expected, results.Count());

                pageNumber++;
                remaining -= pageSize;
            }
        }

        [Fact]
        public async Task Read_player_innings_with_MaxResultsAllowingExtraResultsIfValuesAreEqual_returns_results_equal_to_the_max()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = (await dataSource.ReadPlayerInnings(new StatisticsFilter
            {
                MaxResultsAllowingExtraResultsIfValuesAreEqual = 5,
                Player = _databaseFixture.PlayerWithFifthAndSixthInningsTheSame
            },
            StatisticsSortOrder.BestFirst).ConfigureAwait(false)).ToList();

            var allExpectedResults = _databaseFixture.Matches.SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.PlayerInnings)
                .Where(x => x.Batter.Player.PlayerId == _databaseFixture.PlayerWithFifthAndSixthInningsTheSame.PlayerId && x.RunsScored.HasValue)
                .OrderByDescending(x => x.RunsScored).ThenBy(x => StatisticsConstants.DISMISSALS_THAT_ARE_OUT.Contains(x.DismissalType));

            var expected = new List<PlayerInnings>();
            foreach (var result in allExpectedResults)
            {
                if (expected.Count < 5 ||
                    (expected[expected.Count - 1].RunsScored == result.RunsScored && StatisticsConstants.DISMISSALS_THAT_ARE_OUT.Contains(expected[expected.Count - 1].DismissalType) == StatisticsConstants.DISMISSALS_THAT_ARE_OUT.Contains(result.DismissalType)))
                {
                    expected.Add(result);
                    continue;
                }
                else break;
            }

            Assert.Equal(expected.Count, results.Count);
            foreach (var expectedInnings in expected)
            {
                var result = results.SingleOrDefault(x => x.Result.PlayerInningsId == expectedInnings.PlayerInningsId);
                Assert.NotNull(result);

                Assert.Equal(expectedInnings.DismissalType, result.Result.DismissalType);
                Assert.Equal(expectedInnings.RunsScored, result.Result.RunsScored);
                Assert.Equal(expectedInnings.BallsFaced, result.Result.BallsFaced);
            }
            Assert.Equal(results[4].Result.RunsScored, results[5].Result.RunsScored);
        }
    }
}
