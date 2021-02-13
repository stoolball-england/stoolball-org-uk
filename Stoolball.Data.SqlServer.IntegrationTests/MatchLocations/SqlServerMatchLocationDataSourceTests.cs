using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Stoolball.MatchLocations;
using Stoolball.Routing;
using Stoolball.Teams;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.MatchLocations
{
    [Collection(IntegrationTestConstants.DataSourceIntegrationTestCollection)]
    public class SqlServerMatchLocationDataSourceTests
    {
        private readonly SqlServerDataSourceFixture _databaseFixture;

        public SqlServerMatchLocationDataSourceTests(SqlServerDataSourceFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_minimal_location_by_route_returns_basic_fields()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.MatchLocationWithMinimalDetails.MatchLocationRoute, "locations")).Returns(_databaseFixture.MatchLocationWithMinimalDetails.MatchLocationRoute);
            var locationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await locationDataSource.ReadMatchLocationByRoute(_databaseFixture.MatchLocationWithMinimalDetails.MatchLocationRoute, false).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchLocationWithMinimalDetails.MatchLocationId, result.MatchLocationId);
            Assert.Equal(_databaseFixture.MatchLocationWithMinimalDetails.MatchLocationRoute, result.MatchLocationRoute);
            Assert.Equal(_databaseFixture.MatchLocationWithMinimalDetails.SecondaryAddressableObjectName, result.SecondaryAddressableObjectName);
            Assert.Equal(_databaseFixture.MatchLocationWithMinimalDetails.PrimaryAddressableObjectName, result.PrimaryAddressableObjectName);
            Assert.Equal(_databaseFixture.MatchLocationWithMinimalDetails.StreetDescription, result.StreetDescription);
            Assert.Equal(_databaseFixture.MatchLocationWithMinimalDetails.Locality, result.Locality);
            Assert.Equal(_databaseFixture.MatchLocationWithMinimalDetails.Town, result.Town);
            Assert.Equal(_databaseFixture.MatchLocationWithMinimalDetails.AdministrativeArea, result.AdministrativeArea);
            Assert.Equal(_databaseFixture.MatchLocationWithMinimalDetails.Postcode, result.Postcode);
            Assert.Equal(_databaseFixture.MatchLocationWithMinimalDetails.Latitude, result.Latitude);
            Assert.Equal(_databaseFixture.MatchLocationWithMinimalDetails.Longitude, result.Longitude);
            Assert.Equal(_databaseFixture.MatchLocationWithMinimalDetails.GeoPrecision, result.GeoPrecision);
            Assert.Equal(_databaseFixture.MatchLocationWithMinimalDetails.MatchLocationNotes, result.MatchLocationNotes);
            Assert.Equal(_databaseFixture.MatchLocationWithMinimalDetails.MemberGroupName, result.MemberGroupName);
        }

        [Fact]
        public async Task Read_minimal_location_by_route_with_related_returns_basic_fields()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.MatchLocationWithMinimalDetails.MatchLocationRoute, "locations")).Returns(_databaseFixture.MatchLocationWithMinimalDetails.MatchLocationRoute);
            var locationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await locationDataSource.ReadMatchLocationByRoute(_databaseFixture.MatchLocationWithMinimalDetails.MatchLocationRoute, true).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchLocationWithMinimalDetails.MatchLocationId, result.MatchLocationId);
            Assert.Equal(_databaseFixture.MatchLocationWithMinimalDetails.MatchLocationRoute, result.MatchLocationRoute);
            Assert.Equal(_databaseFixture.MatchLocationWithMinimalDetails.SecondaryAddressableObjectName, result.SecondaryAddressableObjectName);
            Assert.Equal(_databaseFixture.MatchLocationWithMinimalDetails.PrimaryAddressableObjectName, result.PrimaryAddressableObjectName);
            Assert.Equal(_databaseFixture.MatchLocationWithMinimalDetails.StreetDescription, result.StreetDescription);
            Assert.Equal(_databaseFixture.MatchLocationWithMinimalDetails.Locality, result.Locality);
            Assert.Equal(_databaseFixture.MatchLocationWithMinimalDetails.Town, result.Town);
            Assert.Equal(_databaseFixture.MatchLocationWithMinimalDetails.AdministrativeArea, result.AdministrativeArea);
            Assert.Equal(_databaseFixture.MatchLocationWithMinimalDetails.Postcode, result.Postcode);
            Assert.Equal(_databaseFixture.MatchLocationWithMinimalDetails.Latitude, result.Latitude);
            Assert.Equal(_databaseFixture.MatchLocationWithMinimalDetails.Longitude, result.Longitude);
            Assert.Equal(_databaseFixture.MatchLocationWithMinimalDetails.GeoPrecision, result.GeoPrecision);
            Assert.Equal(_databaseFixture.MatchLocationWithMinimalDetails.MatchLocationNotes, result.MatchLocationNotes);
            Assert.Equal(_databaseFixture.MatchLocationWithMinimalDetails.MemberGroupKey, result.MemberGroupKey);
            Assert.Equal(_databaseFixture.MatchLocationWithMinimalDetails.MemberGroupName, result.MemberGroupName);
        }

        [Fact]
        public async Task Read_location_by_route_sorts_active_teams_first()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.MatchLocationWithFullDetails.MatchLocationRoute, "locations")).Returns(_databaseFixture.MatchLocationWithFullDetails.MatchLocationRoute);
            var locationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await locationDataSource.ReadMatchLocationByRoute(_databaseFixture.MatchLocationWithFullDetails.MatchLocationRoute, true).ConfigureAwait(false);

            var expectedActiveStatus = true;
            foreach (var team in result.Teams)
            {
                // The first time an inactive team is seen, set a flag to say they must all be inactive
                if (expectedActiveStatus && team.UntilYear.HasValue)
                {
                    expectedActiveStatus = false;
                }
                Assert.Equal(expectedActiveStatus, !team.UntilYear.HasValue);
            }
        }

        [Fact]
        public async Task Read_location_by_route_excludes_transient_teams()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.MatchLocationWithFullDetails.MatchLocationRoute, "locations")).Returns(_databaseFixture.MatchLocationWithFullDetails.MatchLocationRoute);
            var locationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await locationDataSource.ReadMatchLocationByRoute(_databaseFixture.MatchLocationWithFullDetails.MatchLocationRoute, true).ConfigureAwait(false);

            foreach (var team in _databaseFixture.MatchLocationWithFullDetails.Teams.Where(x => x.TeamType == TeamType.Transient))
            {
                Assert.Null(result.Teams.SingleOrDefault(x => x.TeamId == team.TeamId));
            }
        }

        [Fact]
        public async Task Read_total_locations_supports_no_filter()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await matchLocationDataSource.ReadTotalMatchLocations(null).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchLocations.Count, result);
        }

        [Fact]
        public async Task Read_total_locations_supports_case_insensitive_filter_SAON()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var query = new MatchLocationQuery { Query = "SeCoNdArY" };

            var result = await matchLocationDataSource.ReadTotalMatchLocations(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchLocations.Count(x => x.SecondaryAddressableObjectName.Contains(query.Query, StringComparison.OrdinalIgnoreCase)), result);
        }

        [Fact]
        public async Task Read_total_locations_supports_case_insensitive_filter_PAON()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var query = new MatchLocationQuery { Query = "PrImArY" };

            var result = await matchLocationDataSource.ReadTotalMatchLocations(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchLocations.Count(x => x.PrimaryAddressableObjectName.Contains(query.Query, StringComparison.OrdinalIgnoreCase)), result);
        }


        [Fact]
        public async Task Read_total_locations_supports_case_insensitive_filter_locality()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var query = new MatchLocationQuery { Query = "LoCaLiTy" };

            var result = await matchLocationDataSource.ReadTotalMatchLocations(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchLocations.Count(x => x.Locality.Contains(query.Query, StringComparison.OrdinalIgnoreCase)), result);
        }

        [Fact]
        public async Task Read_total_locations_supports_case_insensitive_filter_town()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var query = new MatchLocationQuery { Query = "ToWn" };

            var result = await matchLocationDataSource.ReadTotalMatchLocations(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchLocations.Count(x => x.Town.Contains(query.Query, StringComparison.OrdinalIgnoreCase)), result);
        }

        [Fact]
        public async Task Read_total_locations_supports_excluding_locations_by_id()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await matchLocationDataSource.ReadTotalMatchLocations(new MatchLocationQuery { ExcludeMatchLocationIds = new List<Guid> { _databaseFixture.MatchLocationWithMinimalDetails.MatchLocationId.Value } }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchLocations.Count - 1, result);
        }

        [Fact]
        public async Task Read_total_locations_supports_excluding_locations_with_no_active_teams()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await matchLocationDataSource.ReadTotalMatchLocations(new MatchLocationQuery { HasActiveTeams = true }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchLocations.Count(x => x.Teams.Any(t => !t.UntilYear.HasValue)), result);
        }

        [Fact]
        public async Task Read_total_locations_supports_filter_by_team_type()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await matchLocationDataSource.ReadTotalMatchLocations(new MatchLocationQuery { TeamTypes = new List<TeamType> { TeamType.Regular } }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchLocations.Count(x => x.Teams.Any(t => t.TeamType == TeamType.Regular)), result);
        }

        [Fact]
        public async Task Read_match_locations_returns_basic_fields()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var results = await matchLocationDataSource.ReadMatchLocations(new MatchLocationQuery { PageSize = _databaseFixture.MatchLocations.Count }).ConfigureAwait(false);

            foreach (var location in _databaseFixture.MatchLocations)
            {
                var result = results.SingleOrDefault(x => x.MatchLocationId == location.MatchLocationId);

                Assert.NotNull(result);
                Assert.Equal(location.MatchLocationRoute, result.MatchLocationRoute);
                Assert.Equal(location.Latitude, result.Latitude);
                Assert.Equal(location.Longitude, result.Longitude);
                Assert.Equal(location.SecondaryAddressableObjectName, result.SecondaryAddressableObjectName);
                Assert.Equal(location.PrimaryAddressableObjectName, result.PrimaryAddressableObjectName);
                Assert.Equal(location.Locality, result.Locality);
                Assert.Equal(location.Town, result.Town);
            }
        }

        [Fact]
        public async Task Read_match_locations_returns_teams()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var results = await matchLocationDataSource.ReadMatchLocations(new MatchLocationQuery { PageSize = _databaseFixture.MatchLocations.Count }).ConfigureAwait(false);

            foreach (var location in _databaseFixture.MatchLocations)
            {
                var result = results.SingleOrDefault(x => x.MatchLocationId == location.MatchLocationId);
                Assert.NotNull(result);

                foreach (var team in location.Teams)
                {
                    var resultTeam = result.Teams.SingleOrDefault(x => x.TeamId == team.TeamId);
                    Assert.NotNull(resultTeam);

                    Assert.Equal(team.TeamName, resultTeam.TeamName);
                    Assert.Equal(team.TeamType, resultTeam.TeamType);
                    Assert.Equal(team.PlayerType, resultTeam.PlayerType);
                    Assert.Equal(team.TeamRoute, resultTeam.TeamRoute);
                    Assert.Equal(team.UntilYear, resultTeam.UntilYear);
                }
            }
        }

        [Fact]
        public async Task Read_match_locations_returns_locations_with_active_teams_first()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var results = await matchLocationDataSource.ReadMatchLocations(new MatchLocationQuery { PageSize = _databaseFixture.MatchLocations.Count }).ConfigureAwait(false);

            var expectedActiveStatus = true;
            foreach (var matchLocation in results)
            {
                // The first time no active team is seen, set a flag to say they must all be inactive
                if (expectedActiveStatus && !matchLocation.Teams.Any(x => !x.UntilYear.HasValue))
                {
                    expectedActiveStatus = false;
                }
                Assert.Equal(expectedActiveStatus, matchLocation.Teams.Any(x => !x.UntilYear.HasValue));
            }
            Assert.False(expectedActiveStatus);
        }

        [Fact]
        public async Task Read_match_locations_supports_no_filter()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await matchLocationDataSource.ReadMatchLocations(new MatchLocationQuery { PageSize = _databaseFixture.MatchLocations.Count }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchLocations.Count, result.Count);
            foreach (var location in _databaseFixture.MatchLocations)
            {
                Assert.NotNull(result.Single(x => x.MatchLocationId == location.MatchLocationId));
            }
        }

        [Fact]
        public async Task Read_match_locations_supports_case_insensitive_filter_SAON()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var query = new MatchLocationQuery { PageSize = _databaseFixture.MatchLocations.Count, Query = "SeCoNdArY" };

            var result = await matchLocationDataSource.ReadMatchLocations(query).ConfigureAwait(false);

            foreach (var location in _databaseFixture.MatchLocations.Where(x => x.SecondaryAddressableObjectName.Contains("secondary", StringComparison.OrdinalIgnoreCase)))
            {
                Assert.NotNull(result.Single(x => x.MatchLocationId == location.MatchLocationId));
            }
        }

        [Fact]
        public async Task Read_match_locations_supports_case_insensitive_filter_PAON()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var query = new MatchLocationQuery { PageSize = _databaseFixture.MatchLocations.Count, Query = "PrImArY" };

            var result = await matchLocationDataSource.ReadMatchLocations(query).ConfigureAwait(false);

            foreach (var location in _databaseFixture.MatchLocations.Where(x => x.PrimaryAddressableObjectName.Contains("primary", StringComparison.OrdinalIgnoreCase)))
            {
                Assert.NotNull(result.Single(x => x.MatchLocationId == location.MatchLocationId));
            }
        }


        [Fact]
        public async Task Read_match_locations_supports_case_insensitive_filter_locality()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var query = new MatchLocationQuery { PageSize = _databaseFixture.MatchLocations.Count, Query = "LoCaLiTy" };

            var result = await matchLocationDataSource.ReadMatchLocations(query).ConfigureAwait(false);

            foreach (var location in _databaseFixture.MatchLocations.Where(x => x.Locality.Contains("locality", StringComparison.OrdinalIgnoreCase)))
            {
                Assert.NotNull(result.Single(x => x.MatchLocationId == location.MatchLocationId));
            }
        }

        [Fact]
        public async Task Read_match_locations_supports_case_insensitive_filter_town()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var query = new MatchLocationQuery { PageSize = _databaseFixture.MatchLocations.Count, Query = "ToWn" };

            var result = await matchLocationDataSource.ReadMatchLocations(query).ConfigureAwait(false);

            foreach (var location in _databaseFixture.MatchLocations.Where(x => x.Town.Contains("town", StringComparison.OrdinalIgnoreCase)))
            {
                Assert.NotNull(result.Single(x => x.MatchLocationId == location.MatchLocationId));
            }
        }

        [Fact]
        public async Task Read_match_locations_supports_excluding_locations_by_id()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var query = new MatchLocationQuery
            {
                PageSize = _databaseFixture.MatchLocations.Count,
                ExcludeMatchLocationIds = new List<Guid> { _databaseFixture.MatchLocationWithMinimalDetails.MatchLocationId.Value }
            };

            var result = await matchLocationDataSource.ReadMatchLocations(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchLocations.Count - 1, result.Count);
        }

        [Fact]
        public async Task Read_match_locations_supports_excluding_locations_with_no_active_teams()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var query = new MatchLocationQuery
            {
                PageSize = _databaseFixture.MatchLocations.Count,
                HasActiveTeams = true
            };

            var result = await matchLocationDataSource.ReadMatchLocations(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchLocations.Count(x => x.Teams.Any(t => !t.UntilYear.HasValue)), result.Count);
        }

        [Fact]
        public async Task Read_match_locations_supports_filter_by_team_type()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var query = new MatchLocationQuery
            {
                PageSize = _databaseFixture.MatchLocations.Count,
                TeamTypes = new List<TeamType> { TeamType.Regular }
            };

            var results = await matchLocationDataSource.ReadMatchLocations(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchLocations.Count(x => x.Teams.Any(t => t.TeamType == TeamType.Regular)), results.Count);
            foreach (var result in results)
            {
                Assert.Empty(result.Teams.Where(x => x.TeamType != TeamType.Regular));
            }
        }

        [Fact]
        public async Task Read_match_locations_pages_results()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            const int pageSize = 10;
            var pageNumber = 1;
            var remaining = _databaseFixture.MatchLocations.Count;
            while (remaining > 0)
            {
                var result = await matchLocationDataSource.ReadMatchLocations(new MatchLocationQuery { PageNumber = pageNumber, PageSize = pageSize }).ConfigureAwait(false);

                var expected = pageSize > remaining ? remaining : pageSize;
                Assert.Equal(expected, result.Count);

                pageNumber++;
                remaining -= pageSize;
            }
        }
    }
}
