﻿using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Html;
using Stoolball.Logging;
using Stoolball.MatchLocations;
using Stoolball.Routing;
using Xunit;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer.IntegrationTests.MatchLocations
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class SqlServerMatchLocationRepositoryTests : IDisposable
    {
        private readonly SqlServerTestDataFixture _databaseFixture;
        private readonly TransactionScope _scope;
        private readonly Mock<IAuditRepository> _auditRepository = new();
        private readonly Mock<ILogger<SqlServerMatchLocationRepository>> _logger = new();
        private readonly Mock<IRouteGenerator> _routeGenerator = new();
        private readonly Mock<IRedirectsRepository> _redirectsRepository = new();
        private readonly Mock<IHtmlSanitizer> _sanitizer = new();
        private readonly Mock<IStoolballEntityCopier> _copier = new();

        public SqlServerMatchLocationRepositoryTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
            _scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        }
        private SqlServerMatchLocationRepository CreateRepository()
        {
            return new SqlServerMatchLocationRepository(_databaseFixture.ConnectionFactory,
                _auditRepository.Object,
                _logger.Object,
                _routeGenerator.Object,
                _redirectsRepository.Object,
                _sanitizer.Object,
                _copier.Object);
        }

#nullable disable
        [Fact]
        public async Task Create_location_throws_ArgumentNullException_if_location_is_null()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.CreateMatchLocation(null, Guid.NewGuid(), "Member name").ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Create_location_throws_ArgumentNullException_if_memberName_is_null()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.CreateMatchLocation(new MatchLocation(), Guid.NewGuid(), null).ConfigureAwait(false)).ConfigureAwait(false);
        }
