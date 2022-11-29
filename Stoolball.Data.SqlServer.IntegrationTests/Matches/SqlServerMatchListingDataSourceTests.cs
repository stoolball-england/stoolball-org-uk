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
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class SqlServerMatchListingDataSourceTests
    {
        private readonly SqlServerTestDataFixture _databaseFixture;

        public SqlServerMatchListingDataSourceTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_total_matches_supports_no_filter()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(null).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.MatchListings.Count, result);
        }

        [Fact]
        public async Task Read_total_matches_supports_only_matches_filter()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchFilter { IncludeMatches = true, IncludeTournaments = false }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.MatchListings.Count(x => x.MatchRoute.StartsWith("/matches/", StringComparison.OrdinalIgnoreCase)), result);
        }

        [Fact]
        public async Task Read_total_matches_supports_only_tournaments_filter()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchFilter { IncludeMatches = false, IncludeTournaments = true }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.MatchListings.Count(x => x.MatchRoute.StartsWith("/tournaments/", StringComparison.OrdinalIgnoreCase)), result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_by_match_type()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchFilter { MatchTypes = new List<MatchType> { MatchType.LeagueMatch } }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.MatchListings.Count(x => x.MatchRoute.StartsWith("/tournaments/", StringComparison.OrdinalIgnoreCase) || x.MatchType == MatchType.LeagueMatch), result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_by_player_type()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchFilter { PlayerTypes = new List<PlayerType> { PlayerType.Mixed } }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.MatchListings.Count(x => x.PlayerType == PlayerType.Mixed), result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_by_result_type()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchFilter { MatchResultTypes = new List<MatchResultType?> { MatchResultType.HomeWin } }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.MatchListings.Count(x => x.MatchResultType == MatchResultType.HomeWin), result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_by_null_result_type()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchFilter { MatchResultTypes = new List<MatchResultType?> { null } }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.MatchListings.Count(x => !x.MatchResultType.HasValue), result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_matches_by_team()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);
            var teamThatIsInATournamentAndAMatch = _databaseFixture.TestData.Teams.First(x =>
                _databaseFixture.TestData.Matches.SelectMany(m => m.Teams).Select(t => t.Team?.TeamId).Contains(x.TeamId) &&
                _databaseFixture.TestData.Tournaments.SelectMany(t => t.Teams).Select(t => t.Team?.TeamId).Contains(x.TeamId));
            var filter = new MatchFilter
            {
                IncludeMatches = true,
                IncludeTournaments = false,
                TeamIds = new List<Guid> { teamThatIsInATournamentAndAMatch.TeamId!.Value }
            };

            var result = await matchDataSource.ReadTotalMatches(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.Where(m => m.Tournament == null).Count(x => x.Teams.Select(x => x.Team?.TeamId).Contains(filter.TeamIds[0]));

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_matches_by_competition()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);
            var filter = new MatchFilter
            {
                IncludeMatches = true,
                IncludeTournaments = false,
                CompetitionIds = new List<Guid> { _databaseFixture.TestData.MatchInThePastWithFullDetails!.Season!.Competition!.CompetitionId!.Value }
            };

            var result = await matchDataSource.ReadTotalMatches(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.Where(m => m.Tournament == null).Count(x => x.Season?.Competition?.CompetitionId == filter.CompetitionIds[0]);

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_matches_by_season()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);
            var filter = new MatchFilter
            {
                IncludeMatches = true,
                IncludeTournaments = false,
                SeasonIds = new List<Guid> { _databaseFixture.TestData.MatchInThePastWithFullDetails!.Season!.SeasonId!.Value }
            };

            var result = await matchDataSource.ReadTotalMatches(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.Where(m => m.Tournament == null).Count(x => x.Season?.SeasonId == filter.SeasonIds[0]);

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_matches_by_match_location()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);
            var filter = new MatchFilter
            {
                IncludeMatches = true,
                IncludeTournaments = false,
                MatchLocationIds = new List<Guid> { _databaseFixture.TestData.MatchInThePastWithFullDetails!.MatchLocation!.MatchLocationId!.Value }
            };

            var result = await matchDataSource.ReadTotalMatches(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.Where(m => m.Tournament == null).Count(x => x.MatchLocation?.MatchLocationId == filter.MatchLocationIds[0]);

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_from_date()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchFilter { FromDate = DateTimeOffset.UtcNow }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.MatchListings.Count(x => x.StartTime >= DateTimeOffset.UtcNow), result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_before_date()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchFilter { UntilDate = DateTimeOffset.UtcNow }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.MatchListings.Count(x => x.StartTime <= DateTimeOffset.UtcNow), result);
        }

        [Fact]
        public async Task Read_total_matches_supports_including_tournament_matches()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchFilter { IncludeTournamentMatches = true }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.MatchListings.Count + _databaseFixture.TestData.TournamentMatchListings.Count, result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_by_tournament()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchFilter { TournamentId = _databaseFixture.TestData.TournamentInThePastWithFullDetails!.TournamentId }).ConfigureAwait(false);

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_tournaments_by_team()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);
            var filter = new MatchFilter
            {
                IncludeMatches = false,
                IncludeTournaments = true,
                TeamIds = new List<Guid> { _databaseFixture.TestData.TournamentInThePastWithFullDetails!.Teams.First().Team!.TeamId!.Value }
            };

            var result = await matchDataSource.ReadTotalMatches(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Tournaments.Count(x => x.Teams.Select(t => t.Team?.TeamId).Contains(filter.TeamIds[0]));

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_tournaments_by_competition()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);
            var filter = new MatchFilter
            {
                IncludeMatches = false,
                IncludeTournaments = true,
                CompetitionIds = new List<Guid> { _databaseFixture.TestData.TournamentInThePastWithFullDetails!.Seasons.First().Competition!.CompetitionId!.Value }
            };

            var result = await matchDataSource.ReadTotalMatches(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Tournaments.Count(x => x.Seasons.Select(s => s.Competition?.CompetitionId).Contains(filter.CompetitionIds[0]));

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_tournaments_by_season()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);
            var filter = new MatchFilter
            {
                IncludeMatches = false,
                IncludeTournaments = true,
                SeasonIds = new List<Guid> { _databaseFixture.TestData.TournamentInThePastWithFullDetails!.Seasons.First().SeasonId!.Value }
            };

            var result = await matchDataSource.ReadTotalMatches(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Tournaments.Count(x => x.Seasons.Select(s => s.SeasonId).Contains(filter.SeasonIds[0]));

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_tournaments_by_match_location()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);
            var filter = new MatchFilter
            {
                IncludeMatches = false,
                IncludeTournaments = true,
                MatchLocationIds = new List<Guid> { _databaseFixture.TestData.TournamentInThePastWithFullDetails!.TournamentLocation!.MatchLocationId!.Value }
            };

            var result = await matchDataSource.ReadTotalMatches(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Tournaments.Count(x => x.TournamentLocation?.MatchLocationId == filter.MatchLocationIds[0]);

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_match_listings_returns_match_fields()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(null, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.MatchListings.Count, results.Count);
            foreach (var listing in _databaseFixture.TestData.MatchListings.Where(x => x.MatchRoute.StartsWith("/matches/", StringComparison.OrdinalIgnoreCase)))
            {
                var result = results.SingleOrDefault(x => x.MatchRoute == listing.MatchRoute);
                Assert.NotNull(result);

                Assert.Equal(listing.MatchId, result!.MatchId);
                Assert.Equal(listing.MatchName, result.MatchName);
                Assert.Equal(listing.StartTime, result.StartTime);
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

            Assert.Equal(_databaseFixture.TestData.MatchListings.Count, results.Count);
            foreach (var listing in _databaseFixture.TestData.MatchListings.Where(x => x.MatchRoute.StartsWith("/matches/", StringComparison.OrdinalIgnoreCase) && x.Teams.Any()))
            {
                var result = results.SingleOrDefault(x => x.MatchRoute == listing.MatchRoute);
                Assert.NotNull(result);

                foreach (var team in listing.Teams)
                {
                    var resultTeam = result!.Teams.SingleOrDefault(x => x.MatchTeamId == team.MatchTeamId);
                    Assert.NotNull(resultTeam);

                    Assert.Equal(team.TeamRole, resultTeam!.TeamRole);
                    Assert.Equal(team.Team.TeamId, resultTeam.Team?.TeamId);
                }
            }
        }

        [Fact]
        public async Task Read_match_listings_returns_MatchInnings_with_runs_and_wickets_only_if_batting_team_is_not_null()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(null, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.MatchListings.Count, results.Count);
            foreach (var listing in _databaseFixture.TestData.MatchListings.Where(x => x.MatchRoute?.StartsWith("/matches/", StringComparison.OrdinalIgnoreCase) ?? false))
            {
                var result = results.SingleOrDefault(x => x.MatchRoute == listing.MatchRoute);
                Assert.NotNull(result);

                if (listing.MatchInnings.Any(x => x.BattingTeam != null))
                {
                    Assert.Equal(listing.MatchInnings.Count, result!.MatchInnings.Count);
                    foreach (var innings in listing.MatchInnings)
                    {
                        var resultInnings = result.MatchInnings.SingleOrDefault(x => x.MatchInningsId == innings.MatchInningsId);
                        Assert.NotNull(resultInnings);

                        Assert.Equal(innings.Runs, resultInnings!.Runs);
                        Assert.Equal(innings.Wickets, resultInnings.Wickets);
                    }
                }
                else
                {
                    Assert.Empty(result!.MatchInnings);
                }
            }
        }

        [Fact]
        public async Task Read_match_listings_returns_audit_dates_for_match()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(null, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            var result = results.SingleOrDefault(x => x.MatchId == _databaseFixture.TestData.MatchInThePastWithFullDetails!.MatchId);
            Assert.NotNull(result);
            Assert.Equal(_databaseFixture.TestData.MatchInThePastWithFullDetails!.History.First().AuditDate, result!.FirstAuditDate);
            Assert.Equal(_databaseFixture.TestData.MatchInThePastWithFullDetails.History.Last().AuditDate, result.LastAuditDate);
        }

        [Fact]
        public async Task Read_match_listings_returns_tournament_fields()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(null, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.MatchListings.Count, results.Count);
            foreach (var listing in _databaseFixture.TestData.MatchListings.Where(x => x.MatchRoute.StartsWith("/tournaments/", StringComparison.OrdinalIgnoreCase)))
            {
                var result = results.SingleOrDefault(x => x.MatchRoute == listing.MatchRoute);
                Assert.NotNull(result);

                Assert.Equal(listing.MatchId, result!.MatchId);
                Assert.Equal(listing.MatchName, result.MatchName);
                Assert.Equal(listing.StartTime, result.StartTime);
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

            Assert.Equal(_databaseFixture.TestData.MatchListings.Count, results.Count);
            foreach (var listing in _databaseFixture.TestData.MatchListings.Where(x => x.MatchLocation != null))
            {
                var result = results.SingleOrDefault(x => x.MatchRoute == listing.MatchRoute);
                Assert.NotNull(result?.MatchLocation);

                Assert.Equal(listing.MatchLocation.MatchLocationId, result!.MatchLocation!.MatchLocationId);
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

            Assert.Equal(_databaseFixture.TestData.MatchListings.Count, results.Count);
            foreach (var match in _databaseFixture.TestData.MatchListings.Where(x => x.MatchRoute.StartsWith("/tournaments/", StringComparison.OrdinalIgnoreCase)))
            {
                var result = results.SingleOrDefault(x => x.MatchRoute == match.MatchRoute);
                Assert.NotNull(result);
                Assert.Empty(result!.Teams);
            }
        }

        [Fact]
        public async Task Read_match_listings_returns_audit_dates_for_tournament()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(null, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            var result = results.SingleOrDefault(x => x.MatchId == _databaseFixture.TestData.TournamentInThePastWithFullDetails!.TournamentId);
            Assert.NotNull(result);
            Assert.Equal(_databaseFixture.TestData.TournamentInThePastWithFullDetails!.History.First().AuditDate, result!.FirstAuditDate);
            Assert.Equal(_databaseFixture.TestData.TournamentInThePastWithFullDetails.History.Last().AuditDate, result.LastAuditDate);
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

            var dataWithAuditHistory = _databaseFixture.TestData.Matches.Where(m => m.Tournament == null)
                .Where(x => x.History.Any()).Select(x => x.MatchId)
                .Union(_databaseFixture.TestData.Tournaments.Where(x => x.History.Any()).Select(x => x.TournamentId));
            var resultsWithAuditHistoryExpected = results.Where(x => dataWithAuditHistory.Contains(x.MatchId));

            Assert.True(resultsWithAuditHistoryExpected.Count() >= 2);
            DateTimeOffset? previousUpdate = DateTimeOffset.MaxValue;
            foreach (var result in resultsWithAuditHistoryExpected)
            {
                Assert.True(result.LastAuditDate <= previousUpdate);
                previousUpdate = result.LastAuditDate;
            }
        }

        [Fact]
        public async Task Read_match_listings_supports_no_filter()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(null, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            var missing = _databaseFixture.TestData.MatchListings.Where(x => !results.Select(r => r.MatchId).Contains(x.MatchId)).ToList();
            Assert.Equal(_databaseFixture.TestData.MatchListings.Count, results.Count);

            foreach (var listing in _databaseFixture.TestData.MatchListings)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.MatchRoute == listing.MatchRoute));
            }
        }

        [Fact]
        public async Task Read_match_listings_supports_only_matches_filter()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(new MatchFilter { IncludeMatches = true, IncludeTournaments = false }, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.MatchListings.Where(x => x.MatchRoute.StartsWith("/matches/", StringComparison.OrdinalIgnoreCase));
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

            var expected = _databaseFixture.TestData.MatchListings.Where(x => x.MatchRoute.StartsWith("/tournaments/", StringComparison.OrdinalIgnoreCase));
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

            var expected = _databaseFixture.TestData.MatchListings.Where(x => x.MatchRoute.StartsWith("/tournaments/", StringComparison.OrdinalIgnoreCase) || x.MatchType == MatchType.FriendlyMatch);
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

            var expected = _databaseFixture.TestData.MatchListings.Where(x => x.PlayerType == PlayerType.Mixed);
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

            var expected = _databaseFixture.TestData.MatchListings.Where(x => x.MatchResultType == MatchResultType.HomeWin);
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

            var expected = _databaseFixture.TestData.MatchListings.Where(x => !x.MatchResultType.HasValue);
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
            var teamThatIsInATournamentAndAMatch = _databaseFixture.TestData.Teams.First(x =>
                    _databaseFixture.TestData.Matches.SelectMany(m => m.Teams).Select(t => t.Team?.TeamId).Contains(x.TeamId) &&
                    _databaseFixture.TestData.Tournaments.SelectMany(t => t.Teams).Select(t => t.Team?.TeamId).Contains(x.TeamId));
            var filter = new MatchFilter { TeamIds = new List<Guid> { teamThatIsInATournamentAndAMatch.TeamId!.Value } };

            var results = await matchDataSource.ReadMatchListings(filter, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.MatchListings.Where(x => x.Teams.Select(x => x.Team?.TeamId).Contains(filter.TeamIds[0]));

            var missing = results.Where(x => !expected.ToList().Select(y => y.MatchId).Contains(x.MatchId)).ToList();

            Assert.Equal(expected.Count(), results.Count());
            foreach (var match in expected)
            {
                var matchFromResults = results.SingleOrDefault(x => x.MatchId == match.MatchId);
                Assert.NotNull(matchFromResults);
            }
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_matches_by_competition()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);
            var filter = new MatchFilter { CompetitionIds = new List<Guid> { _databaseFixture.TestData.MatchInThePastWithFullDetails!.Season!.Competition!.CompetitionId!.Value } };

            var results = await matchDataSource.ReadMatchListings(filter, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.Where(x => x.Tournament == null && x.Season?.Competition?.CompetitionId == filter.CompetitionIds[0]);

            Assert.Equal(expected.Count(), results.Count());
            foreach (var match in expected)
            {
                var matchFromResults = results.SingleOrDefault(x => x.MatchId == match.MatchId);
                Assert.NotNull(matchFromResults);
            }
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_matches_by_season()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);
            var filter = new MatchFilter { SeasonIds = new List<Guid> { _databaseFixture.TestData.MatchInThePastWithFullDetails!.Season!.SeasonId!.Value } };

            var results = await matchDataSource.ReadMatchListings(filter, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.MatchListings.Where(x =>
                 _databaseFixture.TestData.Matches.Where(m => m.Tournament == null && m.Season?.SeasonId == filter.SeasonIds[0]).Select(m => m.MatchId)
                 .Union(_databaseFixture.TestData.Tournaments.Where(t => t.Seasons.Select(s => s.SeasonId).Contains(filter.SeasonIds[0])).Select(t => t.TournamentId))
            .Contains(x.MatchId));

            Assert.Equal(expected.Count(), results.Count());
            foreach (var match in expected)
            {
                var matchFromResults = results.SingleOrDefault(x => x.MatchId == match.MatchId);
                Assert.NotNull(matchFromResults);
            }
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_matches_by_match_location()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);
            var filter = new MatchFilter { MatchLocationIds = new List<Guid> { _databaseFixture.TestData.MatchInThePastWithFullDetails!.MatchLocation!.MatchLocationId!.Value } };

            var results = await matchDataSource.ReadMatchListings(filter, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.MatchListings.Where(x => x.MatchLocation?.MatchLocationId == filter.MatchLocationIds[0]);

            Assert.Equal(expected.Count(), results.Count());
            foreach (var match in expected)
            {
                var matchFromResults = results.SingleOrDefault(x => x.MatchId == match.MatchId);
                Assert.NotNull(matchFromResults);
            }
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_from_date()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(new MatchFilter { FromDate = DateTimeOffset.UtcNow }, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.MatchListings.Where(x => x.StartTime >= DateTimeOffset.Now);
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

            var expected = _databaseFixture.TestData.MatchListings.Where(x => x.StartTime <= DateTimeOffset.Now);
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
            expected.AddRange(_databaseFixture.TestData.MatchListings);
            expected.AddRange(_databaseFixture.TestData.TournamentMatchListings);
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
                TournamentId = _databaseFixture.TestData.TournamentInThePastWithFullDetails!.TournamentId,
                IncludeTournamentMatches = true,
                IncludeTournaments = false,
            }, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.TournamentMatchListings.Where(x => x.MatchRoute != _databaseFixture.TestData.MatchInThePastWithFullDetailsAndTournament!.MatchRoute);
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
            var filter = new MatchFilter { TeamIds = new List<Guid> { _databaseFixture.TestData.TournamentInThePastWithFullDetails!.Teams.First().Team!.TeamId!.Value } };

            var results = await matchDataSource.ReadMatchListings(filter, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Tournaments.Where(x => x.Teams.Select(t => t.Team?.TeamId).Contains(filter.TeamIds[0]));

            Assert.Equal(expected.Count(), results.Where(x => x.MatchRoute!.StartsWith("/tournaments/")).Count());
            foreach (var tournament in expected)
            {
                var tournamentFromResults = results.SingleOrDefault(x => x.MatchId == tournament.TournamentId);
                Assert.NotNull(tournamentFromResults);
            }
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_tournaments_by_competition()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);
            var filter = new MatchFilter { CompetitionIds = new List<Guid> { _databaseFixture.TestData.TournamentInThePastWithFullDetails!.Seasons.First().Competition!.CompetitionId!.Value } };

            var results = await matchDataSource.ReadMatchListings(filter, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Tournaments.Where(x => x.Seasons.Select(s => s.Competition?.CompetitionId).Contains(filter.CompetitionIds[0]));

            Assert.Equal(expected.Count(), results.Where(x => x.MatchRoute!.StartsWith("/tournaments/")).Count());
            foreach (var tournament in expected)
            {
                var tournamentFromResults = results.SingleOrDefault(x => x.MatchId == tournament.TournamentId);
                Assert.NotNull(tournamentFromResults);
            }
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_tournaments_by_season()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);
            var filter = new MatchFilter { SeasonIds = new List<Guid> { _databaseFixture.TestData.TournamentInThePastWithFullDetails!.Seasons.First().SeasonId!.Value } };

            var results = await matchDataSource.ReadMatchListings(filter, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Tournaments.Where(x => x.Seasons.Select(s => s.SeasonId).Contains(filter.SeasonIds[0]));

            Assert.Equal(expected.Count(), results.Where(x => x.MatchRoute!.StartsWith("/tournaments/")).Count());
            foreach (var tournament in expected)
            {
                var tournamentFromResults = results.SingleOrDefault(x => x.MatchId == tournament.TournamentId);
                Assert.NotNull(tournamentFromResults);
            }
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_tournaments_by_match_location()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);
            var filter = new MatchFilter { MatchLocationIds = new List<Guid> { _databaseFixture.TestData.TournamentInThePastWithFullDetails!.TournamentLocation!.MatchLocationId!.Value } };

            var results = await matchDataSource.ReadMatchListings(filter, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Tournaments.Where(x => x.TournamentLocation?.MatchLocationId == filter.MatchLocationIds[0]);

            Assert.Equal(expected.Count(), results.Where(x => x.MatchRoute!.StartsWith("/tournaments/")).Count());
            foreach (var tournament in expected)
            {
                var tournamentFromResults = results.SingleOrDefault(x => x.MatchId == tournament.TournamentId);
                Assert.NotNull(tournamentFromResults);
            }
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_tournaments_by_tournamentid()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(new MatchFilter { TournamentId = _databaseFixture.TestData.TournamentInThePastWithFullDetails!.TournamentId }, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            Assert.Single(results);
            Assert.Equal(_databaseFixture.TestData.TournamentInThePastWithFullDetails.TournamentRoute, results.Single().MatchRoute);
        }

        [Fact]
        public async Task Read_match_listings_pages_results()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            const int pageSize = 10;
            var pageNumber = 1;
            var remaining = _databaseFixture.TestData.MatchListings.Count;
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