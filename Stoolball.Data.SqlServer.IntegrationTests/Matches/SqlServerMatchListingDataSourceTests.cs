using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Teams;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Matches
{
    [Collection(IntegrationTestConstants.DataSourceIntegrationTestCollection)]
    public class SqlServerMatchListingDataSourceTests
    {
        private readonly SqlServerDataSourceFixture _databaseFixture;

        public SqlServerMatchListingDataSourceTests(SqlServerDataSourceFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_total_matches_supports_no_filter()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(null).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchListings.Count, result);
        }

        [Fact]
        public async Task Read_total_matches_supports_only_matches_filter()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchFilter { IncludeMatches = true, IncludeTournaments = false }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchListings.Count(x => x.MatchRoute.StartsWith("/matches/", StringComparison.OrdinalIgnoreCase)), result);
        }

        [Fact]
        public async Task Read_total_matches_supports_only_tournaments_filter()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchFilter { IncludeMatches = false, IncludeTournaments = true }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchListings.Count(x => x.MatchRoute.StartsWith("/tournaments/", StringComparison.OrdinalIgnoreCase)), result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_by_match_type()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchFilter { MatchTypes = new List<MatchType> { MatchType.LeagueMatch } }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchListings.Count(x => x.MatchRoute.StartsWith("/tournaments/", StringComparison.OrdinalIgnoreCase) || x.MatchType == MatchType.LeagueMatch), result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_by_player_type()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchFilter { PlayerTypes = new List<PlayerType> { PlayerType.Mixed } }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchListings.Count(x => x.PlayerType == PlayerType.Mixed), result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_by_result_type()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchFilter { MatchResultTypes = new List<MatchResultType?> { MatchResultType.HomeWin } }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchListings.Count(x => x.MatchResultType == MatchResultType.HomeWin), result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_by_null_result_type()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchFilter { MatchResultTypes = new List<MatchResultType?> { null } }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchListings.Count(x => !x.MatchResultType.HasValue), result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_matches_by_team()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchFilter { TeamIds = new List<Guid> { _databaseFixture.MatchInThePastWithFullDetails.Teams.First().Team.TeamId.Value } }).ConfigureAwait(false);

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_matches_by_competition()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchFilter { CompetitionIds = new List<Guid> { _databaseFixture.MatchInThePastWithFullDetails.Season.Competition.CompetitionId.Value } }).ConfigureAwait(false);

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_matches_by_season()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchFilter { SeasonIds = new List<Guid> { _databaseFixture.MatchInThePastWithFullDetails.Season.SeasonId.Value } }).ConfigureAwait(false);

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_matches_by_match_location()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchFilter { MatchLocationIds = new List<Guid> { _databaseFixture.MatchInThePastWithFullDetails.MatchLocation.MatchLocationId.Value } }).ConfigureAwait(false);

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_from_date()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchFilter { FromDate = DateTimeOffset.UtcNow }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchListings.Count(x => x.StartTime >= DateTimeOffset.UtcNow), result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_before_date()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchFilter { UntilDate = DateTimeOffset.UtcNow }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchListings.Count(x => x.StartTime <= DateTimeOffset.UtcNow), result);
        }

        [Fact]
        public async Task Read_total_matches_supports_including_tournament_matches()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchFilter { IncludeTournamentMatches = true }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchListings.Count + _databaseFixture.TournamentMatchListings.Count, result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_by_tournament()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchFilter { TournamentId = _databaseFixture.TournamentInThePastWithFullDetails.TournamentId }).ConfigureAwait(false);

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_tournaments_by_team()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchFilter { TeamIds = new List<Guid> { _databaseFixture.TournamentInThePastWithFullDetails.Teams.First().Team.TeamId.Value } }).ConfigureAwait(false);

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_tournaments_by_competition()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchFilter { CompetitionIds = new List<Guid> { _databaseFixture.TournamentInThePastWithFullDetails.Seasons.First().Competition.CompetitionId.Value } }).ConfigureAwait(false);

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_tournaments_by_season()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchFilter { SeasonIds = new List<Guid> { _databaseFixture.TournamentInThePastWithFullDetails.Seasons.First().SeasonId.Value } }).ConfigureAwait(false);

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_tournaments_by_match_location()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchFilter { MatchLocationIds = new List<Guid> { _databaseFixture.TournamentInThePastWithFullDetails.TournamentLocation.MatchLocationId.Value } }).ConfigureAwait(false);

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task Read_match_listings_returns_match_fields()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(null, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchListings.Count, results.Count);
            foreach (var listing in _databaseFixture.MatchListings.Where(x => x.MatchRoute.StartsWith("/matches/", StringComparison.OrdinalIgnoreCase)))
            {
                var result = results.SingleOrDefault(x => x.MatchRoute == listing.MatchRoute);
                Assert.NotNull(result);

                Assert.Equal(listing.MatchId, result.MatchId);
                Assert.Equal(listing.MatchName, result.MatchName);
                Assert.Equal(listing.StartTime.AccurateToTheMinute(), result.StartTime.AccurateToTheMinute());
                Assert.Equal(listing.StartTimeIsKnown, result.StartTimeIsKnown);
                Assert.Equal(listing.MatchType, result.MatchType);
                Assert.Equal(listing.PlayerType, result.PlayerType);
                Assert.Equal(listing.PlayersPerTeam, result.PlayersPerTeam);
                Assert.Equal(listing.MatchResultType, result.MatchResultType);
            }
        }

        [Fact]
        public async Task Read_match_listings_returns_match_teams()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(null, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchListings.Count, results.Count);
            foreach (var listing in _databaseFixture.MatchListings.Where(x => x.MatchRoute.StartsWith("/matches/", StringComparison.OrdinalIgnoreCase) && x.Teams.Any()))
            {
                var result = results.SingleOrDefault(x => x.MatchRoute == listing.MatchRoute);
                Assert.NotNull(result);

                foreach (var team in listing.Teams)
                {
                    var resultTeam = result.Teams.SingleOrDefault(x => x.MatchTeamId == team.MatchTeamId);
                    Assert.NotNull(resultTeam);

                    Assert.Equal(team.TeamRole, resultTeam.TeamRole);
                    Assert.Equal(team.Team.TeamId, resultTeam.Team.TeamId);
                }
            }
        }

        [Fact]
        public async Task Read_match_listings_returns_MatchInnings_with_runs_and_wickets_only_if_not_null()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(null, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchListings.Count, results.Count);
            foreach (var listing in _databaseFixture.MatchListings.Where(x => x.MatchRoute.StartsWith("/matches/", StringComparison.OrdinalIgnoreCase)))
            {
                var result = results.SingleOrDefault(x => x.MatchRoute == listing.MatchRoute);
                Assert.NotNull(result);

                if (listing.MatchInnings.Any(x => x.Runs.HasValue || x.Wickets.HasValue))
                {
                    Assert.Equal(listing.MatchInnings.Count, result.MatchInnings.Count);
                    foreach (var innings in listing.MatchInnings)
                    {
                        var resultInnings = result.MatchInnings.SingleOrDefault(x => x.MatchInningsId == innings.MatchInningsId);
                        Assert.NotNull(resultInnings);

                        Assert.Equal(innings.Runs, resultInnings.Runs);
                        Assert.Equal(innings.Wickets, resultInnings.Wickets);
                    }
                }
                else
                {
                    Assert.Empty(result.MatchInnings);
                }
            }
        }

        [Fact]
        public async Task Read_match_listings_returns_audit_dates_for_match()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(null, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            var result = results.SingleOrDefault(x => x.MatchId == _databaseFixture.MatchInThePastWithFullDetails.MatchId);
            Assert.NotNull(result);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.History.First().AuditDate.AccurateToTheMinute(), result.FirstAuditDate.Value.AccurateToTheMinute());
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.History.Last().AuditDate.AccurateToTheMinute(), result.LastAuditDate.Value.AccurateToTheMinute());
        }

        [Fact]
        public async Task Read_match_listings_returns_tournament_fields()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(null, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchListings.Count, results.Count);
            foreach (var listing in _databaseFixture.MatchListings.Where(x => x.MatchRoute.StartsWith("/tournaments/", StringComparison.OrdinalIgnoreCase)))
            {
                var result = results.SingleOrDefault(x => x.MatchRoute == listing.MatchRoute);
                Assert.NotNull(result);

                Assert.Equal(listing.MatchId, result.MatchId);
                Assert.Equal(listing.MatchName, result.MatchName);
                Assert.Equal(listing.StartTime.AccurateToTheMinute(), result.StartTime.AccurateToTheMinute());
                Assert.Equal(listing.StartTimeIsKnown, result.StartTimeIsKnown);
                Assert.Equal(listing.PlayerType, result.PlayerType);
                Assert.Equal(listing.PlayersPerTeam, result.PlayersPerTeam);
                Assert.Equal(listing.TournamentQualificationType, result.TournamentQualificationType);
                Assert.Equal(listing.SpacesInTournament, result.SpacesInTournament);
            }
        }

        [Fact]
        public async Task Read_match_listings_returns_match_location()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(null, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchListings.Count, results.Count);
            foreach (var listing in _databaseFixture.MatchListings.Where(x => x.MatchLocation != null))
            {
                var result = results.SingleOrDefault(x => x.MatchRoute == listing.MatchRoute);
                Assert.NotNull(result);

                Assert.Equal(listing.MatchLocation.MatchLocationId, result.MatchLocation.MatchLocationId);
                Assert.Equal(listing.MatchLocation.SecondaryAddressableObjectName, result.MatchLocation.SecondaryAddressableObjectName);
                Assert.Equal(listing.MatchLocation.PrimaryAddressableObjectName, result.MatchLocation.PrimaryAddressableObjectName);
                Assert.Equal(listing.MatchLocation.Locality, result.MatchLocation.Locality);
                Assert.Equal(listing.MatchLocation.Town, result.MatchLocation.Town);
                Assert.Equal(listing.MatchLocation.Latitude, result.MatchLocation.Latitude);
                Assert.Equal(listing.MatchLocation.Longitude, result.MatchLocation.Longitude);
            }
        }

        [Fact]
        public async Task Read_match_listings_does_not_return_tournament_teams()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(null, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchListings.Count, results.Count);
            foreach (var match in _databaseFixture.MatchListings.Where(x => x.MatchRoute.StartsWith("/tournaments/", StringComparison.OrdinalIgnoreCase)))
            {
                var result = results.SingleOrDefault(x => x.MatchRoute == match.MatchRoute);
                Assert.NotNull(result);
                Assert.Empty(result.Teams);
            }
        }

        [Fact]
        public async Task Read_match_listings_returns_audit_dates_for_tournament()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(null, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            var result = results.SingleOrDefault(x => x.MatchId == _databaseFixture.TournamentInThePastWithFullDetails.TournamentId);
            Assert.NotNull(result);
            Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.History.First().AuditDate.AccurateToTheMinute(), result.FirstAuditDate.Value.AccurateToTheMinute());
            Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.History.Last().AuditDate.AccurateToTheMinute(), result.LastAuditDate.Value.AccurateToTheMinute());
        }

        [Fact]
        public async Task Read_match_listings_sorts_by_start_time()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(null, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            var previousStartTime = DateTimeOffset.MinValue;
            foreach (var result in results)
            {
                Assert.True(result.StartTime >= previousStartTime);
                previousStartTime = result.StartTime;
            }
        }

        [Fact]
        public async Task Read_match_listings_supports_sort_by_last_audit()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(null, MatchSortOrder.LatestUpdateFirst).ConfigureAwait(false);

            var dataWithAuditHistory = _databaseFixture.Matches.Where(x => x.History.Any()).Select(x => x.MatchId.Value).Union(new[] { _databaseFixture.TournamentInThePastWithFullDetails.TournamentId.Value });
            var resultsWithAuditHistoryExpected = results.Where(x => dataWithAuditHistory.Contains(x.MatchId));

            var previousUpdate = DateTimeOffset.MaxValue;
            foreach (var result in resultsWithAuditHistoryExpected)
            {
                Assert.True(result.LastAuditDate <= previousUpdate);
                previousUpdate = result.LastAuditDate.Value;
            }
        }

        [Fact]
        public async Task Read_match_listings_supports_no_filter()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(null, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchListings.Count, results.Count);
            foreach (var listing in _databaseFixture.MatchListings)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.MatchRoute == listing.MatchRoute));
            }
        }

        [Fact]
        public async Task Read_match_listings_supports_only_matches_filter()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(new MatchFilter { IncludeMatches = true, IncludeTournaments = false }, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.MatchListings.Where(x => x.MatchRoute.StartsWith("/matches/", StringComparison.OrdinalIgnoreCase));
            Assert.Equal(expected.Count(), results.Count);
            foreach (var listing in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.MatchRoute == listing.MatchRoute));
            }
        }

        [Fact]
        public async Task Read_match_listings_supports_only_tournaments_filter()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(new MatchFilter { IncludeMatches = false, IncludeTournaments = true }, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.MatchListings.Where(x => x.MatchRoute.StartsWith("/tournaments/", StringComparison.OrdinalIgnoreCase));
            Assert.Equal(expected.Count(), results.Count);
            foreach (var listing in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.MatchRoute == listing.MatchRoute));
            }
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_by_match_type()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(new MatchFilter { MatchTypes = new List<MatchType> { MatchType.FriendlyMatch } }, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.MatchListings.Where(x => x.MatchRoute.StartsWith("/tournaments/", StringComparison.OrdinalIgnoreCase) || x.MatchType == MatchType.FriendlyMatch);
            Assert.Equal(expected.Count(), results.Count);
            foreach (var listing in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.MatchRoute == listing.MatchRoute));
            }
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_by_player_type()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(new MatchFilter { PlayerTypes = new List<PlayerType> { PlayerType.Mixed } }, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.MatchListings.Where(x => x.PlayerType == PlayerType.Mixed);
            Assert.Equal(expected.Count(), results.Count);
            foreach (var listing in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.MatchRoute == listing.MatchRoute));
            }
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_by_result_type()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(new MatchFilter { MatchResultTypes = new List<MatchResultType?> { MatchResultType.HomeWin } }, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.MatchListings.Where(x => x.MatchResultType == MatchResultType.HomeWin);
            Assert.Equal(expected.Count(), results.Count);
            foreach (var listing in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.MatchRoute == listing.MatchRoute));
            }
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_by_null_result_type()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(new MatchFilter { MatchResultTypes = new List<MatchResultType?> { null } }, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.MatchListings.Where(x => !x.MatchResultType.HasValue);
            Assert.Equal(expected.Count(), results.Count);
            foreach (var listing in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.MatchRoute == listing.MatchRoute));
            }
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_matches_by_team()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(new MatchFilter { TeamIds = new List<Guid> { _databaseFixture.MatchInThePastWithFullDetails.Teams.First().Team.TeamId.Value } }, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            Assert.Single(results);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute, results.Single().MatchRoute);
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_matches_by_competition()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(new MatchFilter { CompetitionIds = new List<Guid> { _databaseFixture.MatchInThePastWithFullDetails.Season.Competition.CompetitionId.Value } }, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            Assert.Single(results);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute, results.Single().MatchRoute);
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_matches_by_season()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(new MatchFilter { SeasonIds = new List<Guid> { _databaseFixture.MatchInThePastWithFullDetails.Season.SeasonId.Value } }, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            Assert.Single(results);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute, results.Single().MatchRoute);
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_matches_by_match_location()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(new MatchFilter { MatchLocationIds = new List<Guid> { _databaseFixture.MatchInThePastWithFullDetails.MatchLocation.MatchLocationId.Value } }, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            Assert.Single(results);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute, results.Single().MatchRoute);
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_from_date()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(new MatchFilter { FromDate = DateTimeOffset.UtcNow }, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.MatchListings.Where(x => x.StartTime >= DateTimeOffset.Now);
            Assert.Equal(expected.Count(), results.Count);
            foreach (var listing in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.MatchRoute == listing.MatchRoute));
            }
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_before_date()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(new MatchFilter { UntilDate = DateTimeOffset.UtcNow }, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.MatchListings.Where(x => x.StartTime <= DateTimeOffset.Now);
            Assert.Equal(expected.Count(), results.Count);
            foreach (var listing in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.MatchRoute == listing.MatchRoute));
            }
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_tournament_matches()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(new MatchFilter { IncludeTournamentMatches = true }, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            var expected = new List<MatchListing>();
            expected.AddRange(_databaseFixture.MatchListings);
            expected.AddRange(_databaseFixture.TournamentMatchListings);
            Assert.Equal(expected.Count, results.Count);
            foreach (var listing in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.MatchRoute == listing.MatchRoute));
            }
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_by_tournament_for_tournament_matches()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(new MatchFilter
            {
                TournamentId = _databaseFixture.TournamentInThePastWithFullDetails.TournamentId,
                IncludeTournamentMatches = true,
                IncludeTournaments = false,
            }, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.TournamentMatchListings.Where(x => x.MatchRoute != _databaseFixture.MatchInThePastWithFullDetailsAndTournament.MatchRoute);
            Assert.Equal(expected.Count(), results.Count);
            foreach (var listing in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.MatchRoute == listing.MatchRoute));
            }
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_tournaments_by_team()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(new MatchFilter { TeamIds = new List<Guid> { _databaseFixture.TournamentInThePastWithFullDetails.Teams.First().Team.TeamId.Value } }, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            Assert.Single(results);
            Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.TournamentRoute, results.Single().MatchRoute);
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_tournaments_by_competition()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(new MatchFilter { CompetitionIds = new List<Guid> { _databaseFixture.TournamentInThePastWithFullDetails.Seasons.First().Competition.CompetitionId.Value } }, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            Assert.Single(results);
            Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.TournamentRoute, results.Single().MatchRoute);
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_tournaments_by_season()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(new MatchFilter { SeasonIds = new List<Guid> { _databaseFixture.TournamentInThePastWithFullDetails.Seasons.First().SeasonId.Value } }, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            Assert.Single(results);
            Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.TournamentRoute, results.Single().MatchRoute);
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_tournaments_by_match_location()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(new MatchFilter { MatchLocationIds = new List<Guid> { _databaseFixture.TournamentInThePastWithFullDetails.TournamentLocation.MatchLocationId.Value } }, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            Assert.Single(results);
            Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.TournamentRoute, results.Single().MatchRoute);
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_tournaments_by_tournamentid()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(new MatchFilter { TournamentId = _databaseFixture.TournamentInThePastWithFullDetails.TournamentId }, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            Assert.Single(results);
            Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.TournamentRoute, results.Single().MatchRoute);
        }

        [Fact]
        public async Task Read_match_listings_pages_results()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            const int pageSize = 10;
            var pageNumber = 1;
            var remaining = _databaseFixture.MatchListings.Count;
            while (remaining > 0)
            {
                var result = await matchDataSource.ReadMatchListings(new MatchFilter { Paging = new Paging { PageNumber = pageNumber, PageSize = pageSize } }, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

                var expected = pageSize > remaining ? remaining : pageSize;
                Assert.Equal(expected, result.Count);

                pageNumber++;
                remaining -= pageSize;
            }
        }
    }
}