#nullable enable 

        [Fact]
        public async Task Create_location_throws_ArgumentNullException_if_memberName_is_empty_string()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.CreateMatchLocation(new MatchLocation(), Guid.NewGuid(), string.Empty).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Create_minimal_location_succeeds()
        {
            var location = new MatchLocation
            {
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = "Test group"
            };

            var route = "/locations/" + Guid.NewGuid();
            _routeGenerator.Setup(x => x.GenerateUniqueRoute("/locations", location.NameAndLocalityOrTownIfDifferent(), NoiseWords.MatchLocationRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(route));

            _copier.Setup(x => x.CreateAuditableCopy(location)).Returns(location);

            var repo = CreateRepository();

            var created = await repo.CreateMatchLocation(location, Guid.NewGuid(), "Member name").ConfigureAwait(false);

            _routeGenerator.Verify(x => x.GenerateUniqueRoute("/locations", location.NameAndLocalityOrTownIfDifferent(), NoiseWords.MatchLocationRoute, It.IsAny<Func<string, Task<int>>>()), Times.Once);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var locationResult = await connection.QuerySingleOrDefaultAsync<MatchLocation>($"SELECT MemberGroupKey, MemberGroupName, MatchLocationRoute FROM {Tables.MatchLocation} WHERE MatchLocationId = @MatchLocationId", new { created.MatchLocationId }).ConfigureAwait(false);
                Assert.NotNull(locationResult);
                Assert.Equal(location.MemberGroupKey, locationResult.MemberGroupKey);
                Assert.Equal(location.MemberGroupName, locationResult.MemberGroupName);
                Assert.Equal(location.MatchLocationRoute, locationResult.MatchLocationRoute);
            }
        }

        [Fact]
        public async Task Create_complete_location_succeeds()
        {
            var originalNotes = "<p>These are unsanitised notes.</p>";
            var sanitisedNotes = "<p>Sanitised notes</p>";

            var location = new MatchLocation
            {
                SecondaryAddressableObjectName = "Pitch 1",
                PrimaryAddressableObjectName = "Test ground",
                StreetDescription = "1 Cricketfield Road",
                Locality = "Test area",
                Town = "Test town",
                AdministrativeArea = "Test county",
                Postcode = "AB1 1CD",
                GeoPrecision = GeoPrecision.Postcode,
                Latitude = 123.456,
                Longitude = 234.567,
                MatchLocationNotes = originalNotes,
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = "Test group"
            };

            var route = "/locations/" + Guid.NewGuid();
            _routeGenerator.Setup(x => x.GenerateUniqueRoute("/locations", location.NameAndLocalityOrTownIfDifferent(), NoiseWords.MatchLocationRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(route));

            _copier.Setup(x => x.CreateAuditableCopy(location)).Returns(location);

            _sanitizer.Setup(x => x.Sanitize(location.MatchLocationNotes)).Returns(sanitisedNotes);

            var repo = CreateRepository();

            var created = await repo.CreateMatchLocation(location, Guid.NewGuid(), "Member name").ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var comparableNameResult = await connection.ExecuteScalarAsync<string>(
                    $@"SELECT ComparableName FROM {Tables.MatchLocation} WHERE MatchLocationId = @MatchLocationId",
                    new { created.MatchLocationId }).ConfigureAwait(false);
                var locationResult = await connection.QuerySingleOrDefaultAsync<MatchLocation>(
                    $@"SELECT SecondaryAddressableObjectName, PrimaryAddressableObjectName, StreetDescription, Locality,
                              Town, AdministrativeArea, Postcode, Latitude, Longitude, GeoPrecision, MatchLocationNotes, 
                              MemberGroupKey, MemberGroupName, MatchLocationRoute 
                              FROM {Tables.MatchLocation} WHERE MatchLocationId = @MatchLocationId",
                    new { created.MatchLocationId }).ConfigureAwait(false);
                Assert.NotNull(locationResult);

                Assert.Equal(location.ComparableName(), comparableNameResult);
                Assert.Equal(location.SecondaryAddressableObjectName, locationResult.SecondaryAddressableObjectName);
                Assert.Equal(location.PrimaryAddressableObjectName, locationResult.PrimaryAddressableObjectName);
                Assert.Equal(location.StreetDescription, locationResult.StreetDescription);
                Assert.Equal(location.Locality, locationResult.Locality);
                Assert.Equal(location.Town, locationResult.Town);
                Assert.Equal(location.AdministrativeArea, locationResult.AdministrativeArea);
                Assert.Equal(location.Postcode, locationResult.Postcode);
                Assert.Equal(location.Latitude, locationResult.Latitude);
                Assert.Equal(location.Longitude, locationResult.Longitude);
                Assert.Equal(location.GeoPrecision, locationResult.GeoPrecision);
                _sanitizer.Verify(x => x.Sanitize(originalNotes));
                Assert.Equal(sanitisedNotes, locationResult.MatchLocationNotes);
                Assert.Equal(location.MemberGroupKey, locationResult.MemberGroupKey);
                Assert.Equal(location.MemberGroupName, locationResult.MemberGroupName);
                Assert.Equal(route, locationResult.MatchLocationRoute);
            }
        }

        [Fact]
        public async Task Create_location_returns_a_copy()
        {
            var location = new MatchLocation
            {
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = "Test group"
            };

            var copyLocation = new MatchLocation
            {
                MemberGroupKey = location.MemberGroupKey,
                MemberGroupName = location.MemberGroupName
            };

            _routeGenerator.Setup(x => x.GenerateUniqueRoute("/locations", location.NameAndLocalityOrTownIfDifferent(), NoiseWords.MatchLocationRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult("/locations/" + Guid.NewGuid()));

            _copier.Setup(x => x.CreateAuditableCopy(location)).Returns(copyLocation);

            var repo = CreateRepository();

            var created = await repo.CreateMatchLocation(location, Guid.NewGuid(), "Member name").ConfigureAwait(false);

            _copier.Verify(x => x.CreateAuditableCopy(location), Times.Once);
            Assert.Equal(copyLocation, created);
        }

        [Fact]
        public async Task Create_location_audits_and_logs()
        {
            var location = new MatchLocation
            {
                PrimaryAddressableObjectName = "New location " + Guid.NewGuid(),
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = "Test group"
            };

            var auditable = new MatchLocation
            {
                PrimaryAddressableObjectName = location.PrimaryAddressableObjectName,
                MemberGroupKey = location.MemberGroupKey,
                MemberGroupName = location.MemberGroupName
            };

            var redacted = new MatchLocation
            {
                PrimaryAddressableObjectName = location.PrimaryAddressableObjectName,
                MemberGroupKey = location.MemberGroupKey,
                MemberGroupName = location.MemberGroupName
            };

            var route = "/locations/" + Guid.NewGuid();
            _routeGenerator.Setup(x => x.GenerateUniqueRoute("/locations", location.NameAndLocalityOrTownIfDifferent(), NoiseWords.MatchLocationRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(route));
            _copier.Setup(x => x.CreateAuditableCopy(location)).Returns(auditable);
            _copier.Setup(x => x.CreateRedactedCopy(auditable)).Returns(redacted);

            var repo = CreateRepository();
            var memberKey = Guid.NewGuid();
            var memberName = "Person 1";

            var created = await repo.CreateMatchLocation(location, memberKey, memberName).ConfigureAwait(false);

            _copier.Verify(x => x.CreateRedactedCopy(auditable), Times.Once);
            _auditRepository.Verify(x => x.CreateAudit(It.IsAny<AuditRecord>(), It.IsAny<IDbTransaction>()), Times.Once);
            _logger.Verify(x => x.Info(LoggingTemplates.Created, redacted, memberName, memberKey, typeof(SqlServerMatchLocationRepository), nameof(SqlServerMatchLocationRepository.CreateMatchLocation)));
        }

#nullable disable
        [Fact]
        public async Task Update_location_throws_ArgumentNullException_if_location_is_null()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.UpdateMatchLocation(null, Guid.NewGuid(), "Member name").ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Update_location_throws_ArgumentNullException_if_memberName_is_null()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.UpdateMatchLocation(new MatchLocation(), Guid.NewGuid(), null).ConfigureAwait(false)).ConfigureAwait(false);
        }
