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
    public class ReadBestPlayerPerformanceTests
    {
        private readonly SqlServerTestDataFixture _databaseFixture;
        private readonly Mock<IStatisticsQueryBuilder> _queryBuilder = new();

        public ReadBestPlayerPerformanceTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_total_performances_supports_no_filter()
        {
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object);

            var result = await dataSource.ReadTotalPlayerIdentityPerformances(null).ConfigureAwait(false);

            var performances = AllPlayerIdentityPerformances(_databaseFixture.TestData.Matches);

            Assert.Equal(performances.Count(), result);
        }

        [Fact]
        public async Task Read_total_performances_supports_filter_by_player_id()
        {
            var filter = new StatisticsFilter { Player = _databaseFixture.TestData.BowlerWithMultipleIdentities };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerId = @PlayerId", new Dictionary<string, object> { { "PlayerId", _databaseFixture.TestData.BowlerWithMultipleIdentities!.PlayerId! } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object);

            var result = await dataSource.ReadTotalPlayerIdentityPerformances(filter).ConfigureAwait(false);

            var matchesForPlayer = new MatchFinder().MatchesPlayedByPlayer(_databaseFixture.TestData.Matches, filter.Player!.PlayerId!.Value);
            var performances = AllPlayerIdentityPerformances(matchesForPlayer);
            performances = performances.Where(x => x.PlayerId == filter.Player.PlayerId);

            Assert.Equal(performances.Count(), result);
        }

        [Fact]
        public async Task Read_total_performances_supports_filter_by_club_id()
        {
            var filter = new StatisticsFilter { Club = _databaseFixture.TestData.TeamWithFullDetails!.Club };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND ClubId = @ClubId", new Dictionary<string, object> { { "ClubId", _databaseFixture.TestData.TeamWithFullDetails.Club!.ClubId! } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object);

            var result = await dataSource.ReadTotalPlayerIdentityPerformances(filter).ConfigureAwait(false);

            var teamIdsInClub = filter.Club!.Teams.Select(x => x.TeamId);
            var playerIdentitiesInClub = _databaseFixture.TestData.PlayerIdentities.Where(x => teamIdsInClub.Contains(x.Team?.TeamId)).Select(x => x.PlayerIdentityId);
            var performances = AllPlayerIdentityPerformances(_databaseFixture.TestData.Matches.Where(x => x.Teams.Any(t => teamIdsInClub.Contains(t.Team?.TeamId))));
            performances = performances.Where(x => playerIdentitiesInClub.Contains(x.PlayerIdentityId));

            Assert.Equal(performances.Count(), result);
        }

        [Fact]
        public async Task Read_total_performances_supports_filter_by_team_id()
        {
            var filter = new StatisticsFilter { Team = _databaseFixture.TestData.TeamWithFullDetails };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND TeamId = @TeamId", new Dictionary<string, object> { { "TeamId", _databaseFixture.TestData.TeamWithFullDetails!.TeamId! } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object);

            var result = await dataSource.ReadTotalPlayerIdentityPerformances(filter).ConfigureAwait(false);

            var matchesForTeam = _databaseFixture.TestData.Matches.Where(x => x.Teams.Select(t => t.Team?.TeamId).Contains(filter.Team!.TeamId));
            var playerIdentitiesInTeam = _databaseFixture.TestData.PlayerIdentities.Where(x => x.Team?.TeamId == filter.Team!.TeamId).Select(x => x.PlayerIdentityId);
            var performances = AllPlayerIdentityPerformances(_databaseFixture.TestData.Matches.Where(x => x.Teams.Select(t => t.Team?.TeamId).Contains(filter.Team!.TeamId)));
            performances = performances.Where(x => playerIdentitiesInTeam.Contains(x.PlayerIdentityId));

            Assert.Equal(performances.Count(), result);
        }

        [Fact]
        public async Task Read_total_performances_supports_filter_by_team_route()
        {
            var filter = new StatisticsFilter { Team = _databaseFixture.TestData.TeamWithFullDetails };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND TeamRoute = @TeamRoute", new Dictionary<string, object> { { "TeamRoute", _databaseFixture.TestData.TeamWithFullDetails!.TeamRoute! } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object);

            var result = await dataSource.ReadTotalPlayerIdentityPerformances(filter).ConfigureAwait(false);

            var matchesForTeam = _databaseFixture.TestData.Matches.Where(x => x.Teams.Select(t => t.Team?.TeamRoute).Contains(filter.Team!.TeamRoute));
            var playerIdentitiesInTeam = _databaseFixture.TestData.PlayerIdentities.Where(x => x.Team?.TeamRoute == filter.Team!.TeamRoute).Select(x => x.PlayerIdentityId);
            var performances = AllPlayerIdentityPerformances(_databaseFixture.TestData.Matches.Where(x => x.Teams.Select(t => t.Team?.TeamRoute).Contains(filter.Team!.TeamRoute)));
            performances = performances.Where(x => playerIdentitiesInTeam.Contains(x.PlayerIdentityId));

            Assert.Equal(performances.Count(), result);
        }

        [Fact]
        public async Task Read_total_performances_supports_filter_by_match_location_id()
        {
            var filter = new StatisticsFilter { MatchLocation = _databaseFixture.TestData.MatchLocations.First() };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND MatchLocationId = @MatchLocationId", new Dictionary<string, object> { { "MatchLocationId", _databaseFixture.TestData.MatchLocations.First().MatchLocationId! } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object);

            var result = await dataSource.ReadTotalPlayerIdentityPerformances(filter).ConfigureAwait(false);

            var performances = AllPlayerIdentityPerformances(_databaseFixture.TestData.Matches.Where(x => x.MatchLocation?.MatchLocationId == filter.MatchLocation.MatchLocationId));

            Assert.Equal(performances.Count(), result);
        }

        [Fact]
        public async Task Read_total_performances_supports_filter_by_competition_id()
        {
            var filter = new StatisticsFilter { Competition = _databaseFixture.TestData.Competitions.First() };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND CompetitionId = @CompetitionId", new Dictionary<string, object> { { "CompetitionId", _databaseFixture.TestData.Competitions.First().CompetitionId! } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object);

            var result = await dataSource.ReadTotalPlayerIdentityPerformances(filter).ConfigureAwait(false);

            var performances = AllPlayerIdentityPerformances(_databaseFixture.TestData.Matches.Where(x => x.Season?.Competition.CompetitionId == filter.Competition.CompetitionId));

            Assert.Equal(performances.Count(), result);
        }


        [Fact]
        public async Task Read_total_performances_supports_filter_by_season_id()
        {
            var filter = new StatisticsFilter { Season = _databaseFixture.TestData.Competitions.First().Seasons.First() };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND SeasonId = @SeasonId", new Dictionary<string, object> { { "SeasonId", _databaseFixture.TestData.Competitions.First().Seasons.First().SeasonId! } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object);

            var result = await dataSource.ReadTotalPlayerIdentityPerformances(filter).ConfigureAwait(false);

            var performances = AllPlayerIdentityPerformances(_databaseFixture.TestData.Matches.Where(x => x.Season?.SeasonId == filter.Season.SeasonId));

            Assert.Equal(performances.Count(), result);
        }

        [Fact]
        public async Task Read_total_performances_supports_filter_by_date()
        {
            var dateRangeGenerator = new DateRangeGenerator();
            var (fromDate, untilDate) = dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);

            var filter = new StatisticsFilter { FromDate = fromDate, UntilDate = untilDate };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate", new Dictionary<string, object> { { "FromDate", filter.FromDate }, { "UntilDate", filter.UntilDate } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object);

            var result = await dataSource.ReadTotalPlayerIdentityPerformances(filter).ConfigureAwait(false);

            var performances = AllPlayerIdentityPerformances(_databaseFixture.TestData.Matches.Where(x => x.StartTime >= filter.FromDate && x.StartTime <= filter.UntilDate));

            Assert.Equal(performances.Count(), result);
        }

        [Fact]
        public async Task Read_total_performances_supports_filter_by_player_of_the_match()
        {
            var filter = new StatisticsFilter { PlayerOfTheMatch = true };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerOfTheMatch = 1", new Dictionary<string, object>()));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object);

            var result = await dataSource.ReadTotalPlayerIdentityPerformances(filter).ConfigureAwait(false);

            var performances = AllPlayerIdentityPerformances(_databaseFixture.TestData.Matches.Where(x => x.Awards.Any(aw => aw.Award?.AwardName == "Player of the match"))).Where(x => x.PlayerOfTheMatch);

            Assert.Equal(performances.Count(), result);
        }

        [Fact]
        public async Task Read_performances_supports_no_filter()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object);

            var results = await dataSource.ReadPlayerIdentityPerformances(filter).ConfigureAwait(false);

            var performances = AllPlayerIdentityPerformances(_databaseFixture.TestData.Matches);

            Assert.NotEmpty(performances);
            AssertPerformancesEachOccurOnceInResults(performances, results);
        }

        [Fact]
        public async Task Read_performances_supports_filter_by_player_id()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                Player = _databaseFixture.TestData.BowlerWithMultipleIdentities
            };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerId = @PlayerId", new Dictionary<string, object> { { "PlayerId", _databaseFixture.TestData.BowlerWithMultipleIdentities!.PlayerId! } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object);

            var results = await dataSource.ReadPlayerIdentityPerformances(filter).ConfigureAwait(false);

            var matchesForPlayer = new MatchFinder().MatchesPlayedByPlayer(_databaseFixture.TestData.Matches, filter.Player!.PlayerId!.Value);
            var performances = AllPlayerIdentityPerformances(matchesForPlayer);
            performances = performances.Where(x => x.PlayerId == filter.Player.PlayerId);

            Assert.NotEmpty(performances);
            AssertPerformancesEachOccurOnceInResults(performances, results);
        }

        [Fact]
        public async Task Read_performances_supports_filter_by_club_id()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                Club = _databaseFixture.TestData.TeamWithFullDetails!.Club
            };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND ClubId = @ClubId", new Dictionary<string, object> { { "ClubId", _databaseFixture.TestData.TeamWithFullDetails.Club!.ClubId! } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object);

            var results = await dataSource.ReadPlayerIdentityPerformances(filter).ConfigureAwait(false);

            var teamIdsInClub = filter.Club!.Teams.Select(x => x.TeamId);
            var playerIdentitiesInClub = _databaseFixture.TestData.PlayerIdentities.Where(x => teamIdsInClub.Contains(x.Team?.TeamId)).Select(x => x.PlayerIdentityId);
            var performances = AllPlayerIdentityPerformances(_databaseFixture.TestData.Matches.Where(x => x.Teams.Any(t => teamIdsInClub.Contains(t.Team?.TeamId))));
            performances = performances.Where(x => playerIdentitiesInClub.Contains(x.PlayerIdentityId));

            Assert.NotEmpty(performances);
            AssertPerformancesEachOccurOnceInResults(performances, results);
        }

        [Fact]
        public async Task Read_performances_supports_filter_by_team_id()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                Team = _databaseFixture.TestData.TeamWithFullDetails
            };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND TeamId = @TeamId", new Dictionary<string, object> { { "TeamId", _databaseFixture.TestData.TeamWithFullDetails!.TeamId! } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object);

            var results = await dataSource.ReadPlayerIdentityPerformances(filter).ConfigureAwait(false);

            var playerIdentitiesInTeam = _databaseFixture.TestData.PlayerIdentities.Where(x => x.Team?.TeamId == filter.Team!.TeamId).Select(x => x.PlayerIdentityId);
            var performances = AllPlayerIdentityPerformances(_databaseFixture.TestData.Matches.Where(x => x.Teams.Select(t => t.Team?.TeamId).Contains(filter.Team!.TeamId)));
            performances = performances.Where(x => playerIdentitiesInTeam.Contains(x.PlayerIdentityId));

            Assert.NotEmpty(performances);
            AssertPerformancesEachOccurOnceInResults(performances, results);
        }


        [Fact]
        public async Task Read_performances_supports_filter_by_team_route()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                Team = _databaseFixture.TestData.TeamWithFullDetails
            };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND TeamRoute = @TeamRoute", new Dictionary<string, object> { { "TeamRoute", _databaseFixture.TestData.TeamWithFullDetails!.TeamRoute! } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object);

            var results = await dataSource.ReadPlayerIdentityPerformances(filter).ConfigureAwait(false);

            var playerIdentitiesInTeam = _databaseFixture.TestData.PlayerIdentities.Where(x => x.Team?.TeamRoute == filter.Team!.TeamRoute).Select(x => x.PlayerIdentityId);
            var performances = AllPlayerIdentityPerformances(_databaseFixture.TestData.Matches.Where(x => x.Teams.Select(t => t.Team?.TeamRoute).Contains(filter.Team!.TeamRoute)));
            performances = performances.Where(x => playerIdentitiesInTeam.Contains(x.PlayerIdentityId));

            Assert.NotEmpty(performances);
            AssertPerformancesEachOccurOnceInResults(performances, results);
        }

        [Fact]
        public async Task Read_performances_supports_filter_by_match_location_id()
        {
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
                _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND MatchLocationId = @MatchLocationId", new Dictionary<string, object> { { "MatchLocationId", filter.MatchLocation.MatchLocationId! } }));
                var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object);

                var results = await dataSource.ReadPlayerIdentityPerformances(filter).ConfigureAwait(false);

                var performances = AllPlayerIdentityPerformances(_databaseFixture.TestData.Matches.Where(x => x.MatchLocation?.MatchLocationId == filter.MatchLocation.MatchLocationId));

                foundAtLeastOne = foundAtLeastOne || performances.Any();

                AssertPerformancesEachOccurOnceInResults(performances, results);
            }
            Assert.True(foundAtLeastOne);
        }

        [Fact]
        public async Task Read_performances_supports_filter_by_competition_id()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                Competition = _databaseFixture.TestData.Competitions.First()
            };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND CompetitionId = @CompetitionId", new Dictionary<string, object> { { "CompetitionId", _databaseFixture.TestData.Competitions.First().CompetitionId! } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object);

            var results = await dataSource.ReadPlayerIdentityPerformances(filter).ConfigureAwait(false);

            var performances = AllPlayerIdentityPerformances(_databaseFixture.TestData.Matches.Where(x => x.Season?.Competition?.CompetitionId == filter.Competition.CompetitionId));

            Assert.NotEmpty(performances);
            AssertPerformancesEachOccurOnceInResults(performances, results);
        }

        [Fact]
        public async Task Read_performances_supports_filter_by_season_id()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                Season = _databaseFixture.TestData.Competitions.First().Seasons.First()
            };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND SeasonId = @SeasonId", new Dictionary<string, object> { { "SeasonId", _databaseFixture.TestData.Competitions.First().Seasons.First().SeasonId! } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object);

            var results = await dataSource.ReadPlayerIdentityPerformances(filter).ConfigureAwait(false);

            var performances = AllPlayerIdentityPerformances(_databaseFixture.TestData.Matches.Where(x => x.Season?.SeasonId == filter.Season.SeasonId));

            Assert.NotEmpty(performances);
            AssertPerformancesEachOccurOnceInResults(performances, results);
        }

        [Fact]
        public async Task Read_performances_supports_filter_by_date()
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
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate", new Dictionary<string, object> { { "FromDate", filter.FromDate }, { "UntilDate", filter.UntilDate } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object);

            var results = await dataSource.ReadPlayerIdentityPerformances(filter).ConfigureAwait(false);

            var performances = AllPlayerIdentityPerformances(_databaseFixture.TestData.Matches.Where(x => x.StartTime >= filter.FromDate && x.StartTime <= filter.UntilDate));

            Assert.NotEmpty(performances);
            AssertPerformancesEachOccurOnceInResults(performances, results);
        }

        [Fact]
        public async Task Read_performances_supports_filter_by_player_of_the_match()
        {
            var filter = new StatisticsFilter
            {
                Paging = new Paging
                {
                    PageSize = int.MaxValue
                },
                PlayerOfTheMatch = true
            };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerOfTheMatch = 1", new Dictionary<string, object>()));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object);

            var results = await dataSource.ReadPlayerIdentityPerformances(filter).ConfigureAwait(false);

            var performances = AllPlayerIdentityPerformances(_databaseFixture.TestData.Matches.Where(x => x.Awards.Any(aw => aw.Award?.AwardName == "Player of the match"))).Where(x => x.PlayerOfTheMatch);

            Assert.NotEmpty(performances);
            AssertPerformancesEachOccurOnceInResults(performances, results);
        }

        private void AssertPerformancesEachOccurOnceInResults(IEnumerable<PlayerInMatchStatisticsRecord> performances, IEnumerable<StatisticsResult<PlayerIdentityPerformance>> results)
        {
            Assert.Equal(performances.Count(), results.Count());
            foreach (var performance in performances)
            {
                var result = results.SingleOrDefault(x => x.Match?.MatchId == performance.MatchId &&
                                                        x.Player?.PlayerIdentities[0].PlayerIdentityId == performance.PlayerIdentityId &&
                                                        x.Result?.MatchTeamid == performance.MatchTeamId &&
                                                        x.Result.MatchInningsPair == performance.MatchInningsPair &&
                                                        x.Result.PlayerInningsNumber == performance.PlayerInningsNumber &&
                                                        x.Result.BattingPosition == performance.BattingPosition &&
                                                        x.Result.RunsScored == performance.RunsScored &&
                                                        x.Result.PlayerWasDismissed == performance.PlayerWasDismissed &&
                                                        x.Result.Wickets == performance.Wickets &&
                                                        x.Result.RunsConceded == performance.RunsConceded &&
                                                        x.Result.Catches == performance.Catches &&
                                                        x.Result.RunOuts == performance.RunOuts
                                                        );
                Assert.NotNull(result);

                var match = _databaseFixture.TestData.Matches.Single(x => x.MatchId == result!.Match?.MatchId);
                Assert.Equal(match.MatchName, result!.Match?.MatchName);
                Assert.Equal(match.StartTime.AccurateToTheMinute(), result.Match?.StartTime.AccurateToTheMinute());
                Assert.Equal(match.MatchRoute, result.Match?.MatchRoute);

                var playerIdentity = _databaseFixture.TestData.PlayerIdentities.Single(x => x.PlayerIdentityId == result.Player?.PlayerIdentities[0].PlayerIdentityId);
                Assert.Equal(playerIdentity.PlayerIdentityName, result.Player?.PlayerIdentities[0].PlayerIdentityName);
                Assert.Equal(playerIdentity.Player?.PlayerId, result.Player?.PlayerId);
                Assert.Equal(playerIdentity.Player?.PlayerRoute, result.Player?.PlayerRoute);
            }
        }

        [Fact]
        public async Task Read_performances_sorts_by_most_recent_first()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object);

            var results = await dataSource.ReadPlayerIdentityPerformances(filter).ConfigureAwait(false);

            DateTimeOffset? previousPerformanceStartTime = DateTimeOffset.MaxValue;
            foreach (var result in results)
            {
                Assert.True(result.Match?.StartTime <= previousPerformanceStartTime);
                previousPerformanceStartTime = result.Match?.StartTime;
            }
        }

        [Fact]
        public async Task Read_performances_pages_results()
        {
            const int pageSize = 10;
            var pageNumber = 1;
            var remaining = AllPlayerIdentityPerformances(_databaseFixture.TestData.Matches).Count();
            while (remaining > 0)
            {
                var filter = new StatisticsFilter { Paging = new Paging { PageNumber = pageNumber, PageSize = pageSize } };
                _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));
                var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object);
                var results = await dataSource.ReadPlayerIdentityPerformances(filter).ConfigureAwait(false);

                var expected = pageSize > remaining ? remaining : pageSize;
                Assert.Equal(expected, results.Count());

                pageNumber++;
                remaining -= pageSize;
            }
        }


        private static IEnumerable<PlayerInMatchStatisticsRecord> AllPlayerIdentityPerformances(IEnumerable<Stoolball.Matches.Match> matches)
        {
            var performances = new List<PlayerInMatchStatisticsRecord>();
            var statsBuilder = new PlayerInMatchStatisticsBuilder(new PlayerIdentityFinder(), new OversHelper());
            foreach (var match in matches)
            {
                performances.AddRange(statsBuilder.BuildStatisticsForMatch(match));
            }
            return performances;
        }
    }
}
