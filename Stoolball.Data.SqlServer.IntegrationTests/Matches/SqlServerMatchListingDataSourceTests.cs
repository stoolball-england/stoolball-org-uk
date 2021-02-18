using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stoolball.Matches;
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

        private static DateTime DateTimeOffsetAccurateToTheMinute(DateTimeOffset dateTime)
        {
            return dateTime.Date.AddHours(dateTime.Hour).AddMinutes(dateTime.Minute);
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

            var result = await matchDataSource.ReadTotalMatches(new MatchQuery { IncludeMatches = true, IncludeTournaments = false }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchListings.Count(x => x.MatchRoute.StartsWith("/matches/", StringComparison.OrdinalIgnoreCase)), result);
        }

        [Fact]
        public async Task Read_total_matches_supports_only_tournaments_filter()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchQuery { IncludeMatches = false, IncludeTournaments = true }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchListings.Count(x => x.MatchRoute.StartsWith("/tournaments/", StringComparison.OrdinalIgnoreCase)), result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_by_match_type()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchQuery { MatchTypes = new List<MatchType> { MatchType.LeagueMatch } }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchListings.Count(x => x.MatchRoute.StartsWith("/tournaments/", StringComparison.OrdinalIgnoreCase) || x.MatchType == MatchType.LeagueMatch), result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_matches_by_team()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchQuery { TeamIds = new List<Guid> { _databaseFixture.MatchInThePastWithFullDetails.Teams.First().Team.TeamId.Value } }).ConfigureAwait(false);

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_matches_by_competition()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchQuery { CompetitionIds = new List<Guid> { _databaseFixture.MatchInThePastWithFullDetails.Season.Competition.CompetitionId.Value } }).ConfigureAwait(false);

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_matches_by_season()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchQuery { SeasonIds = new List<Guid> { _databaseFixture.MatchInThePastWithFullDetails.Season.SeasonId.Value } }).ConfigureAwait(false);

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_matches_by_match_location()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchQuery { MatchLocationIds = new List<Guid> { _databaseFixture.MatchInThePastWithFullDetails.MatchLocation.MatchLocationId.Value } }).ConfigureAwait(false);

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_from_date()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchQuery { FromDate = DateTimeOffset.UtcNow }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchListings.Count(x => x.StartTime >= DateTimeOffset.UtcNow), result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_before_date()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchQuery { UntilDate = DateTimeOffset.UtcNow }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchListings.Count(x => x.StartTime <= DateTimeOffset.UtcNow), result);
        }

        [Fact]
        public async Task Read_total_matches_supports_including_tournament_matches()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchQuery { IncludeTournamentMatches = true }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchListings.Count + _databaseFixture.TournamentMatchListings.Count, result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_by_tournament()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchQuery { TournamentId = _databaseFixture.TournamentInThePastWithFullDetails.TournamentId }).ConfigureAwait(false);

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_tournaments_by_team()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchQuery { TeamIds = new List<Guid> { _databaseFixture.TournamentInThePastWithFullDetails.Teams.First().Team.TeamId.Value } }).ConfigureAwait(false);

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_tournaments_by_competition()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchQuery { CompetitionIds = new List<Guid> { _databaseFixture.TournamentInThePastWithFullDetails.Seasons.First().Competition.CompetitionId.Value } }).ConfigureAwait(false);

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_tournaments_by_season()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchQuery { SeasonIds = new List<Guid> { _databaseFixture.TournamentInThePastWithFullDetails.Seasons.First().SeasonId.Value } }).ConfigureAwait(false);

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_tournaments_by_match_location()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await matchDataSource.ReadTotalMatches(new MatchQuery { MatchLocationIds = new List<Guid> { _databaseFixture.TournamentInThePastWithFullDetails.TournamentLocation.MatchLocationId.Value } }).ConfigureAwait(false);

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task Read_match_listings_returns_match_fields()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(null).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchListings.Count, results.Count);
            foreach (var listing in _databaseFixture.MatchListings.Where(x => x.MatchRoute.StartsWith("/matches/", StringComparison.OrdinalIgnoreCase)))
            {
                var result = results.SingleOrDefault(x => x.MatchRoute == listing.MatchRoute);
                Assert.NotNull(result);

                Assert.Equal(listing.MatchName, result.MatchName);
                Assert.Equal(DateTimeOffsetAccurateToTheMinute(listing.StartTime), DateTimeOffsetAccurateToTheMinute(result.StartTime));
                Assert.Equal(listing.StartTimeIsKnown, result.StartTimeIsKnown);
                Assert.Equal(listing.MatchType, result.MatchType);
                Assert.Equal(listing.PlayerType, result.PlayerType);
                Assert.Equal(listing.MatchResultType, result.MatchResultType);
            }
        }

        [Fact]
        public async Task Read_match_listings_returns_match_teams()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(null).ConfigureAwait(false);

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

            var results = await matchDataSource.ReadMatchListings(null).ConfigureAwait(false);

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
        public async Task Read_match_listings_returns_tournament_fields()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(null).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchListings.Count, results.Count);
            foreach (var listing in _databaseFixture.MatchListings.Where(x => x.MatchRoute.StartsWith("/tournaments/", StringComparison.OrdinalIgnoreCase)))
            {
                var result = results.SingleOrDefault(x => x.MatchRoute == listing.MatchRoute);
                Assert.NotNull(result);

                Assert.Equal(listing.MatchName, result.MatchName);
                Assert.Equal(DateTimeOffsetAccurateToTheMinute(listing.StartTime), DateTimeOffsetAccurateToTheMinute(result.StartTime));
                Assert.Equal(listing.StartTimeIsKnown, result.StartTimeIsKnown);
                Assert.Equal(listing.PlayerType, result.PlayerType);
                Assert.Equal(listing.TournamentQualificationType, result.TournamentQualificationType);
                Assert.Equal(listing.SpacesInTournament, result.SpacesInTournament);
            }
        }

        [Fact]
        public async Task Read_match_listings_does_not_return_tournament_teams()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(null).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchListings.Count, results.Count);
            foreach (var match in _databaseFixture.MatchListings.Where(x => x.MatchRoute.StartsWith("/tournaments/", StringComparison.OrdinalIgnoreCase)))
            {
                var result = results.SingleOrDefault(x => x.MatchRoute == match.MatchRoute);
                Assert.NotNull(result);
                Assert.Empty(result.Teams);
            }
        }

        [Fact]
        public async Task Read_match_listings_sorts_by_start_time()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(null).ConfigureAwait(false);

            var previousStartTime = DateTimeOffset.MinValue;
            foreach (var result in results)
            {
                Assert.True(result.StartTime >= previousStartTime);
                previousStartTime = result.StartTime;
            }
        }

        [Fact]
        public async Task Read_match_listings_supports_no_filter()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(null).ConfigureAwait(false);

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

            var results = await matchDataSource.ReadMatchListings(new MatchQuery { IncludeMatches = true, IncludeTournaments = false }).ConfigureAwait(false);

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

            var results = await matchDataSource.ReadMatchListings(new MatchQuery { IncludeMatches = false, IncludeTournaments = true }).ConfigureAwait(false);

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

            var results = await matchDataSource.ReadMatchListings(new MatchQuery { MatchTypes = new List<MatchType> { MatchType.FriendlyMatch } }).ConfigureAwait(false);

            var expected = _databaseFixture.MatchListings.Where(x => x.MatchRoute.StartsWith("/tournaments/", StringComparison.OrdinalIgnoreCase) || x.MatchType == MatchType.FriendlyMatch);
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

            var results = await matchDataSource.ReadMatchListings(new MatchQuery { TeamIds = new List<Guid> { _databaseFixture.MatchInThePastWithFullDetails.Teams.First().Team.TeamId.Value } }).ConfigureAwait(false);

            Assert.Single(results);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute, results.Single().MatchRoute);
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_matches_by_competition()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(new MatchQuery { CompetitionIds = new List<Guid> { _databaseFixture.MatchInThePastWithFullDetails.Season.Competition.CompetitionId.Value } }).ConfigureAwait(false);

            Assert.Single(results);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute, results.Single().MatchRoute);
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_matches_by_season()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(new MatchQuery { SeasonIds = new List<Guid> { _databaseFixture.MatchInThePastWithFullDetails.Season.SeasonId.Value } }).ConfigureAwait(false);

            Assert.Single(results);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute, results.Single().MatchRoute);
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_matches_by_match_location()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(new MatchQuery { MatchLocationIds = new List<Guid> { _databaseFixture.MatchInThePastWithFullDetails.MatchLocation.MatchLocationId.Value } }).ConfigureAwait(false);

            Assert.Single(results);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute, results.Single().MatchRoute);
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_from_date()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(new MatchQuery { FromDate = DateTimeOffset.UtcNow }).ConfigureAwait(false);

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

            var results = await matchDataSource.ReadMatchListings(new MatchQuery { UntilDate = DateTimeOffset.UtcNow }).ConfigureAwait(false);

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

            var results = await matchDataSource.ReadMatchListings(new MatchQuery { IncludeTournamentMatches = true }).ConfigureAwait(false);

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

            var results = await matchDataSource.ReadMatchListings(new MatchQuery
            {
                TournamentId = _databaseFixture.TournamentInThePastWithFullDetails.TournamentId,
                IncludeTournamentMatches = true,
                IncludeTournaments = false,
            }).ConfigureAwait(false);

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

            var results = await matchDataSource.ReadMatchListings(new MatchQuery { TeamIds = new List<Guid> { _databaseFixture.TournamentInThePastWithFullDetails.Teams.First().Team.TeamId.Value } }).ConfigureAwait(false);

            Assert.Single(results);
            Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.TournamentRoute, results.Single().MatchRoute);
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_tournaments_by_competition()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(new MatchQuery { CompetitionIds = new List<Guid> { _databaseFixture.TournamentInThePastWithFullDetails.Seasons.First().Competition.CompetitionId.Value } }).ConfigureAwait(false);

            Assert.Single(results);
            Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.TournamentRoute, results.Single().MatchRoute);
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_tournaments_by_season()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(new MatchQuery { SeasonIds = new List<Guid> { _databaseFixture.TournamentInThePastWithFullDetails.Seasons.First().SeasonId.Value } }).ConfigureAwait(false);

            Assert.Single(results);
            Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.TournamentRoute, results.Single().MatchRoute);
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_tournaments_by_match_location()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(new MatchQuery { MatchLocationIds = new List<Guid> { _databaseFixture.TournamentInThePastWithFullDetails.TournamentLocation.MatchLocationId.Value } }).ConfigureAwait(false);

            Assert.Single(results);
            Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.TournamentRoute, results.Single().MatchRoute);
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_tournaments_by_tournamentid()
        {
            var matchDataSource = new SqlServerMatchListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await matchDataSource.ReadMatchListings(new MatchQuery { TournamentId = _databaseFixture.TournamentInThePastWithFullDetails.TournamentId }).ConfigureAwait(false);

            Assert.Single(results);
            Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.TournamentRoute, results.Single().MatchRoute);
        }
    }
}