#nullable enable

        [Fact]
        public async Task Update_location_throws_ArgumentNullException_if_memberName_is_empty_string()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.UpdateMatchLocation(new MatchLocation(), Guid.NewGuid(), string.Empty).ConfigureAwait(false)).ConfigureAwait(false);
        }


        [Fact]
        public async Task Update_location_returns_a_copy()
        {
            var location = new MatchLocation
            {
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = "Test group"
            };

            var copyLocation = new MatchLocation
            {
                MemberGroupKey = location.MemberGroupKey,
                MemberGroupName = location.MemberGroupName
            };

            _routeGenerator.Setup(x => x.GenerateUniqueRoute(location.MatchLocationRoute, "/locations", location.NameAndLocalityOrTownIfDifferent(), NoiseWords.MatchLocationRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult("/locations/" + Guid.NewGuid()));

            _copier.Setup(x => x.CreateAuditableCopy(location)).Returns(copyLocation);

            var repo = CreateRepository();

            var updated = await repo.UpdateMatchLocation(location, Guid.NewGuid(), "Member name").ConfigureAwait(false);

            _copier.Verify(x => x.CreateAuditableCopy(location), Times.Once);
            Assert.Equal(copyLocation, updated);
        }

        [Fact]
        public async Task Update_location_succeeds()
        {
            var location = _databaseFixture.TestData.MatchLocations.First();
            var auditable = new MatchLocation
            {
                MatchLocationId = location.MatchLocationId,
                SecondaryAddressableObjectName = Guid.NewGuid().ToString(),
                PrimaryAddressableObjectName = Guid.NewGuid().ToString(),
                StreetDescription = Guid.NewGuid().ToString(),
                Locality = Guid.NewGuid().ToString().Substring(0, 35),
                Town = Guid.NewGuid().ToString().Substring(0, 30),
                AdministrativeArea = Guid.NewGuid().ToString().Substring(0, 30),
                Postcode = new string(location.Postcode.Reverse().ToArray()),
                GeoPrecision = location.GeoPrecision == GeoPrecision.Exact ? GeoPrecision.Street : GeoPrecision.Exact,
                Latitude = location.Latitude.HasValue ? location.Latitude + 3.5 : 3.5,
                Longitude = location.Longitude.HasValue ? location.Longitude + 5.1 : 5.1,
                MatchLocationNotes = location.MatchLocationNotes ?? "Unsanitised notes"
            };

            var updatedRoute = location.MatchLocationRoute + Guid.NewGuid();

            _routeGenerator.Setup(x => x.GenerateUniqueRoute(location.MatchLocationRoute, "/locations", auditable.NameAndLocalityOrTownIfDifferent(), NoiseWords.MatchLocationRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(updatedRoute));

            _copier.Setup(x => x.CreateAuditableCopy(location)).Returns(auditable);

            var originalNotes = auditable.MatchLocationNotes;
            var sanitisedNotes = location.MatchLocationNotes + Guid.NewGuid();
            _sanitizer.Setup(x => x.Sanitize(auditable.MatchLocationNotes)).Returns(sanitisedNotes);

            var repo = CreateRepository();

            var updated = await repo.UpdateMatchLocation(location, Guid.NewGuid(), "Person 1").ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var comparableNameResult = await connection.ExecuteScalarAsync<string>(
                    $@"SELECT ComparableName FROM {Tables.MatchLocation} WHERE MatchLocationId = @MatchLocationId",
                    new { updated.MatchLocationId }).ConfigureAwait(false);
                var locationResult = await connection.QuerySingleOrDefaultAsync<MatchLocation>(
                    $@"SELECT SecondaryAddressableObjectName, PrimaryAddressableObjectName, StreetDescription, Locality,
                              Town, AdministrativeArea, Postcode, Latitude, Longitude, GeoPrecision, MatchLocationNotes, 
                              MemberGroupKey, MemberGroupName, MatchLocationRoute 
                              FROM {Tables.MatchLocation} WHERE MatchLocationId = @MatchLocationId",
                    new { updated.MatchLocationId }).ConfigureAwait(false);
                Assert.NotNull(locationResult);

                Assert.Equal(auditable.ComparableName(), comparableNameResult);
                Assert.Equal(auditable.SecondaryAddressableObjectName, locationResult.SecondaryAddressableObjectName);
                Assert.Equal(auditable.PrimaryAddressableObjectName, locationResult.PrimaryAddressableObjectName);
                Assert.Equal(auditable.StreetDescription, locationResult.StreetDescription);
                Assert.Equal(auditable.Locality, locationResult.Locality);
                Assert.Equal(auditable.Town, locationResult.Town);
                Assert.Equal(auditable.AdministrativeArea, locationResult.AdministrativeArea);
                Assert.Equal(auditable.Postcode, locationResult.Postcode);
                Assert.Equal(auditable.Latitude, locationResult.Latitude);
                Assert.Equal(auditable.Longitude, locationResult.Longitude);
                Assert.Equal(auditable.GeoPrecision, locationResult.GeoPrecision);
                _sanitizer.Verify(x => x.Sanitize(originalNotes));
                Assert.Equal(sanitisedNotes, locationResult.MatchLocationNotes);
                Assert.Equal(updatedRoute, locationResult.MatchLocationRoute);
            }

        }

        [Fact]
        public async Task Update_location_updates_route_and_inserts_redirect()
        {
            var location = _databaseFixture.TestData.MatchLocations.First();
            var auditable = new MatchLocation
            {
                MatchLocationId = location.MatchLocationId,
                MatchLocationRoute = location.MatchLocationRoute
            };

            var updatedRoute = location.MatchLocationRoute + "-updated";
            _routeGenerator.Setup(x => x.GenerateUniqueRoute(location.MatchLocationRoute, "/locations", auditable.NameAndLocalityOrTownIfDifferent(), NoiseWords.MatchLocationRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(updatedRoute));
            _copier.Setup(x => x.CreateAuditableCopy(location)).Returns(auditable);

            var repo = CreateRepository();

            var updated = await repo.UpdateMatchLocation(location, Guid.NewGuid(), "Person 1").ConfigureAwait(false);

            _routeGenerator.Verify(x => x.GenerateUniqueRoute(location.MatchLocationRoute, "/locations", auditable.NameAndLocalityOrTownIfDifferent(), NoiseWords.MatchLocationRoute, It.IsAny<Func<string, Task<int>>>()), Times.Once);
            _redirectsRepository.Verify(x => x.InsertRedirect(location.MatchLocationRoute, updatedRoute, null, It.IsAny<IDbTransaction>()), Times.Once);
        }


        [Fact]
        public async Task Update_location_does_not_redirect_unchanged_route()
        {
            var location = _databaseFixture.TestData.MatchLocations.First();
            var auditable = new MatchLocation
            {
                MatchLocationId = location.MatchLocationId,
                MatchLocationRoute = location.MatchLocationRoute
            };

            _routeGenerator.Setup(x => x.GenerateUniqueRoute(location.MatchLocationRoute, "/locations", auditable.NameAndLocalityOrTownIfDifferent(), NoiseWords.MatchLocationRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(location.MatchLocationRoute));
            _copier.Setup(x => x.CreateAuditableCopy(location)).Returns(auditable);

            var repo = CreateRepository();

            var updated = await repo.UpdateMatchLocation(location, Guid.NewGuid(), "Person 1").ConfigureAwait(false);

            _redirectsRepository.Verify(x => x.InsertRedirect(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDbTransaction>()), Times.Never);
        }

        [Fact]
        public async Task Update_location_audits_and_logs()
        {
            var location = _databaseFixture.TestData.MatchLocations.First();
            var auditable = new MatchLocation
            {
                MatchLocationId = location.MatchLocationId,
                MatchLocationRoute = location.MatchLocationRoute,
                PrimaryAddressableObjectName = location.PrimaryAddressableObjectName
            };
            var redacted = new MatchLocation
            {
                MatchLocationId = location.MatchLocationId,
                MatchLocationRoute = location.MatchLocationRoute,
                PrimaryAddressableObjectName = location.PrimaryAddressableObjectName
            };

            _routeGenerator.Setup(x => x.GenerateUniqueRoute(location.MatchLocationRoute, "/locations", auditable.NameAndLocalityOrTownIfDifferent(), NoiseWords.MatchLocationRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(location.MatchLocationRoute));
            _copier.Setup(x => x.CreateAuditableCopy(location)).Returns(auditable);
            _copier.Setup(x => x.CreateRedactedCopy(auditable)).Returns(redacted);
            var repo = CreateRepository();
            var memberKey = Guid.NewGuid();
            var memberName = "Person 1";

            var updated = await repo.UpdateMatchLocation(location, memberKey, memberName).ConfigureAwait(false);

            _copier.Verify(x => x.CreateRedactedCopy(auditable), Times.Once);
            _auditRepository.Verify(x => x.CreateAudit(It.IsAny<AuditRecord>(), It.IsAny<IDbTransaction>()), Times.Once);
            _logger.Verify(x => x.Info(LoggingTemplates.Updated, redacted, memberName, memberKey, typeof(SqlServerMatchLocationRepository), nameof(SqlServerMatchLocationRepository.UpdateMatchLocation)));
        }

        [Fact]
        public async Task Delete_match_location_succeeds()
        {
            _copier.Setup(x => x.CreateAuditableCopy(_databaseFixture.TestData.MatchLocationWithFullDetails))
                  .Returns(new MatchLocation
                  {
                      MatchLocationId = _databaseFixture.TestData.MatchLocationWithFullDetails!.MatchLocationId,
                      SecondaryAddressableObjectName = _databaseFixture.TestData.MatchLocationWithFullDetails.SecondaryAddressableObjectName,
                      PrimaryAddressableObjectName = _databaseFixture.TestData.MatchLocationWithFullDetails.PrimaryAddressableObjectName,
                      StreetDescription = _databaseFixture.TestData.MatchLocationWithFullDetails.StreetDescription,
                      Locality = _databaseFixture.TestData.MatchLocationWithFullDetails.Locality,
                      Town = _databaseFixture.TestData.MatchLocationWithFullDetails.Town,
                      AdministrativeArea = _databaseFixture.TestData.MatchLocationWithFullDetails.AdministrativeArea,
                      Postcode = _databaseFixture.TestData.MatchLocationWithFullDetails.Postcode
                  });

            var memberKey = Guid.NewGuid();
            var memberName = "Dee Leeter";

            var repo = CreateRepository();

            await repo.DeleteMatchLocation(_databaseFixture.TestData.MatchLocationWithFullDetails, memberKey, memberName).ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<Guid?>($"SELECT MatchLocationId FROM {Tables.MatchLocation} WHERE MatchLocationId = @MatchLocationId", new { _databaseFixture.TestData.MatchLocationWithFullDetails.MatchLocationId }).ConfigureAwait(false);
                Assert.Null(result);
            }
        }
        public void Dispose() => _scope.Dispose();
    }
}
