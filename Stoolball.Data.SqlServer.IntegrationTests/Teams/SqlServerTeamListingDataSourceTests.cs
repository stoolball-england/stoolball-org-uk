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
    [Collection(IntegrationTestConstants.DataSourceIntegrationTestCollection)]
    public class SqlServerTeamListingDataSourceTests
    {
        private readonly SqlServerDataSourceFixture _databaseFixture;

        public SqlServerTeamListingDataSourceTests(SqlServerDataSourceFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_total_teams_supports_no_filter()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await teamDataSource.ReadTotalTeams(null).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TeamListings.Count, result);
        }

        [Fact]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_team_name()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamListingFilter { Query = _databaseFixture.TeamWithFullDetails.TeamName.ToUpperInvariant() };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TeamListings.Count(x => x.ClubOrTeamName.Contains(query.Query, StringComparison.OrdinalIgnoreCase)), result);
        }

        [Fact]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_team_name_where_the_club_name_is_different()
        {
            var team = _databaseFixture.Clubs.Where(x => x.Teams.Count > 1).SelectMany(x => x.Teams).First(x => !x.Club.ClubName.Contains(x.TeamName, StringComparison.OrdinalIgnoreCase));
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamListingFilter { Query = team.TeamName.ToUpperInvariant() };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            var matchedByClubName = _databaseFixture.Clubs.Where(x => x.ClubName.Contains(query.Query, StringComparison.OrdinalIgnoreCase));
            var matchedByTeamName = _databaseFixture.Teams.Where(x => x.TeamName.Contains(query.Query, StringComparison.OrdinalIgnoreCase) && (x.Club == null || !matchedByClubName.Select(c => x.Club.ClubId.Value).Contains(x.Club.ClubId.Value)));
            Assert.Equal(matchedByClubName.Count() + matchedByTeamName.Count(x => x.Club == null) + matchedByTeamName.Where(x => x.Club != null).Select(x => x.Club.ClubId.Value).Distinct().Count(), result);
        }

        [Fact]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_player_type()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamListingFilter { Query = "LaDiEs" };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TeamListings.Count(x => x.PlayerTypes.Contains(PlayerType.Ladies)), result);
        }

        [Fact]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_team_locality()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamListingFilter { Query = _databaseFixture.TeamWithFullDetails.MatchLocations.First().Locality.ToUpperInvariant() };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Where(x => x.Club == null).Count(x => x.MatchLocations.Any(ml => ml.Locality.Contains(query.Query, StringComparison.OrdinalIgnoreCase))), result);
        }

        [Fact]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_team_town()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamListingFilter { Query = _databaseFixture.TeamWithFullDetails.MatchLocations.First().Town.ToUpperInvariant() };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Where(x => x.Club == null).Count(x => x.MatchLocations.Any(ml => ml.Town.Contains(query.Query, StringComparison.OrdinalIgnoreCase))), result);
        }

        [Fact]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_team_administrative_area()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamListingFilter { Query = _databaseFixture.TeamWithFullDetails.MatchLocations.First().AdministrativeArea.ToUpperInvariant() };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Where(x => x.Club == null).Count(x => x.MatchLocations.Any(ml => ml.AdministrativeArea.Contains(query.Query, StringComparison.OrdinalIgnoreCase))), result);
        }

        [Fact]
        public async Task Read_total_teams_supports_filter_by_team_type()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamListingFilter { TeamTypes = new List<TeamType?> { TeamType.Representative } };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            var expected = _databaseFixture.TeamListings
               .Count(x => _databaseFixture.Teams.Where(t => t.TeamType == TeamType.Representative).Select(t => t.TeamId.Value).Contains(x.TeamListingId.Value) ||
                           _databaseFixture.Clubs.Where(c => c.Teams.Any(t => t.TeamType == TeamType.Representative)).Select(c => c.ClubId.Value).Contains(x.TeamListingId.Value));
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Read_total_teams_supports_filter_by_team_type_for_club_with_no_teams()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamListingFilter { TeamTypes = new List<TeamType?> { null } };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            var expected = _databaseFixture.Clubs.Count(c => !c.Teams.Any());
            Assert.Equal(expected, result);
        }


        [Fact]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_club_name()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamListingFilter { Query = "ClUB MiNiMaL" };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TeamListings.Count(x => x.ClubOrTeamName.Contains(query.Query, StringComparison.OrdinalIgnoreCase)), result);
        }

        [Fact]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_club_with_locality()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamListingFilter { Query = _databaseFixture.MatchLocationForClub.Locality.ToUpperInvariant() };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TeamListings.Count(x => x.MatchLocations.Any(ml => ml.Locality.Contains(query.Query, StringComparison.OrdinalIgnoreCase))), result);
        }

        [Fact]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_club_with_town()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamListingFilter { Query = _databaseFixture.MatchLocationForClub.Town.ToUpperInvariant() };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TeamListings.Count(x => x.MatchLocations.Any(ml => ml.Town.Contains(query.Query, StringComparison.OrdinalIgnoreCase))), result);
        }

        [Fact]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_club_with_administrative_area()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamListingFilter { Query = _databaseFixture.MatchLocationForClub.AdministrativeArea.ToUpperInvariant() };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TeamListings.Count(x => x.MatchLocations.Any(ml => ml.AdministrativeArea.Contains(query.Query, StringComparison.OrdinalIgnoreCase))), result);
        }

        [Fact]
        public async Task Read_team_listings_returns_basic_fields_for_teams()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await teamDataSource.ReadTeamListings(null).ConfigureAwait(false);

            foreach (var team in _databaseFixture.Teams.Where(x => x.Club == null))
            {
                var result = results.SingleOrDefault(x => x.TeamListingId == team.TeamId);

                Assert.NotNull(result);
                Assert.Equal(team.TeamName, result.ClubOrTeamName);
                Assert.Equal(team.TeamRoute, result.ClubOrTeamRoute);
                Assert.Equal(team.TeamType, result.TeamType);
                Assert.Equal(!team.UntilYear.HasValue, result.Active);
                Assert.Equal(team.PlayerType, result.PlayerTypes.SingleOrDefault());
            }
        }

        [Fact]
        public async Task Read_team_listings_returns_basic_fields_for_clubs()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await teamDataSource.ReadTeamListings(null).ConfigureAwait(false);

            foreach (var club in _databaseFixture.TeamListings.Where(x => _databaseFixture.Clubs.Select(c => c.ClubId.Value).ToList().Contains(x.TeamListingId.Value)))
            {
                var result = results.SingleOrDefault(x => x.TeamListingId == club.TeamListingId);

                Assert.NotNull(result);
                Assert.Equal(club.ClubOrTeamName, result.ClubOrTeamName);
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
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await teamDataSource.ReadTeamListings(null).ConfigureAwait(false);

            foreach (var team in _databaseFixture.Teams.Where(x => x.Club == null))
            {
                var result = results.SingleOrDefault(x => x.TeamListingId == team.TeamId);
                Assert.NotNull(result);

                foreach (var matchLocation in team.MatchLocations)
                {
                    var resultMatchLocation = result.MatchLocations.SingleOrDefault(x => x.MatchLocationId == matchLocation.MatchLocationId);
                    Assert.NotNull(resultMatchLocation);
                    Assert.Equal(matchLocation.SecondaryAddressableObjectName, resultMatchLocation.SecondaryAddressableObjectName);
                    Assert.Equal(matchLocation.PrimaryAddressableObjectName, resultMatchLocation.PrimaryAddressableObjectName);
                    Assert.Equal(matchLocation.Locality, resultMatchLocation.Locality);
                    Assert.Equal(matchLocation.Town, resultMatchLocation.Town);
                }
            }
        }

        [Fact]
        public async Task Read_team_listings_returns_match_locations_for_clubs()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await teamDataSource.ReadTeamListings(null).ConfigureAwait(false);

            var result = results.SingleOrDefault(x => x.TeamListingId == _databaseFixture.ClubWithTeamsAndMatchLocation.ClubId);
            Assert.NotNull(result);

            foreach (var matchLocation in _databaseFixture.ClubWithTeamsAndMatchLocation.Teams.SelectMany(x => x.MatchLocations))
            {
                var resultMatchLocation = result.MatchLocations.SingleOrDefault(x => x.MatchLocationId == matchLocation.MatchLocationId);
                Assert.NotNull(resultMatchLocation);
                Assert.Equal(matchLocation.SecondaryAddressableObjectName, resultMatchLocation.SecondaryAddressableObjectName);
                Assert.Equal(matchLocation.PrimaryAddressableObjectName, resultMatchLocation.PrimaryAddressableObjectName);
                Assert.Equal(matchLocation.Locality, resultMatchLocation.Locality);
                Assert.Equal(matchLocation.Town, resultMatchLocation.Town);
            }
        }

        [Fact]
        public async Task Read_team_listings_sorts_inactive_last()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);

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
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await teamDataSource.ReadTeamListings(null).ConfigureAwait(false);

            Assert.NotNull(results.SingleOrDefault(x => x.TeamListingId == _databaseFixture.ClubWithOneTeam.Teams.First().TeamId));
            Assert.Null(results.SingleOrDefault(x => x.TeamListingId == _databaseFixture.ClubWithOneTeam.ClubId));
        }

        [Fact]
        public async Task Read_team_listings_supports_no_filter()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await teamDataSource.ReadTeamListings(null).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TeamListings.Count, result.Count);
            foreach (var team in _databaseFixture.TeamListings)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == team.TeamListingId));
            }
        }

        [Fact]
        public async Task Read_team_listings_supports_case_insensitive_filter_by_team_name()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamListingFilter { Query = _databaseFixture.TeamWithFullDetails.TeamName.ToUpperInvariant() };

            var result = await teamDataSource.ReadTeamListings(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TeamListings.Count(x => x.ClubOrTeamName.Contains(query.Query, StringComparison.OrdinalIgnoreCase)), result.Count);
            foreach (var team in _databaseFixture.TeamListings.Where(x => x.ClubOrTeamName.Contains(query.Query, StringComparison.OrdinalIgnoreCase)))
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == team.TeamListingId));
            }
        }

        [Fact]
        public async Task Read_team_listings_supports_case_insensitive_filter_by_team_name_where_the_club_name_is_different()
        {
            var team = _databaseFixture.Clubs.Where(x => x.Teams.Count > 1).SelectMany(x => x.Teams).First(x => !x.Club.ClubName.Contains(x.TeamName, StringComparison.OrdinalIgnoreCase));
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamListingFilter { Query = team.TeamName.ToUpperInvariant() };

            var results = await teamDataSource.ReadTeamListings(query).ConfigureAwait(false);

            foreach (var result in results)
            {
                var club = _databaseFixture.Clubs.SingleOrDefault(x => x.ClubId == result.TeamListingId);
                if (club != null)
                {
                    var clubNameMatches = club.ClubName.Contains(team.TeamName, StringComparison.OrdinalIgnoreCase);
                    var teamNameMatches = club.Teams.Any(x => x.TeamName.Contains(team.TeamName, StringComparison.OrdinalIgnoreCase));
                    Assert.True(clubNameMatches || teamNameMatches);
                }
                else
                {
                    var resultTeam = _databaseFixture.Teams.SingleOrDefault(x => x.TeamId == result.TeamListingId);
                    Assert.Contains(team.TeamName, result.ClubOrTeamName, StringComparison.OrdinalIgnoreCase);
                }

            }
            if (team.Club.Teams.Count > 1)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.TeamListingId == team.Club.ClubId));
            }
            else
            {
                Assert.NotNull(results.SingleOrDefault(x => x.TeamListingId == team.TeamId));
            }
        }

        [Fact]
        public async Task Read_team_listings_supports_case_insensitive_filter_by_team_locality()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamListingFilter { Query = _databaseFixture.TeamWithFullDetails.MatchLocations.First().Locality.ToUpperInvariant() };

            var result = await teamDataSource.ReadTeamListings(query).ConfigureAwait(false);

            var expected = _databaseFixture.Teams.Where(x => x.Club == null && x.MatchLocations.Any(ml => ml.Locality.Contains(query.Query, StringComparison.OrdinalIgnoreCase)));
            Assert.Equal(expected.Count(), result.Count);
            foreach (var team in expected)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == team.TeamId));
            }
        }

        [Fact]
        public async Task Read_team_listings_supports_case_insensitive_filter_by_team_town()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamListingFilter { Query = _databaseFixture.TeamWithFullDetails.MatchLocations.First().Town.ToUpperInvariant() };

            var result = await teamDataSource.ReadTeamListings(query).ConfigureAwait(false);

            var expected = _databaseFixture.Teams.Where(x => x.Club == null && x.MatchLocations.Any(ml => ml.Town.Contains(query.Query, StringComparison.OrdinalIgnoreCase)));
            Assert.Equal(expected.Count(), result.Count);
            foreach (var team in expected)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == team.TeamId));
            }
        }

        [Fact]
        public async Task Read_team_listings_supports_case_insensitive_filter_by_team_administrative_area()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamListingFilter { Query = _databaseFixture.TeamWithFullDetails.MatchLocations.First().AdministrativeArea.ToUpperInvariant() };

            var result = await teamDataSource.ReadTeamListings(query).ConfigureAwait(false);

            var expected = _databaseFixture.Teams.Where(x => x.Club == null && x.MatchLocations.Any(ml => ml.AdministrativeArea.Contains(query.Query, StringComparison.OrdinalIgnoreCase)));
            Assert.Equal(expected.Count(), result.Count);
            foreach (var team in expected)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == team.TeamId));
            }
        }

        [Fact]
        public async Task Read_team_listings_supports_filter_by_team_type()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamListingFilter { TeamTypes = new List<TeamType?> { TeamType.Representative } };

            var result = await teamDataSource.ReadTeamListings(query).ConfigureAwait(false);

            var expected = _databaseFixture.TeamListings
                .Where(x => _databaseFixture.Teams.Where(t => t.TeamType == TeamType.Representative && t.TeamId.HasValue).Select(t => t.TeamId!.Value).Contains(x.TeamListingId!.Value) ||
                            _databaseFixture.Clubs.Where(c => c.Teams.Any(t => t.TeamType == TeamType.Representative && c.ClubId.HasValue)).Select(c => c.ClubId!.Value).Contains(x.TeamListingId.Value));
            Assert.Equal(expected.Count(), result.Count);
            foreach (var team in expected)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == team.TeamListingId));
            }
        }

        [Fact]
        public async Task Read_team_listings_supports_filter_by_team_type_for_club_with_no_teams()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamListingFilter { TeamTypes = new List<TeamType?> { null } };

            var result = await teamDataSource.ReadTeamListings(query).ConfigureAwait(false);

            var expected = _databaseFixture.Clubs.Where(x => !x.Teams.Any());
            Assert.Equal(expected.Count(), result.Count);
            foreach (var listing in expected)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == listing.ClubId));
            }
        }

        [Fact]
        public async Task Read_team_listings_supports_case_insensitive_filter_by_club_name()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamListingFilter { Query = "ClUB MiNiMaL" };

            var result = await teamDataSource.ReadTeamListings(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TeamListings.Count(x => x.ClubOrTeamName.Contains(query.Query, StringComparison.OrdinalIgnoreCase)), result.Count);
            foreach (var listing in _databaseFixture.TeamListings.Where(x => x.ClubOrTeamName.Contains(query.Query, StringComparison.OrdinalIgnoreCase)))
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == listing.TeamListingId));
            }
        }

        [Fact]
        public async Task Read_team_listings_supports_case_insensitive_filter_by_club_with_locality()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamListingFilter { Query = _databaseFixture.MatchLocationForClub.Locality.ToUpperInvariant() };

            var result = await teamDataSource.ReadTeamListings(query).ConfigureAwait(false);

            var expected = _databaseFixture.TeamListings.Where(x => x.MatchLocations.Any(ml => ml.Locality.Contains(query.Query, StringComparison.OrdinalIgnoreCase)));
            Assert.Equal(expected.Count(), result.Count);
            foreach (var team in expected)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == team.TeamListingId));
            }
        }

        [Fact]
        public async Task Read_team_listings_supports_case_insensitive_filter_by_club_with_town()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamListingFilter { Query = _databaseFixture.MatchLocationForClub.Town.ToUpperInvariant() };

            var result = await teamDataSource.ReadTeamListings(query).ConfigureAwait(false);

            var expected = _databaseFixture.TeamListings.Where(x => x.MatchLocations.Any(ml => ml.Town.Contains(query.Query, StringComparison.OrdinalIgnoreCase)));
            Assert.Equal(expected.Count(), result.Count);
            foreach (var team in expected)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == team.TeamListingId));
            }
        }

        [Fact]
        public async Task Read_team_listings_supports_case_insensitive_filter_by_club_with_administrative_area()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamListingFilter { Query = _databaseFixture.MatchLocationForClub.AdministrativeArea.ToUpperInvariant() };

            var result = await teamDataSource.ReadTeamListings(query).ConfigureAwait(false);

            var expected = _databaseFixture.TeamListings.Where(x => x.MatchLocations.Any(ml => ml.AdministrativeArea.Contains(query.Query, StringComparison.OrdinalIgnoreCase)));
            Assert.Equal(expected.Count(), result.Count);
            foreach (var team in expected)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == team.TeamListingId));
            }
        }

        [Fact]
        public async Task Read_team_listings_pages_results()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);

            const int pageSize = 10;
            var pageNumber = 1;
            var remaining = _databaseFixture.TeamListings.Count;
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
