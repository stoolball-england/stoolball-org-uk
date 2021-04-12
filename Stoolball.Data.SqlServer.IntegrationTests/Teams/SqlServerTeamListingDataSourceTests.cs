using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

            Assert.Equal(_databaseFixture.Teams.Count + _databaseFixture.Clubs.Count, result);
        }

        [Fact(Skip = "Query done. Assert not done.")]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_team_name()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamFilter { Query = "DeTaIlS" };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.TeamName.Contains(query.Query, StringComparison.OrdinalIgnoreCase)), result);
        }

        [Fact(Skip = "Query done. Assert not done.")]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_team_player_type()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamFilter { Query = "LaDiEs" };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.PlayerType == PlayerType.Ladies), result);
        }

        [Fact(Skip = "Query done. Assert not done.")]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_team_locality()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamFilter { Query = "LoCaLiTy" };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.MatchLocations.Any(ml => ml.Locality.Contains(query.Query, StringComparison.OrdinalIgnoreCase))), result);
        }

        [Fact(Skip = "Query done. Assert not done.")]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_team_town()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamFilter { Query = "ToWn" };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.MatchLocations.Any(ml => ml.Town.Contains(query.Query, StringComparison.OrdinalIgnoreCase))), result);
        }

        [Fact(Skip = "Query done. Assert not done.")]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_team_administrative_area()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamFilter { Query = "CoUnTy" };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.MatchLocations.Any(ml => ml.AdministrativeArea.Contains(query.Query, StringComparison.OrdinalIgnoreCase))), result);
        }

        [Fact(Skip = "Query done. Assert not done.")]
        public async Task Read_total_teams_supports_filter_by_team_type()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamFilter { TeamTypes = new List<TeamType> { TeamType.Representative } };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.TeamType == TeamType.Representative), result);
        }

        [Fact(Skip = "Query done. Assert not done.")]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_club_name()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamFilter { Query = "DeTaIlS" };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.TeamName.Contains(query.Query, StringComparison.OrdinalIgnoreCase)), result);
        }

        [Fact(Skip = "Query done. Assert not done.")]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_club_with_player_type()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamFilter { Query = "LaDiEs" };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.PlayerType == PlayerType.Ladies), result);
        }

        [Fact(Skip = "Query done. Assert not done.")]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_club_with_locality()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamFilter { Query = "LoCaLiTy" };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.MatchLocations.Any(ml => ml.Locality.Contains(query.Query, StringComparison.OrdinalIgnoreCase))), result);
        }

        [Fact(Skip = "Query done. Assert not done.")]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_club_with_town()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamFilter { Query = "ToWn" };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.MatchLocations.Any(ml => ml.Town.Contains(query.Query, StringComparison.OrdinalIgnoreCase))), result);
        }

        [Fact(Skip = "Query done. Assert not done.")]
        public async Task Read_total_teams_supports_case_insensitive_filter_by_club_with_administrative_area()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamFilter { Query = "CoUnTy" };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.MatchLocations.Any(ml => ml.AdministrativeArea.Contains(query.Query, StringComparison.OrdinalIgnoreCase))), result);
        }

        [Fact(Skip = "Query done. Assert not done.")]
        public async Task Read_total_teams_supports_filter_by_club_with_team_type()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamFilter { TeamTypes = new List<TeamType> { TeamType.Representative } };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.TeamType == TeamType.Representative), result);
        }

        [Fact]
        public async Task Read_total_teams_supports_filter_by_excluding_club_teams()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamFilter { IncludeClubTeams = false };

            var result = await teamDataSource.ReadTotalTeams(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.Club == null) + _databaseFixture.Clubs.Count, result);
        }


        [Fact(Skip = "Query done. Assert not done.")]
        public async Task Read_team_listings_returns_basic_fields_for_teams()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await teamDataSource.ReadTeamListings(null).ConfigureAwait(false);

            foreach (var team in _databaseFixture.Teams)
            {
                var result = results.SingleOrDefault(x => x.TeamListingId == team.TeamId);

                Assert.NotNull(result);
                Assert.Equal(team.TeamName, result.ClubOrTeamName);
                Assert.Equal(team.TeamRoute, result.ClubOrTeamRoute);
                //Assert.Equal(team.PlayerType, result.PlayerType);
                //Assert.Equal(team.UntilYear, result.UntilYear);
            }
        }

        [Fact(Skip = "Query done. Assert not done.")]
        public async Task Read_team_listings_returns_basic_fields_for_clubs()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await teamDataSource.ReadTeamListings(null).ConfigureAwait(false);

            foreach (var team in _databaseFixture.Teams)
            {
                var result = results.SingleOrDefault(x => x.TeamListingId == team.TeamId);

                Assert.NotNull(result);
                Assert.Equal(team.TeamName, result.ClubOrTeamName);
                Assert.Equal(team.TeamRoute, result.ClubOrTeamRoute);
                //Assert.Equal(team.PlayerType, result.PlayerType);
                //Assert.Equal(team.UntilYear, result.UntilYear);
            }
        }

        [Fact(Skip = "Query done. Assert not done.")]
        public async Task Read_team_listings_returns_match_locations_for_teams()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await teamDataSource.ReadTeamListings(null).ConfigureAwait(false);

            foreach (var team in _databaseFixture.Teams)
            {
                var result = results.SingleOrDefault(x => x.TeamListingId == team.TeamId);
                Assert.NotNull(result);

                foreach (var matchLocation in team.MatchLocations)
                {
                    var resultMatchLocation = result.MatchLocations.SingleOrDefault(x => x.MatchLocationId == matchLocation.MatchLocationId);
                    Assert.NotNull(resultMatchLocation);
                    Assert.Equal(matchLocation.SecondaryAddressableObjectName, resultMatchLocation.SecondaryAddressableObjectName);
                    Assert.Equal(matchLocation.PrimaryAddressableObjectName, resultMatchLocation.PrimaryAddressableObjectName);
                    Assert.Equal(matchLocation.StreetDescription, resultMatchLocation.StreetDescription);
                    Assert.Equal(matchLocation.Locality, resultMatchLocation.Locality);
                    Assert.Equal(matchLocation.Town, resultMatchLocation.Town);
                    Assert.Equal(matchLocation.AdministrativeArea, resultMatchLocation.AdministrativeArea);
                    Assert.Equal(matchLocation.Postcode, resultMatchLocation.Postcode);
                    Assert.Equal(matchLocation.Latitude, resultMatchLocation.Latitude);
                    Assert.Equal(matchLocation.Longitude, resultMatchLocation.Longitude);
                }
            }
        }

        [Fact(Skip = "Query done. Assert not done.")]
        public async Task Read_team_listings_returns_match_locations_for_clubs()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await teamDataSource.ReadTeamListings(null).ConfigureAwait(false);

            foreach (var team in _databaseFixture.Teams)
            {
                var result = results.SingleOrDefault(x => x.TeamListingId == team.TeamId);
                Assert.NotNull(result);

                foreach (var matchLocation in team.MatchLocations)
                {
                    var resultMatchLocation = result.MatchLocations.SingleOrDefault(x => x.MatchLocationId == matchLocation.MatchLocationId);
                    Assert.NotNull(resultMatchLocation);
                    Assert.Equal(matchLocation.SecondaryAddressableObjectName, resultMatchLocation.SecondaryAddressableObjectName);
                    Assert.Equal(matchLocation.PrimaryAddressableObjectName, resultMatchLocation.PrimaryAddressableObjectName);
                    Assert.Equal(matchLocation.StreetDescription, resultMatchLocation.StreetDescription);
                    Assert.Equal(matchLocation.Locality, resultMatchLocation.Locality);
                    Assert.Equal(matchLocation.Town, resultMatchLocation.Town);
                    Assert.Equal(matchLocation.AdministrativeArea, resultMatchLocation.AdministrativeArea);
                    Assert.Equal(matchLocation.Postcode, resultMatchLocation.Postcode);
                    Assert.Equal(matchLocation.Latitude, resultMatchLocation.Latitude);
                    Assert.Equal(matchLocation.Longitude, resultMatchLocation.Longitude);
                }
            }
        }

        [Fact(Skip = "Query done. Assert not done.")]
        public async Task Read_team_listings_sorts_inactive_last()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);

            var results = await teamDataSource.ReadTeamListings(null).ConfigureAwait(false);

            var expectedActiveStatus = true;
            foreach (var team in results)
            {
                // The first time an inactive team is seen, set a flag to say they must all be inactive
                //if (expectedActiveStatus && team.UntilYear.HasValue)
                //{
                //    expectedActiveStatus = false;
                //}
                //Assert.Equal(expectedActiveStatus, !team.UntilYear.HasValue);
            }
            Assert.False(expectedActiveStatus);
        }

        [Fact(Skip = "Query done. Assert not done.")]
        public async Task Read_team_listings_supports_no_filter()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);

            var result = await teamDataSource.ReadTeamListings(null).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count, result.Count);
            foreach (var team in _databaseFixture.Teams)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == team.TeamId));
            }
        }

        [Fact(Skip = "Query done. Assert not done.")]
        public async Task Read_team_listings_supports_case_insensitive_filter_by_team_name()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamFilter { Query = "DeTaIlS" };

            var result = await teamDataSource.ReadTeamListings(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.TeamName.Contains(query.Query, StringComparison.OrdinalIgnoreCase)), result.Count);
            foreach (var team in _databaseFixture.Teams.Where(x => x.TeamName.Contains(query.Query, StringComparison.OrdinalIgnoreCase)))
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == team.TeamId));
            }
        }

        [Fact(Skip = "Query done. Assert not done.")]
        public async Task Read_team_listings_supports_case_insensitive_filter_by_team_locality()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamFilter { Query = "LoCaLiTy" };

            var result = await teamDataSource.ReadTeamListings(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.MatchLocations.Any(ml => ml.Locality.Contains(query.Query, StringComparison.OrdinalIgnoreCase))), result.Count);
            foreach (var team in _databaseFixture.Teams.Where(x => x.MatchLocations.Any(ml => ml.Locality.Contains(query.Query, StringComparison.OrdinalIgnoreCase))))
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == team.TeamId));
            }
        }

        [Fact(Skip = "Query done. Assert not done.")]
        public async Task Read_team_listings_supports_case_insensitive_filter_by_team_town()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamFilter { Query = "ToWn" };

            var result = await teamDataSource.ReadTeamListings(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.MatchLocations.Any(ml => ml.Town.Contains(query.Query, StringComparison.OrdinalIgnoreCase))), result.Count);
            foreach (var team in _databaseFixture.Teams.Where(x => x.MatchLocations.Any(ml => ml.Town.Contains(query.Query, StringComparison.OrdinalIgnoreCase))))
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == team.TeamId));
            }
        }

        [Fact(Skip = "Query done. Assert not done.")]
        public async Task Read_team_listings_supports_case_insensitive_filter_by_team_administrative_area()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamFilter { Query = "CoUnTy" };

            var result = await teamDataSource.ReadTeamListings(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.MatchLocations.Any(ml => ml.AdministrativeArea.Contains(query.Query, StringComparison.OrdinalIgnoreCase))), result.Count);
            foreach (var team in _databaseFixture.Teams.Where(x => x.MatchLocations.Any(ml => ml.AdministrativeArea.Contains(query.Query, StringComparison.OrdinalIgnoreCase))))
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == team.TeamId));
            }
        }

        [Fact(Skip = "Query done. Assert not done.")]
        public async Task Read_team_listings_supports_filter_by_team_type()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamFilter { TeamTypes = new List<TeamType> { TeamType.Representative } };

            var result = await teamDataSource.ReadTeamListings(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.TeamType == TeamType.Representative), result.Count);
            foreach (var team in _databaseFixture.Teams.Where(x => x.TeamType == TeamType.Representative))
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == team.TeamId));
            }
        }

        [Fact(Skip = "Query done. Assert not done.")]
        public async Task Read_team_listings_supports_case_insensitive_filter_by_club_name()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamFilter { Query = "DeTaIlS" };

            var result = await teamDataSource.ReadTeamListings(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.TeamName.Contains(query.Query, StringComparison.OrdinalIgnoreCase)), result.Count);
            foreach (var team in _databaseFixture.Teams.Where(x => x.TeamName.Contains(query.Query, StringComparison.OrdinalIgnoreCase)))
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == team.TeamId));
            }
        }

        [Fact(Skip = "Query done. Assert not done.")]
        public async Task Read_team_listings_supports_case_insensitive_filter_by_club_with_locality()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamFilter { Query = "LoCaLiTy" };

            var result = await teamDataSource.ReadTeamListings(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.MatchLocations.Any(ml => ml.Locality.Contains(query.Query, StringComparison.OrdinalIgnoreCase))), result.Count);
            foreach (var team in _databaseFixture.Teams.Where(x => x.MatchLocations.Any(ml => ml.Locality.Contains(query.Query, StringComparison.OrdinalIgnoreCase))))
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == team.TeamId));
            }
        }

        [Fact(Skip = "Query done. Assert not done.")]
        public async Task Read_team_listings_supports_case_insensitive_filter_by_club_with_town()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamFilter { Query = "ToWn" };

            var result = await teamDataSource.ReadTeamListings(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.MatchLocations.Any(ml => ml.Town.Contains(query.Query, StringComparison.OrdinalIgnoreCase))), result.Count);
            foreach (var team in _databaseFixture.Teams.Where(x => x.MatchLocations.Any(ml => ml.Town.Contains(query.Query, StringComparison.OrdinalIgnoreCase))))
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == team.TeamId));
            }
        }

        [Fact(Skip = "Query done. Assert not done.")]
        public async Task Read_team_listings_supports_case_insensitive_filter_by_club_with_administrative_area()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamFilter { Query = "CoUnTy" };

            var result = await teamDataSource.ReadTeamListings(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.MatchLocations.Any(ml => ml.AdministrativeArea.Contains(query.Query, StringComparison.OrdinalIgnoreCase))), result.Count);
            foreach (var team in _databaseFixture.Teams.Where(x => x.MatchLocations.Any(ml => ml.AdministrativeArea.Contains(query.Query, StringComparison.OrdinalIgnoreCase))))
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == team.TeamId));
            }
        }

        [Fact(Skip = "Query done. Assert not done.")]
        public async Task Read_team_listings_supports_filter_by_club_with_team_type()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamFilter { TeamTypes = new List<TeamType> { TeamType.Representative } };

            var result = await teamDataSource.ReadTeamListings(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.TeamType == TeamType.Representative), result.Count);
            foreach (var team in _databaseFixture.Teams.Where(x => x.TeamType == TeamType.Representative))
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == team.TeamId));
            }
        }

        [Fact(Skip = "Query done. Assert not done.")]
        public async Task Read_team_listings_supports_filter_by_excluding_club_teams()
        {
            var teamDataSource = new SqlServerTeamListingDataSource(_databaseFixture.ConnectionFactory);
            var query = new TeamFilter { IncludeClubTeams = false };

            var result = await teamDataSource.ReadTeamListings(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Teams.Count(x => x.Club == null), result.Count);
            foreach (var team in _databaseFixture.Teams.Where(x => x.Club == null))
            {
                Assert.NotNull(result.SingleOrDefault(x => x.TeamListingId == team.TeamId));
            }
        }
    }
}
