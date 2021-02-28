using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stoolball.Statistics;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Statistics
{
    [Collection(IntegrationTestConstants.StatisticsDataSourceIntegrationTestCollection)]
    public class ReadBowlingFiguresTests
    {
        private readonly SqlServerStatisticsDataSourceFixture _databaseFixture;

        public ReadBowlingFiguresTests(SqlServerStatisticsDataSourceFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_total_bowling_figures_supports_no_filter()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var result = await dataSource.ReadTotalBowlingFigures(null).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.BowlingFigures).Count();
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_bowling_figures_supports_filter_by_player_id()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var result = await dataSource.ReadTotalBowlingFigures(new StatisticsFilter { Player = _databaseFixture.BowlerWithMultipleIdentities }).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.BowlingFigures)
                .Count(x => x.Bowler.Player.PlayerId == _databaseFixture.BowlerWithMultipleIdentities.PlayerId);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_bowling_figures_supports_filter_by_club_id()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var result = await dataSource.ReadTotalBowlingFigures(new StatisticsFilter { Club = _databaseFixture.TeamWithClub.Club }).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.Where(x => x.Teams.Select(t => t.Team.TeamId).Contains(_databaseFixture.TeamWithClub.TeamId.Value))
                .SelectMany(x => x.MatchInnings.Where(i => i.BowlingTeam.Team.TeamId == _databaseFixture.TeamWithClub.TeamId.Value))
                .SelectMany(x => x.BowlingFigures)
                .Count();
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_bowling_figures_supports_filter_by_team_id()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var result = await dataSource.ReadTotalBowlingFigures(new StatisticsFilter { Team = _databaseFixture.TeamWithClub }).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.Where(x => x.Teams.Select(t => t.Team.TeamId).Contains(_databaseFixture.TeamWithClub.TeamId.Value))
                .SelectMany(x => x.MatchInnings.Where(i => i.BowlingTeam.Team.TeamId == _databaseFixture.TeamWithClub.TeamId.Value))
                .SelectMany(x => x.BowlingFigures)
                .Count();
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_bowling_figures_supports_filter_by_match_location_id()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var result = await dataSource.ReadTotalBowlingFigures(new StatisticsFilter { MatchLocation = _databaseFixture.MatchLocations.First() }).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.Where(x => x.MatchLocation?.MatchLocationId == _databaseFixture.MatchLocations.First().MatchLocationId)
                .SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.BowlingFigures)
                .Count();
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_bowling_figures_supports_filter_by_competition_id()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var result = await dataSource.ReadTotalBowlingFigures(new StatisticsFilter { Competition = _databaseFixture.Competitions.First() }).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.Where(x => x.Season?.Competition?.CompetitionId == _databaseFixture.Competitions.First().CompetitionId)
                .SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.BowlingFigures)
                .Count();
            Assert.Equal(expected, result);
        }


        [Fact]
        public async Task Read_total_bowling_figures_supports_filter_by_season_id()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var result = await dataSource.ReadTotalBowlingFigures(new StatisticsFilter { Season = _databaseFixture.Competitions.First().Seasons.First() }).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.Where(x => x.Season?.SeasonId == _databaseFixture.Competitions.First().Seasons.First().SeasonId)
                .SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.BowlingFigures)
                .Count();
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_bowling_figures_returns_player()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadBowlingFigures(new StatisticsFilter { PageSize = int.MaxValue }, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.BowlingFigures).ToList();
            foreach (var expectedBowler in expected)
            {
                var result = results.SingleOrDefault(x => x.Result.BowlingFiguresId == expectedBowler.BowlingFiguresId);
                Assert.NotNull(result);

                Assert.Equal(expectedBowler.Bowler.Player.PlayerId, result.Player.PlayerId);
                Assert.Equal(expectedBowler.Bowler.Player.PlayerRoute, result.Player.PlayerRoute);
                Assert.Equal(expectedBowler.Bowler.PlayerIdentityId, result.Player.PlayerIdentities.First().PlayerIdentityId);
                Assert.Equal(expectedBowler.Bowler.PlayerIdentityName, result.Player.PlayerIdentities.First().PlayerIdentityName);
            }
        }

        [Fact]
        public async Task Read_bowling_figures_returns_figures()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadBowlingFigures(new StatisticsFilter { PageSize = int.MaxValue }, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.BowlingFigures).ToList();
            foreach (var expectedBowler in expected)
            {
                var result = results.SingleOrDefault(x => x.Result.BowlingFiguresId == expectedBowler.BowlingFiguresId);
                Assert.NotNull(result);

                Assert.Equal(expectedBowler.Overs, result.Result.Overs);
                Assert.Equal(expectedBowler.Maidens, result.Result.Maidens);
                Assert.Equal(expectedBowler.RunsConceded, result.Result.RunsConceded);
                Assert.Equal(expectedBowler.Wickets, result.Result.Wickets);
            }
        }

        [Fact]
        public async Task Read_bowling_figures_returns_team()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadBowlingFigures(new StatisticsFilter { PageSize = int.MaxValue }, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.BowlingFigures).ToList();
            foreach (var expectedBowler in expected)
            {
                var result = results.SingleOrDefault(x => x.Result.BowlingFiguresId == expectedBowler.BowlingFiguresId);
                Assert.NotNull(result);

                Assert.Equal(expectedBowler.Bowler.Team.TeamId, result.Team.TeamId);
                Assert.Equal(expectedBowler.Bowler.Team.TeamName, result.Team.TeamName);
            }
        }

        [Fact]
        public async Task Read_bowling_figures_returns_opposition_team()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadBowlingFigures(new StatisticsFilter { PageSize = int.MaxValue }, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var matchInnings = _databaseFixture.Matches.SelectMany(x => x.MatchInnings);
            foreach (var innings in matchInnings)
            {
                foreach (var expectedBowler in innings.BowlingFigures)
                {
                    var result = results.SingleOrDefault(x => x.Result.BowlingFiguresId == expectedBowler.BowlingFiguresId);
                    Assert.NotNull(result);

                    Assert.Equal(innings.BattingTeam.Team.TeamId, result.OppositionTeam.TeamId);
                    Assert.Equal(innings.BattingTeam.Team.TeamName, result.OppositionTeam.TeamName);
                }
            }
        }

        [Fact]
        public async Task Read_bowling_figures_returns_match()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadBowlingFigures(new StatisticsFilter { PageSize = int.MaxValue }, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            foreach (var match in _databaseFixture.Matches)
            {
                foreach (var innings in match.MatchInnings)
                {
                    foreach (var expectedBowler in innings.BowlingFigures)
                    {
                        var result = results.SingleOrDefault(x => x.Result.BowlingFiguresId == expectedBowler.BowlingFiguresId);
                        Assert.NotNull(result);

                        Assert.Equal(match.MatchRoute, result.Match.MatchRoute);
                        Assert.Equal(match.StartTime.AccurateToTheMinute(), result.Match.StartTime.AccurateToTheMinute());
                        Assert.Equal(match.MatchName, result.Match.MatchName);
                    }
                }
            }
        }

        [Fact]
        public async Task Read_bowling_figures_supports_no_filter()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadBowlingFigures(new StatisticsFilter { PageSize = int.MaxValue }, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.BowlingFigures).ToList();
            Assert.Equal(expected.Count, results.Count());
            foreach (var expectedFigures in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.BowlingFiguresId == expectedFigures.BowlingFiguresId));
            }
        }

        [Fact]
        public async Task Read_bowling_figures_supports_filter_by_player_id()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadBowlingFigures(new StatisticsFilter
            {
                PageSize = int.MaxValue,
                Player = _databaseFixture.BowlerWithMultipleIdentities
            },
            StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.BowlingFigures)
                .Where(x => x.Bowler.Player.PlayerId == _databaseFixture.BowlerWithMultipleIdentities.PlayerId).ToList();
            Assert.Equal(expected.Count, results.Count());
            foreach (var expectedFigures in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.BowlingFiguresId == expectedFigures.BowlingFiguresId));
            }
        }

        [Fact]
        public async Task Read_bowling_figures_supports_filter_by_club_id()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadBowlingFigures(new StatisticsFilter
            {
                PageSize = int.MaxValue,
                Club = _databaseFixture.TeamWithClub.Club
            },
            StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.Where(x => x.Teams.Select(t => t.Team.TeamId).Contains(_databaseFixture.TeamWithClub.TeamId.Value))
                .SelectMany(x => x.MatchInnings.Where(i => i.BowlingTeam.Team.TeamId == _databaseFixture.TeamWithClub.TeamId.Value))
                .SelectMany(x => x.BowlingFigures)
                .ToList();
            Assert.Equal(expected.Count, results.Count());
            foreach (var expectedFigures in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.BowlingFiguresId == expectedFigures.BowlingFiguresId));
            }
        }

        [Fact]
        public async Task Read_bowling_figures_supports_filter_by_team_id()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadBowlingFigures(new StatisticsFilter
            {
                PageSize = int.MaxValue,
                Team = _databaseFixture.TeamWithClub
            },
            StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.Where(x => x.Teams.Select(t => t.Team.TeamId).Contains(_databaseFixture.TeamWithClub.TeamId.Value))
                .SelectMany(x => x.MatchInnings.Where(i => i.BowlingTeam.Team.TeamId == _databaseFixture.TeamWithClub.TeamId.Value))
                .SelectMany(x => x.BowlingFigures)
                .ToList();
            Assert.Equal(expected.Count, results.Count());
            foreach (var expectedFigures in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.BowlingFiguresId == expectedFigures.BowlingFiguresId));
            }
        }

        [Fact]
        public async Task Read_bowling_figures_supports_filter_by_match_location_id()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadBowlingFigures(new StatisticsFilter
            {
                PageSize = int.MaxValue,
                MatchLocation = _databaseFixture.MatchLocations.First()
            },
            StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.Where(x => x.MatchLocation?.MatchLocationId == _databaseFixture.MatchLocations.First().MatchLocationId)
                .SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.BowlingFigures)
                .ToList();
            Assert.Equal(expected.Count, results.Count());
            foreach (var expectedFigures in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.BowlingFiguresId == expectedFigures.BowlingFiguresId));
            }
        }

        [Fact]
        public async Task Read_bowling_figures_supports_filter_by_competition_id()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadBowlingFigures(new StatisticsFilter
            {
                PageSize = int.MaxValue,
                Competition = _databaseFixture.Competitions.First()
            },
            StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.Where(x => x.Season?.Competition?.CompetitionId == _databaseFixture.Competitions.First().CompetitionId)
                .SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.BowlingFigures)
                .ToList();
            Assert.Equal(expected.Count, results.Count());
            foreach (var expectedFigures in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.BowlingFiguresId == expectedFigures.BowlingFiguresId));
            }
        }

        [Fact]
        public async Task Read_bowling_figures_supports_filter_by_season_id()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadBowlingFigures(new StatisticsFilter
            {
                PageSize = int.MaxValue,
                Season = _databaseFixture.Competitions.First().Seasons.First()
            },
            StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.Matches.Where(x => x.Season?.SeasonId == _databaseFixture.Competitions.First().Seasons.First().SeasonId)
                .SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.BowlingFigures)
                .ToList();
            Assert.Equal(expected.Count, results.Count());
            foreach (var expectedFigures in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.BowlingFiguresId == expectedFigures.BowlingFiguresId));
            }
        }

        [Fact]
        public async Task Read_best_bowling_figures_sorts_by_most_wickets_for_fewest_runs_with_null_runs_last()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadBowlingFigures(new StatisticsFilter { PageSize = int.MaxValue }, StatisticsSortOrder.BestFirst).ConfigureAwait(false);

            var previousWickets = int.MaxValue;
            int? previousRunsConceded = int.MinValue;
            foreach (var result in results)
            {
                Assert.True(result.Result.Wickets <= previousWickets);

                if (result.Result.Wickets == previousWickets)
                {
                    Assert.True((result.Result.RunsConceded.HasValue && previousRunsConceded.HasValue && result.Result.RunsConceded >= previousRunsConceded) || !result.Result.RunsConceded.HasValue);
                }

                previousWickets = result.Result.Wickets;
                previousRunsConceded = result.Result.RunsConceded;
            }
        }

        [Fact]
        public async Task Read_latest_bowling_figures_sorts_by_most_recent_first()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = await dataSource.ReadBowlingFigures(new StatisticsFilter { PageSize = int.MaxValue }, StatisticsSortOrder.LatestFirst).ConfigureAwait(false);

            var previousStartTime = DateTimeOffset.MaxValue;
            foreach (var result in results)
            {
                Assert.True(result.Match.StartTime.AccurateToTheMinute() <= previousStartTime);
                previousStartTime = result.Match.StartTime.AccurateToTheMinute();
            }
        }

        [Fact]
        public async Task Read_bowling_figures_pages_results()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            const int pageSize = 10;
            var pageNumber = 1;
            var remaining = _databaseFixture.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.BowlingFigures).Count();
            while (remaining > 0)
            {
                var results = await dataSource.ReadBowlingFigures(new StatisticsFilter { PageNumber = pageNumber, PageSize = pageSize }, StatisticsSortOrder.LatestFirst).ConfigureAwait(false);

                var expected = pageSize > remaining ? remaining : pageSize;
                Assert.Equal(expected, results.Count());

                pageNumber++;
                remaining -= pageSize;
            }
        }

        [Fact]
        public async Task Read_bowling_figures_with_MaxResultsAllowingExtraResultsIfValuesAreEqual_returns_results_equal_to_the_max()
        {
            var dataSource = new SqlServerStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var results = (await dataSource.ReadBowlingFigures(new StatisticsFilter
            {
                MaxResultsAllowingExtraResultsIfValuesAreEqual = 5,
                Player = _databaseFixture.PlayerWithFifthAndSixthBowlingFiguresTheSame
            },
            StatisticsSortOrder.BestFirst).ConfigureAwait(false)).ToList();

            var allExpectedResults = _databaseFixture.Matches.SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.BowlingFigures)
                .Where(x => x.Bowler.Player.PlayerId == _databaseFixture.PlayerWithFifthAndSixthBowlingFiguresTheSame.PlayerId)
                .OrderByDescending(x => x.Wickets).ThenByDescending(x => x.RunsConceded.HasValue).ThenBy(x => x.RunsConceded);

            var expected = new List<BowlingFigures>();
            foreach (var result in allExpectedResults)
            {
                if (expected.Count < 5 ||
                    (expected[expected.Count - 1].Wickets == result.Wickets && expected[expected.Count - 1].RunsConceded == result.RunsConceded))
                {
                    expected.Add(result);
                    continue;
                }
                else break;
            }

            Assert.Equal(expected.Count, results.Count);
            foreach (var expectedFigures in expected)
            {
                var result = results.SingleOrDefault(x => x.Result.BowlingFiguresId == expectedFigures.BowlingFiguresId);
                Assert.NotNull(result);

                Assert.Equal(expectedFigures.Overs, result.Result.Overs);
                Assert.Equal(expectedFigures.Maidens, result.Result.Maidens);
                Assert.Equal(expectedFigures.RunsConceded, result.Result.RunsConceded);
                Assert.Equal(expectedFigures.Wickets, result.Result.Wickets);
            }
            Assert.Equal(results[4].Result.Wickets, results[5].Result.Wickets);
            Assert.Equal(results[4].Result.RunsConceded, results[5].Result.RunsConceded);
        }
    }
}
