using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.MatchLocations;
using Stoolball.Navigation;
using Stoolball.Routing;
using Stoolball.Teams;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.MatchLocations
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class SqlServerMatchLocationDataSourceTests
    {
        private readonly SqlServerTestDataFixture _databaseFixture;
        private readonly Mock<IRouteNormaliser> _routeNormaliser = new();

        public SqlServerMatchLocationDataSourceTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_minimal_location_by_route_returns_basic_fields()
        {
            _routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.MatchLocationWithMinimalDetails!.MatchLocationRoute!, "locations")).Returns(_databaseFixture.TestData.MatchLocationWithMinimalDetails!.MatchLocationRoute!);
            var locationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, _routeNormaliser.Object);

            var result = await locationDataSource.ReadMatchLocationByRoute(_databaseFixture.TestData.MatchLocationWithMinimalDetails!.MatchLocationRoute!, false).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Equal(_databaseFixture.TestData.MatchLocationWithMinimalDetails.MatchLocationId, result!.MatchLocationId);
            Assert.Equal(_databaseFixture.TestData.MatchLocationWithMinimalDetails!.MatchLocationRoute!, result.MatchLocationRoute);
            Assert.Equal(_databaseFixture.TestData.MatchLocationWithMinimalDetails.SecondaryAddressableObjectName, result.SecondaryAddressableObjectName);
            Assert.Equal(_databaseFixture.TestData.MatchLocationWithMinimalDetails.PrimaryAddressableObjectName, result.PrimaryAddressableObjectName);
            Assert.Equal(_databaseFixture.TestData.MatchLocationWithMinimalDetails.StreetDescription, result.StreetDescription);
            Assert.Equal(_databaseFixture.TestData.MatchLocationWithMinimalDetails.Locality, result.Locality);
            Assert.Equal(_databaseFixture.TestData.MatchLocationWithMinimalDetails.Town, result.Town);
            Assert.Equal(_databaseFixture.TestData.MatchLocationWithMinimalDetails.AdministrativeArea, result.AdministrativeArea);
            Assert.Equal(_databaseFixture.TestData.MatchLocationWithMinimalDetails.Postcode, result.Postcode);
            Assert.Equal(_databaseFixture.TestData.MatchLocationWithMinimalDetails.Latitude, result.Latitude);
            Assert.Equal(_databaseFixture.TestData.MatchLocationWithMinimalDetails.Longitude, result.Longitude);
            Assert.Equal(_databaseFixture.TestData.MatchLocationWithMinimalDetails.GeoPrecision, result.GeoPrecision);
            Assert.Equal(_databaseFixture.TestData.MatchLocationWithMinimalDetails.MatchLocationNotes, result.MatchLocationNotes);
            Assert.Equal(_databaseFixture.TestData.MatchLocationWithMinimalDetails.MemberGroupName, result.MemberGroupName);
        }

        [Fact]
        public async Task Read_minimal_location_by_route_with_related_returns_basic_fields()
        {
            _routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.MatchLocationWithMinimalDetails!.MatchLocationRoute!, "locations")).Returns(_databaseFixture.TestData.MatchLocationWithMinimalDetails!.MatchLocationRoute!);
            var locationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, _routeNormaliser.Object);

            var result = await locationDataSource.ReadMatchLocationByRoute(_databaseFixture.TestData.MatchLocationWithMinimalDetails!.MatchLocationRoute!, true).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Equal(_databaseFixture.TestData.MatchLocationWithMinimalDetails.MatchLocationId, result!.MatchLocationId);
            Assert.Equal(_databaseFixture.TestData.MatchLocationWithMinimalDetails!.MatchLocationRoute!, result.MatchLocationRoute);
            Assert.Equal(_databaseFixture.TestData.MatchLocationWithMinimalDetails.SecondaryAddressableObjectName, result.SecondaryAddressableObjectName);
            Assert.Equal(_databaseFixture.TestData.MatchLocationWithMinimalDetails.PrimaryAddressableObjectName, result.PrimaryAddressableObjectName);
            Assert.Equal(_databaseFixture.TestData.MatchLocationWithMinimalDetails.StreetDescription, result.StreetDescription);
            Assert.Equal(_databaseFixture.TestData.MatchLocationWithMinimalDetails.Locality, result.Locality);
            Assert.Equal(_databaseFixture.TestData.MatchLocationWithMinimalDetails.Town, result.Town);
            Assert.Equal(_databaseFixture.TestData.MatchLocationWithMinimalDetails.AdministrativeArea, result.AdministrativeArea);
            Assert.Equal(_databaseFixture.TestData.MatchLocationWithMinimalDetails.Postcode, result.Postcode);
            Assert.Equal(_databaseFixture.TestData.MatchLocationWithMinimalDetails.Latitude, result.Latitude);
            Assert.Equal(_databaseFixture.TestData.MatchLocationWithMinimalDetails.Longitude, result.Longitude);
            Assert.Equal(_databaseFixture.TestData.MatchLocationWithMinimalDetails.GeoPrecision, result.GeoPrecision);
            Assert.Equal(_databaseFixture.TestData.MatchLocationWithMinimalDetails.MatchLocationNotes, result.MatchLocationNotes);
            Assert.Equal(_databaseFixture.TestData.MatchLocationWithMinimalDetails.MemberGroupKey, result.MemberGroupKey);
            Assert.Equal(_databaseFixture.TestData.MatchLocationWithMinimalDetails.MemberGroupName, result.MemberGroupName);
        }

        [Fact]
        public async Task Read_location_by_route_sorts_active_teams_first()
        {
            _routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.MatchLocationWithFullDetails!.MatchLocationRoute!, "locations")).Returns(_databaseFixture.TestData.MatchLocationWithFullDetails!.MatchLocationRoute!);
            var locationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, _routeNormaliser.Object);

            var result = await locationDataSource.ReadMatchLocationByRoute(_databaseFixture.TestData.MatchLocationWithFullDetails!.MatchLocationRoute!, true).ConfigureAwait(false);

            Assert.NotNull(result);
            var expectedActiveStatus = true;
            foreach (var team in result!.Teams)
            {
                // The first time an inactive team is seen, set a flag to say they must all be inactive
                if (expectedActiveStatus && team.UntilYear.HasValue)
                {
                    expectedActiveStatus = false;
                }
                Assert.Equal(expectedActiveStatus, !team.UntilYear.HasValue);
            }
            Assert.False(expectedActiveStatus);
        }

        [Fact]
        public async Task Read_location_by_route_excludes_transient_teams()
        {
            _routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.MatchLocationWithFullDetails!.MatchLocationRoute!, "locations")).Returns(_databaseFixture.TestData.MatchLocationWithFullDetails!.MatchLocationRoute!);
            var locationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, _routeNormaliser.Object);

            var result = await locationDataSource.ReadMatchLocationByRoute(_databaseFixture.TestData.MatchLocationWithFullDetails!.MatchLocationRoute!, true).ConfigureAwait(false);

            Assert.NotNull(result);
            foreach (var team in _databaseFixture.TestData.MatchLocationWithFullDetails.Teams.Where(x => x.TeamType == TeamType.Transient))
            {
                Assert.Null(result!.Teams.SingleOrDefault(x => x.TeamId == team.TeamId));
            }
        }

        [Fact]
        public async Task Read_total_locations_supports_no_filter()
        {
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, _routeNormaliser.Object);

            var result = await matchLocationDataSource.ReadTotalMatchLocations(null).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.MatchLocations.Count, result);
        }

        [Fact]
        public async Task Read_total_locations_supports_case_insensitive_filter_SAON()
        {
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, _routeNormaliser.Object);

            var uniqueSAONs = _databaseFixture.TestData.Teams.SelectMany(x => x.MatchLocations)
                .Where(x => !string.IsNullOrEmpty(x.SecondaryAddressableObjectName)).Select(x => x.SecondaryAddressableObjectName).OfType<string>()
                .Distinct()
                .ToList()
                .ChangeCaseAndSometimesTrimOneEnd();

            foreach (var saon in uniqueSAONs)
            {
                var query = new MatchLocationFilter { Query = saon };

                var result = await matchLocationDataSource.ReadTotalMatchLocations(query).ConfigureAwait(false);

                Assert.Equal(_databaseFixture.TestData.MatchLocations.Count(x => x.SecondaryAddressableObjectName?.Contains(query.Query, StringComparison.OrdinalIgnoreCase) ?? false), result);
            }
        }

        [Fact]
        public async Task Read_total_locations_supports_case_insensitive_filter_PAON()
        {
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, _routeNormaliser.Object);

            var uniquePAONs = _databaseFixture.TestData.Teams.SelectMany(x => x.MatchLocations)
                .Where(x => !string.IsNullOrEmpty(x.PrimaryAddressableObjectName)).Select(x => x.PrimaryAddressableObjectName).OfType<string>()
                .Distinct()
                .ToList()
                .ChangeCaseAndSometimesTrimOneEnd();

            foreach (var paon in uniquePAONs)
            {
                var query = new MatchLocationFilter { Query = paon };

                var result = await matchLocationDataSource.ReadTotalMatchLocations(query).ConfigureAwait(false);

                Assert.Equal(_databaseFixture.TestData.MatchLocations.Count(x => x.PrimaryAddressableObjectName?.Contains(query.Query, StringComparison.OrdinalIgnoreCase) ?? false), result);
            }
        }


        [Fact]
        public async Task Read_total_locations_supports_case_insensitive_filter_locality()
        {
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, _routeNormaliser.Object);

            var uniqueLocalities = _databaseFixture.TestData.Teams.SelectMany(x => x.MatchLocations)
                .Where(x => !string.IsNullOrEmpty(x.Locality)).Select(x => x.Locality).OfType<string>()
                .Distinct()
                .ToList()
                .ChangeCaseAndSometimesTrimOneEnd();

            foreach (var locality in uniqueLocalities)
            {
                var query = new MatchLocationFilter { Query = locality };

                var result = await matchLocationDataSource.ReadTotalMatchLocations(query).ConfigureAwait(false);

                Assert.Equal(_databaseFixture.TestData.MatchLocations.Count(x => x.Locality?.Contains(query.Query, StringComparison.OrdinalIgnoreCase) ?? false), result);
            }
        }

        [Fact]
        public async Task Read_total_locations_supports_case_insensitive_filter_town()
        {
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, _routeNormaliser.Object);

            var uniqueTowns = _databaseFixture.TestData.Teams.SelectMany(x => x.MatchLocations)
                .Where(x => !string.IsNullOrEmpty(x.Town)).Select(x => x.Town).OfType<string>()
                .Distinct()
                .ToList()
                .ChangeCaseAndSometimesTrimOneEnd();

            foreach (var town in uniqueTowns)
            {
                var query = new MatchLocationFilter { Query = town };

                var result = await matchLocationDataSource.ReadTotalMatchLocations(query).ConfigureAwait(false);

                Assert.Equal(_databaseFixture.TestData.MatchLocations.Count(x => x.Town?.Contains(query.Query, StringComparison.OrdinalIgnoreCase) ?? false), result);
            }
        }

        [Fact]
        public async Task Read_total_locations_supports_excluding_locations_by_id()
        {
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, _routeNormaliser.Object);

            var result = await matchLocationDataSource.ReadTotalMatchLocations(new MatchLocationFilter { ExcludeMatchLocationIds = new List<Guid> { _databaseFixture.TestData.MatchLocationWithMinimalDetails!.MatchLocationId!.Value } }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.MatchLocations.Count - 1, result);
        }

        [Fact]
        public async Task Read_total_locations_supports_excluding_locations_with_no_active_teams()
        {
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, _routeNormaliser.Object);

            var result = await matchLocationDataSource.ReadTotalMatchLocations(new MatchLocationFilter { HasActiveTeams = true }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.MatchLocations.Count(x => x.Teams.Any(t => !t.UntilYear.HasValue)), result);
        }

        [Fact]
        public async Task Read_total_locations_supports_filter_by_team_type()
        {
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, _routeNormaliser.Object);

            var result = await matchLocationDataSource.ReadTotalMatchLocations(new MatchLocationFilter { TeamTypes = new List<TeamType> { TeamType.Regular } }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.MatchLocations.Count(x => x.Teams.Any(t => t.TeamType == TeamType.Regular)), result);
        }

        [Fact]
        public async Task Read_match_locations_returns_basic_fields()
        {
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, _routeNormaliser.Object);

            var results = await matchLocationDataSource.ReadMatchLocations(new MatchLocationFilter { Paging = new Paging { PageSize = _databaseFixture.TestData.MatchLocations.Count } }).ConfigureAwait(false);

            foreach (var location in _databaseFixture.TestData.MatchLocations)
            {
                var result = results.SingleOrDefault(x => x.MatchLocationId == location.MatchLocationId);

                Assert.NotNull(result);
                Assert.Equal(location.MatchLocationRoute, result!.MatchLocationRoute);
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
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, _routeNormaliser.Object);

            var results = await matchLocationDataSource.ReadMatchLocations(new MatchLocationFilter { Paging = new Paging { PageSize = _databaseFixture.TestData.MatchLocations.Count } }).ConfigureAwait(false);

            foreach (var location in _databaseFixture.TestData.MatchLocations)
            {
                var result = results.SingleOrDefault(x => x.MatchLocationId == location.MatchLocationId);
                Assert.NotNull(result);

                foreach (var team in location.Teams)
                {
                    var resultTeam = result!.Teams.SingleOrDefault(x => x.TeamId == team.TeamId);
                    Assert.NotNull(resultTeam);

                    Assert.Equal(team.TeamName, resultTeam!.TeamName);
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
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, _routeNormaliser.Object);

            var results = await matchLocationDataSource.ReadMatchLocations(new MatchLocationFilter { Paging = new Paging { PageSize = _databaseFixture.TestData.MatchLocations.Count } }).ConfigureAwait(false);

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
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, _routeNormaliser.Object);

            var result = await matchLocationDataSource.ReadMatchLocations(new MatchLocationFilter { Paging = new Paging { PageSize = _databaseFixture.TestData.MatchLocations.Count } }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.MatchLocations.Count, result.Count);
            foreach (var location in _databaseFixture.TestData.MatchLocations)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.MatchLocationId == location.MatchLocationId));
            }
        }

        [Fact]
        public async Task Read_match_locations_supports_case_insensitive_filter_SAON()
        {
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, _routeNormaliser.Object);

            var uniqueSAONs = _databaseFixture.TestData.Teams.SelectMany(x => x.MatchLocations)
                .Where(x => !string.IsNullOrEmpty(x.SecondaryAddressableObjectName)).Select(x => x.SecondaryAddressableObjectName).OfType<string>()
                .Distinct()
                .ToList()
                .ChangeCaseAndSometimesTrimOneEnd();

            foreach (var saon in uniqueSAONs)
            {
                var query = new MatchLocationFilter { Paging = new Paging { PageSize = int.MaxValue }, Query = saon };

                var result = await matchLocationDataSource.ReadMatchLocations(query).ConfigureAwait(false);

                var expected = _databaseFixture.TestData.MatchLocations.Where(x => x.SecondaryAddressableObjectName?.Contains(saon, StringComparison.OrdinalIgnoreCase) ?? false);
                Assert.Equal(expected.Count(), result.Count);
                foreach (var location in expected)
                {
                    Assert.NotNull(result.SingleOrDefault(x => x.MatchLocationId == location.MatchLocationId));
                }
            }
        }

        [Fact]
        public async Task Read_match_locations_supports_case_insensitive_filter_PAON()
        {
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, _routeNormaliser.Object);

            var uniquePAONs = _databaseFixture.TestData.Teams.SelectMany(x => x.MatchLocations)
                .Where(x => !string.IsNullOrEmpty(x.PrimaryAddressableObjectName)).Select(x => x.PrimaryAddressableObjectName).OfType<string>()
                .Distinct()
                .ToList()
                .ChangeCaseAndSometimesTrimOneEnd();

            foreach (var paon in uniquePAONs)
            {
                var query = new MatchLocationFilter { Paging = new Paging { PageSize = int.MaxValue }, Query = paon };

                var result = await matchLocationDataSource.ReadMatchLocations(query).ConfigureAwait(false);

                var expected = _databaseFixture.TestData.MatchLocations.Where(x => x.PrimaryAddressableObjectName?.Contains(paon, StringComparison.OrdinalIgnoreCase) ?? false);
                Assert.Equal(expected.Count(), result.Count);
                foreach (var location in expected)
                {
                    Assert.NotNull(result.SingleOrDefault(x => x.MatchLocationId == location.MatchLocationId));
                }
            }
        }


        [Fact]
        public async Task Read_match_locations_supports_case_insensitive_filter_locality()
        {
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, _routeNormaliser.Object);

            var uniqueLocalities = _databaseFixture.TestData.Teams.SelectMany(x => x.MatchLocations)
                .Where(x => !string.IsNullOrEmpty(x.Locality)).Select(x => x.Locality).OfType<string>()
                .Distinct()
                .ToList()
                .ChangeCaseAndSometimesTrimOneEnd();

            foreach (var locality in uniqueLocalities)
            {
                var query = new MatchLocationFilter { Paging = new Paging { PageSize = int.MaxValue }, Query = locality };

                var result = await matchLocationDataSource.ReadMatchLocations(query).ConfigureAwait(false);

                var expected = _databaseFixture.TestData.MatchLocations.Where(x => x.Locality?.Contains(locality, StringComparison.OrdinalIgnoreCase) ?? false);
                Assert.Equal(expected.Count(), result.Count);
                foreach (var location in expected)
                {
                    Assert.NotNull(result.SingleOrDefault(x => x.MatchLocationId == location.MatchLocationId));
                }
            }
        }

        [Fact]
        public async Task Read_match_locations_supports_case_insensitive_filter_town()
        {
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, _routeNormaliser.Object);

            var uniqueTowns = _databaseFixture.TestData.Teams.SelectMany(x => x.MatchLocations)
                .Where(x => !string.IsNullOrEmpty(x.Town)).Select(x => x.Town).OfType<string>()
                .Distinct()
                .ToList()
                .ChangeCaseAndSometimesTrimOneEnd();

            foreach (var town in uniqueTowns)
            {
                var query = new MatchLocationFilter { Paging = new Paging { PageSize = int.MaxValue }, Query = town };

                var result = await matchLocationDataSource.ReadMatchLocations(query).ConfigureAwait(false);

                var expected = _databaseFixture.TestData.MatchLocations.Where(x => x.Town?.Contains(town, StringComparison.OrdinalIgnoreCase) ?? false);
                Assert.Equal(expected.Count(), result.Count);
                foreach (var location in expected)
                {
                    Assert.NotNull(result.SingleOrDefault(x => x.MatchLocationId == location.MatchLocationId));
                }
            }
        }

        [Fact]
        public async Task Read_match_locations_supports_excluding_locations_by_id()
        {
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, _routeNormaliser.Object);
            var query = new MatchLocationFilter
            {
                Paging = new Paging
                {
                    PageSize = _databaseFixture.TestData.MatchLocations.Count
                },
                ExcludeMatchLocationIds = new List<Guid> { _databaseFixture.TestData.MatchLocationWithMinimalDetails!.MatchLocationId!.Value }
            };

            var result = await matchLocationDataSource.ReadMatchLocations(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.MatchLocations.Count - 1, result.Count);
            Assert.Null(result.SingleOrDefault(x => x.MatchLocationId == _databaseFixture.TestData.MatchLocationWithMinimalDetails.MatchLocationId));
        }

        [Fact]
        public async Task Read_match_locations_supports_excluding_locations_with_no_active_teams()
        {
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, _routeNormaliser.Object);
            var query = new MatchLocationFilter
            {
                Paging = new Paging
                {
                    PageSize = _databaseFixture.TestData.MatchLocations.Count
                },
                HasActiveTeams = true
            };

            var results = await matchLocationDataSource.ReadMatchLocations(query).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.MatchLocations.Where(x => x.Teams.Any(t => !t.UntilYear.HasValue)).ToList();

            var missing = results.Where(x => !expected.Select(ml => ml.MatchLocationId).Contains(x.MatchLocationId)).ToList();


            Assert.Equal(expected.Count, results.Count);
            foreach (var location in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.MatchLocationId == location.MatchLocationId));
            }
        }

        [Fact]
        public async Task Read_match_locations_supports_filter_by_team_type()
        {
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, _routeNormaliser.Object);
            var query = new MatchLocationFilter
            {
                Paging = new Paging
                {
                    PageSize = _databaseFixture.TestData.MatchLocations.Count
                },
                TeamTypes = new List<TeamType> { TeamType.Regular }
            };

            var results = await matchLocationDataSource.ReadMatchLocations(query).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.MatchLocations.Count(x => x.Teams.Any(t => t.TeamType == TeamType.Regular)), results.Count);
            foreach (var result in results)
            {
                Assert.Empty(result.Teams.Where(x => x.TeamType != TeamType.Regular));
            }
        }

        [Fact]
        public async Task Read_match_locations_supports_filter_by_season()
        {
            var season = _databaseFixture.TestData.Seasons.First(x => x.Teams.Any(t => t.Team?.MatchLocations.Any() ?? false));
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, _routeNormaliser.Object);
            var query = new MatchLocationFilter
            {
                Paging = new Paging
                {
                    PageSize = _databaseFixture.TestData.MatchLocations.Count
                },
                SeasonIds = new List<Guid> { season.SeasonId!.Value }
            };

            var results = await matchLocationDataSource.ReadMatchLocations(query).ConfigureAwait(false);

            var teamIdsInSeason = season.Teams.Select(st => st.Team?.TeamId);
            Assert.Equal(_databaseFixture.TestData.MatchLocations.Count(x => x.Teams.Any(t => teamIdsInSeason.Contains(t.TeamId))), results.Count);
            foreach (var result in results)
            {
                Assert.NotEmpty(result.Teams);
                foreach (var team in result.Teams)
                {
                    Assert.Contains(team.TeamId, teamIdsInSeason);
                }
            }
        }

        [Fact]
        public async Task Read_match_locations_pages_results()
        {
            var matchLocationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, _routeNormaliser.Object);

            const int pageSize = 10;
            var pageNumber = 1;
            var remaining = _databaseFixture.TestData.MatchLocations.Count;
            while (remaining > 0)
            {
                var result = await matchLocationDataSource.ReadMatchLocations(new MatchLocationFilter { Paging = new Paging { PageNumber = pageNumber, PageSize = pageSize } }).ConfigureAwait(false);

                var expected = pageSize > remaining ? remaining : pageSize;
                Assert.Equal(expected, result.Count);

                pageNumber++;
                remaining -= pageSize;
            }
        }
    }
}
