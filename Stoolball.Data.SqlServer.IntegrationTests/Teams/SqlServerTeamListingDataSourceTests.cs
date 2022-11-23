using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Navigation;
using Stoolball.Teams;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Teams
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class SqlServerTeamListingDataSourceTests
    {
        private readonly SqlServerTestDataFixture _databaseFixture;

        public SqlServerTeamListingDataSourceTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }
        private SqlServerTeamListingDataSource CreateDataSource()
        {
            return new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
        }

        [Fact]
        public async Task Read_total_teams_supports_no_filter()
        {
            var teamDataSource = CreateDataSource();

            var result = await teamDataSource.ReadTotalTeams(null).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.TeamListings.Count, result);
        }

        [Fact]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_team_name()
        {
            var teamDataSource = CreateDataSource();
            var query = new TeamListingFilter { Query = _databaseFixture.TestData.TeamWithFullDetails!.TeamName!.ToUpperInvariant() };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.TeamListings.Count(x => x.ClubOrTeamName?.Contains(query.Query, StringComparison.OrdinalIgnoreCase) ?? false), result);
        }

        [Fact]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_team_name_where_the_club_name_is_different()
        {
            var team = _databaseFixture.TestData.Clubs
                .Where(x => x.Teams.Count > 1)
                .SelectMany(x => x.Teams).Where(x => !string.IsNullOrEmpty(x.TeamName))
                .First(x => !x.Club?.ClubName?.Contains(x.TeamName!, StringComparison.OrdinalIgnoreCase) ?? false);
            var teamDataSource = CreateDataSource();
            var query = new TeamListingFilter { Query = team.TeamName!.ToUpperInvariant() };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            var matchedByClubName = _databaseFixture.TestData.Clubs.Where(x => x.ClubName.Contains(query.Query, StringComparison.OrdinalIgnoreCase));
            var matchedByTeamName = _databaseFixture.TestData.Teams.Where(x => x.TeamName.Contains(query.Query, StringComparison.OrdinalIgnoreCase) && (x.Club == null || !matchedByClubName.Select(c => x.Club.ClubId).Contains(x.Club.ClubId)));
            Assert.Equal(matchedByClubName.Count() + matchedByTeamName.Count(x => x.Club == null) + matchedByTeamName.Where(x => x.Club != null).Select(x => x.Club?.ClubId).OfType<Guid>().Distinct().Count(), result);
        }

        [Fact]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_player_type()
        {
            var teamDataSource = CreateDataSource();
            var query = new TeamListingFilter { Query = "LaDiEs" };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.TeamListings.Count(x => x.PlayerTypes.Contains(PlayerType.Ladies)), result);
        }

        [Fact]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_team_locality()
        {
            var teamDataSource = CreateDataSource();
            var query = new TeamListingFilter { Query = _databaseFixture.TestData.TeamWithFullDetails!.MatchLocations.First().Locality!.ToUpperInvariant() };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.TeamListings.Count(x => x.MatchLocations.Any(ml => ml.Locality?.Contains(query.Query, StringComparison.OrdinalIgnoreCase) ?? false)), result);
        }

        [Fact]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_team_town()
        {
            var teamDataSource = CreateDataSource();
            var query = new TeamListingFilter { Query = _databaseFixture.TestData.TeamWithFullDetails!.MatchLocations.First().Town!.ToUpperInvariant() };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.TeamListings.Count(x => x.MatchLocations.Any(ml => ml.Town?.Contains(query.Query, StringComparison.OrdinalIgnoreCase) ?? false)), result);
        }

        [Fact]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_team_administrative_area()
        {
            var teamDataSource = CreateDataSource();
            var query = new TeamListingFilter { Query = _databaseFixture.TestData.TeamWithFullDetails!.MatchLocations.First().AdministrativeArea!.ToUpperInvariant() };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.TeamListings.Count(x => x.MatchLocations.Any(ml => ml.AdministrativeArea?.Contains(query.Query, StringComparison.OrdinalIgnoreCase) ?? false)), result);
        }

        [Fact]
        public async Task Read_total_teams_supports_filter_by_team_type()
        {
            var teamDataSource = CreateDataSource();
            var query = new TeamListingFilter { TeamTypes = new List<TeamType?> { TeamType.Representative } };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.TeamListings
               .Count(x => _databaseFixture.TestData.Teams.Where(t => t.TeamType == TeamType.Representative).Select(t => t.TeamId).Contains(x.TeamListingId) ||
                           _databaseFixture.TestData.Clubs.Where(c => c.Teams.Any(t => t.TeamType == TeamType.Representative)).Select(c => c.ClubId).Contains(x.TeamListingId));
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_teams_supports_filter_by_team_type_for_club_with_no_teams()
        {
            var teamDataSource = CreateDataSource();
            var query = new TeamListingFilter { TeamTypes = new List<TeamType?> { null } };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Clubs.Count(c => !c.Teams.Any());
            Assert.Equal(expected, result);
        }


        [Fact]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_club_name()
        {
            var teamDataSource = CreateDataSource();
            var query = new TeamListingFilter { Query = "ClUB MiNiMaL" };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.TeamListings.Count(x => x.ClubOrTeamName.Contains(query.Query, StringComparison.OrdinalIgnoreCase)), result);
        }

        [Fact]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_club_with_locality()
        {
            var teamDataSource = CreateDataSource();
            var query = new TeamListingFilter { Query = _databaseFixture.TestData.MatchLocationForClub.Locality.ToUpperInvariant() };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.TeamListings.Count(x => x.MatchLocations.Any(ml => ml.Locality.Contains(query.Query, StringComparison.OrdinalIgnoreCase))), result);
        }

        [Fact]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_club_with_town()
        {
            var teamDataSource = CreateDataSource();
            var query = new TeamListingFilter { Query = _databaseFixture.TestData.MatchLocationForClub.Town.ToUpperInvariant() };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.TeamListings.Count(x => x.MatchLocations.Any(ml => ml.Town.Contains(query.Query, StringComparison.OrdinalIgnoreCase))), result);
        }

        [Fact]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_club_with_administrative_area()
        {
            var teamDataSource = CreateDataSource();
            var query = new TeamListingFilter { Query = _databaseFixture.TestData.MatchLocationForClub.AdministrativeArea.ToUpperInvariant() };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.TeamListings.Count(x => x.MatchLocations.Any(ml => ml.AdministrativeArea.Contains(query.Query, StringComparison.OrdinalIgnoreCase))), result);
        }

        [Fact]
        public async Task Read_team_listings_returns_basic_fields_for_teams()
        {
            var teamDataSource = CreateDataSource();

            var results = await teamDataSource.ReadTeamListings(null).ConfigureAwait(false);

            foreach (var team in _databaseFixture.TestData.Teams.Where(x => x.Club == null))
            {
                var result = results.SingleOrDefault(x => x.TeamListingId == team.TeamId);

                Assert.NotNull(result);
                Assert.Equal(team.TeamName, result!.ClubOrTeamName);
                Assert.Equal(team.TeamRoute, result.ClubOrTeamRoute);
                Assert.Equal(team.TeamType, result.TeamType);
                Assert.Equal(!team.UntilYear.HasValue, result.Active);
                Assert.Equal(team.PlayerType, result.PlayerTypes.SingleOrDefault());
            }
        }

        [Fact]
        public async Task Read_team_listings_returns_basic_fields_for_clubs()
        {
            var teamDataSource = CreateDataSource();

            var results = await teamDataSource.ReadTeamListings(null).ConfigureAwait(false);

            foreach (var club in _databaseFixture.TestData.TeamListings.Where(x => _databaseFixture.TestData.Clubs.Select(c => c.ClubId).ToList().Contains(x.TeamListingId)))
            {
                var result = results.SingleOrDefault(x => x.TeamListingId == club.TeamListingId);

                Assert.NotNull(result);
                Assert.Equal(club.ClubOrTeamName, result!.ClubOrTeamName);
                Assert.Equal(club.ClubOrTeamRoute, result.ClubOrTeamRoute);
                Assert.Equal(club.Active, result.Active);

                Assert.Equal(club.PlayerTypes.Count, result.PlayerTypes.Count);
                foreach (var playerType in club.PlayerTypes)
                {
                    Assert.Contains(playerType, result.PlayerTypes);
                }
            }
        }

        [Fact]
        public async Task Read_team_listings_returns_match_locations_for_teams()
        {
            var teamDataSource = CreateDataSource();

            var results = await teamDataSource.ReadTeamListings(null).ConfigureAwait(false);

            foreach (var team in _databaseFixture.TestData.Teams.Where(x => x.Club == null))
            {
                var result = results.SingleOrDefault(x => x.TeamListingId == team.TeamId);
                Assert.NotNull(result);

                foreach (var matchLocation in team.MatchLocations)
                {
                    var resultMatchLocation = result!.MatchLocations.SingleOrDefault(x => x.MatchLocationId == matchLocation.MatchLocationId);
                    Assert.NotNull(resultMatchLocation);
                    Assert.Equal(matchLocation.SecondaryAddressableObjectName, resultMatchLocation!.SecondaryAddressableObjectName);
                    Assert.Equal(matchLocation.PrimaryAddressableObjectName, resultMatchLocation.PrimaryAddressableObjectName);
                    Assert.Equal(matchLocation.Locality, resultMatchLocation.Locality);
                    Assert.Equal(matchLocation.Town, resultMatchLocation.Town);
                }
            }
        }

        [Fact]
        public async Task Read_team_listings_returns_match_locations_for_clubs()
        {
            var teamDataSource = CreateDataSource();

            var results = await teamDataSource.ReadTeamListings(null).ConfigureAwait(false);

            var result = results.SingleOrDefault(x => x.TeamListingId == _databaseFixture.TestData.ClubWithTeamsAndMatchLocation!.ClubId);
            Assert.NotNull(result);

            foreach (var matchLocation in _databaseFixture.TestData.ClubWithTeamsAndMatchLocation!.Teams.SelectMany(x => x.MatchLocations))
            {
                var resultMatchLocation = result!.MatchLocations.SingleOrDefault(x => x.MatchLocationId == matchLocation.MatchLocationId);
                Assert.NotNull(resultMatchLocation);
                Assert.Equal(matchLocation.SecondaryAddressableObjectName, resultMatchLocation!.SecondaryAddressableObjectName);
                Assert.Equal(matchLocation.PrimaryAddressableObjectName, resultMatchLocation.PrimaryAddressableObjectName);
                Assert.Equal(matchLocation.Locality, resultMatchLocation.Locality);
                Assert.Equal(matchLocation.Town, resultMatchLocation.Town);
            }
        }

        [Fact]
        public async Task Read_team_listings_sorts_inactive_last()
        {
            var teamDataSource = CreateDataSource();

            var results = await teamDataSource.ReadTeamListings(null).ConfigureAwait(false);

            var expectedActiveStatus = true;
            foreach (var team in results)
            {
                // The first time an inactive team is seen, set a flag to say they must all be inactive
                if (expectedActiveStatus && !team.Active)
                {
                    expectedActiveStatus = false;
                }
                Assert.Equal(expectedActiveStatus, team.Active);
            }
            Assert.False(expectedActiveStatus);
        }

        [Fact]
        public async Task Read_team_listings_returns_team_for_club_with_one_team()
        {
            var teamDataSource = CreateDataSource();

            var results = await teamDataSource.ReadTeamListings(null).ConfigureAwait(false);

            var foundClubWithNoTeams = false;
            var foundClubWithOneActiveTeam = false;
            var foundClubWithOneInactiveTeam = false;
            var foundClubWithOneActiveTeamAndOthersInactive = false;
            var foundClubWithMultipleActiveTeams = false;

            foreach (var club in _databaseFixture.TestData.Clubs)
            {
                var isClubWithNoTeams = !club.Teams.Any();
                var isClubWithOneActiveTeam = club.Teams.Count == 1 && !club.Teams[0].UntilYear.HasValue;
                var isClubWithOneInactiveTeam = club.Teams.Count == 1 && club.Teams[0].UntilYear.HasValue;
                var isClubWithOneActiveTeamAndOthersInactive = club.Teams.Count > 1 && club.Teams.Count(t => !t.UntilYear.HasValue) == 1;
                var isClubWithMultipleActiveTeams = club.Teams.Count(t => !t.UntilYear.HasValue) > 1;

                if (isClubWithNoTeams)
                {
                    Assert.NotNull(results.SingleOrDefault(x => x.TeamListingId == club.ClubId));
                    foundClubWithNoTeams = true;
                }

                if (isClubWithOneActiveTeam)
                {
                    Assert.NotNull(results.SingleOrDefault(x => x.TeamListingId == club.Teams.Single().TeamId));
                    Assert.Null(results.SingleOrDefault(x => x.TeamListingId == club.ClubId));
                    foundClubWithOneActiveTeam = true;
                }

                else if (isClubWithOneInactiveTeam)
                {
                    Assert.NotNull(results.SingleOrDefault(x => x.TeamListingId == club.Teams.Single().TeamId));
                    Assert.Null(results.SingleOrDefault(x => x.TeamListingId == club.ClubId));
                    foundClubWithOneInactiveTeam = true;
                }

                else if (isClubWithOneActiveTeamAndOthersInactive)
                {
                    Assert.NotNull(results.SingleOrDefault(x => x.TeamListingId == club.Teams.Single(x => !x.UntilYear.HasValue).TeamId));
                    foreach (var team in club.Teams.Where(x => x.UntilYear.HasValue))
                    {
                        Assert.Null(results.SingleOrDefault(x => x.TeamListingId == team.TeamId));
                    }
                    Assert.Null(results.SingleOrDefault(x => x.TeamListingId == club.ClubId));
                    foundClubWithOneActiveTeamAndOthersInactive = true;
                }

                else if (isClubWithMultipleActiveTeams)
                {
                    Assert.NotNull(results.SingleOrDefault(x => x.TeamListingId == club.ClubId));
                    foreach (var team in club.Teams)
                    {
                        var resultForTeam = results.SingleOrDefault(x => x.TeamListingId == team.TeamId);
                        Assert.Null(resultForTeam);
                    }
                    foundClubWithMultipleActiveTeams = true;
                }
            }

            Assert.True(foundClubWithNoTeams);
            Assert.True(foundClubWithOneActiveTeam);
            Assert.True(foundClubWithOneInactiveTeam);
            Assert.True(foundClubWithOneActiveTeamAndOthersInactive);
            Assert.True(foundClubWithMultipleActiveTeams);
        }

        [Fact]
        public async Task Read_team_listings_supports_no_filter()
        {
            var teamDataSource = CreateDataSource();

            var result = await teamDataSource.ReadTeamListings(null).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.TeamListings.Select(x => x.TeamListingId);
            var missing = result.Where(x => !expected.Contains(x.TeamListingId));

            Assert.Equal(_databaseFixture.TestData.TeamListings.Count, result.Count);
            foreach (var team in _databaseFixture.TestData.TeamListings)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == team.TeamListingId));
            }
        }

        [Fact]
        public async Task Read_team_listings_supports_case_insensitive_filter_by_team_name()
        {
            var teamDataSource = CreateDataSource();
            var query = new TeamListingFilter { Query = _databaseFixture.TestData.TeamWithFullDetails!.TeamName!.ToUpperInvariant() };

            var result = await teamDataSource.ReadTeamListings(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.TeamListings.Count(x => x.ClubOrTeamName?.Contains(query.Query, StringComparison.OrdinalIgnoreCase) ?? false), result.Count);
            foreach (var team in _databaseFixture.TestData.TeamListings.Where(x => x.ClubOrTeamName?.Contains(query.Query, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == team.TeamListingId));
            }
        }

        [Fact]
        public async Task Read_team_listings_supports_case_insensitive_filter_by_team_name_where_the_club_name_is_different()
        {
            var team = _databaseFixture.TestData.Clubs.Where(x => x.Teams.Count(t => !t.UntilYear.HasValue) > 1)
                                .SelectMany(x => x.Teams)
                                .Where(x => !string.IsNullOrEmpty(x.TeamName))
                                .First(x => !x.Club?.ClubName?.Contains(x.TeamName!, StringComparison.OrdinalIgnoreCase) ?? false);
            var teamDataSource = CreateDataSource();
            var query = new TeamListingFilter { Query = team.TeamName!.ToUpperInvariant() };

            var results = await teamDataSource.ReadTeamListings(query).ConfigureAwait(false);

            foreach (var result in results)
            {
                var club = _databaseFixture.TestData.Clubs.SingleOrDefault(x => x.ClubId == result.TeamListingId);
                if (club != null)
                {
                    var clubNameMatches = club.ClubName?.Contains(team.TeamName, StringComparison.OrdinalIgnoreCase) ?? false;
                    var teamNameMatches = club.Teams.Any(x => x.TeamName?.Contains(team.TeamName, StringComparison.OrdinalIgnoreCase) ?? false);
                    Assert.True(clubNameMatches || teamNameMatches);
                }
                else
                {
                    var resultTeam = _databaseFixture.TestData.Teams.SingleOrDefault(x => x.TeamId == result.TeamListingId);
                    Assert.Contains(team.TeamName, result.ClubOrTeamName, StringComparison.OrdinalIgnoreCase);
                }

            }

            Assert.NotNull(results.SingleOrDefault(x => x.TeamListingId == team.Club?.ClubId));
        }

        [Fact]
        public async Task Read_team_listings_supports_case_insensitive_filter_by_team_locality()
        {
            var teamDataSource = CreateDataSource();
            var query = new TeamListingFilter { Query = _databaseFixture.TestData.TeamWithFullDetails!.MatchLocations.First().Locality!.ToUpperInvariant() };

            var result = await teamDataSource.ReadTeamListings(query).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.TeamListings.Where(x => x.MatchLocations.Any(ml => ml.Locality?.Contains(query.Query, StringComparison.OrdinalIgnoreCase) ?? false));
            Assert.Equal(expected.Count(), result.Count);
            foreach (var team in expected)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == team.TeamListingId));
            }
        }

        [Fact]
        public async Task Read_team_listings_supports_case_insensitive_filter_by_team_town()
        {
            var teamDataSource = CreateDataSource();
            var query = new TeamListingFilter { Query = _databaseFixture.TestData.TeamWithFullDetails!.MatchLocations.First().Town!.ToUpperInvariant() };

            var result = await teamDataSource.ReadTeamListings(query).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.TeamListings.Where(x => x.MatchLocations.Any(ml => ml.Town?.Contains(query.Query, StringComparison.OrdinalIgnoreCase) ?? false));
            Assert.Equal(expected.Count(), result.Count);
            foreach (var team in expected)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == team.TeamListingId));
            }
        }

        [Fact]
        public async Task Read_team_listings_supports_case_insensitive_filter_by_team_administrative_area()
        {
            var teamDataSource = CreateDataSource();
            var query = new TeamListingFilter { Query = _databaseFixture.TestData.TeamWithFullDetails!.MatchLocations.First().AdministrativeArea!.ToUpperInvariant() };

            var result = await teamDataSource.ReadTeamListings(query).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.TeamListings.Where(x => x.MatchLocations.Any(ml => ml.AdministrativeArea!.Contains(query.Query, StringComparison.OrdinalIgnoreCase)));
            Assert.Equal(expected.Count(), result.Count);
            foreach (var teamListing in expected)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == teamListing.TeamListingId));
            }
        }

        [Fact]
        public async Task Read_team_listings_supports_filter_by_team_type()
        {
            var teamDataSource = CreateDataSource();
            var query = new TeamListingFilter { TeamTypes = new List<TeamType?> { TeamType.Representative } };

            var result = await teamDataSource.ReadTeamListings(query).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.TeamListings
                .Where(x => _databaseFixture.TestData.Teams.Where(t => t.TeamType == TeamType.Representative && t.TeamId.HasValue).Select(t => t.TeamId!.Value).Contains(x.TeamListingId!.Value) ||
                            _databaseFixture.TestData.Clubs.Where(c => c.Teams.Any(t => t.TeamType == TeamType.Representative && c.ClubId.HasValue)).Select(c => c.ClubId!.Value).Contains(x.TeamListingId.Value));
            Assert.Equal(expected.Count(), result.Count);
            foreach (var team in expected)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == team.TeamListingId));
            }
        }

        [Fact]
        public async Task Read_team_listings_supports_filter_by_team_type_for_club_with_no_teams()
        {
            var teamDataSource = CreateDataSource();
            var query = new TeamListingFilter { TeamTypes = new List<TeamType?> { null } };

            var result = await teamDataSource.ReadTeamListings(query).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Clubs.Where(x => !x.Teams.Any());
            Assert.Equal(expected.Count(), result.Count);
            foreach (var listing in expected)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == listing.ClubId));
            }
        }

        [Fact]
        public async Task Read_team_listings_supports_case_insensitive_filter_by_club_name()
        {
            var teamDataSource = CreateDataSource();
            var query = new TeamListingFilter { Query = "ClUB MiNiMaL" };

            var result = await teamDataSource.ReadTeamListings(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.TeamListings.Count(x => x.ClubOrTeamName.Contains(query.Query, StringComparison.OrdinalIgnoreCase)), result.Count);
            foreach (var listing in _databaseFixture.TestData.TeamListings.Where(x => x.ClubOrTeamName.Contains(query.Query, StringComparison.OrdinalIgnoreCase)))
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == listing.TeamListingId));
            }
        }

        [Fact]
        public async Task Read_team_listings_supports_case_insensitive_filter_by_club_with_locality()
        {
            var teamDataSource = CreateDataSource();
            var query = new TeamListingFilter { Query = _databaseFixture.TestData.MatchLocationForClub.Locality.ToUpperInvariant() };

            var result = await teamDataSource.ReadTeamListings(query).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.TeamListings.Where(x => x.MatchLocations.Any(ml => ml.Locality.Contains(query.Query, StringComparison.OrdinalIgnoreCase)));
            Assert.Equal(expected.Count(), result.Count);
            foreach (var team in expected)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == team.TeamListingId));
            }
        }

        [Fact]
        public async Task Read_team_listings_supports_case_insensitive_filter_by_club_with_town()
        {
            var teamDataSource = CreateDataSource();
            var query = new TeamListingFilter { Query = _databaseFixture.TestData.MatchLocationForClub.Town.ToUpperInvariant() };

            var result = await teamDataSource.ReadTeamListings(query).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.TeamListings.Where(x => x.MatchLocations.Any(ml => ml.Town.Contains(query.Query, StringComparison.OrdinalIgnoreCase)));
            Assert.Equal(expected.Count(), result.Count);
            foreach (var team in expected)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == team.TeamListingId));
            }
        }

        [Fact]
        public async Task Read_team_listings_supports_case_insensitive_filter_by_club_with_administrative_area()
        {
            var teamDataSource = CreateDataSource();
            var query = new TeamListingFilter { Query = _databaseFixture.TestData.MatchLocationForClub.AdministrativeArea.ToUpperInvariant() };

            var result = await teamDataSource.ReadTeamListings(query).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.TeamListings.Where(x => x.MatchLocations.Any(ml => ml.AdministrativeArea.Contains(query.Query, StringComparison.OrdinalIgnoreCase)));
            Assert.Equal(expected.Count(), result.Count);
            foreach (var team in expected)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == team.TeamListingId));
            }
        }

        [Fact]
        public async Task Read_team_listings_pages_results()
        {
            var teamDataSource = CreateDataSource();

            const int pageSize = 10;
            var pageNumber = 1;
            var remaining = _databaseFixture.TestData.TeamListings.Count;
            while (remaining > 0)
            {
                var result = await teamDataSource.ReadTeamListings(new TeamListingFilter { Paging = new Paging { PageNumber = pageNumber, PageSize = pageSize } }).ConfigureAwait(false);

                var expected = pageSize > remaining ? remaining : pageSize;
                Assert.Equal(expected, result.Count);

                pageNumber++;
                remaining -= pageSize;
            }
        }
    }
}
