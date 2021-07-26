using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Moq;
using Stoolball.Clubs;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Logging;
using Stoolball.Routing;
using Stoolball.Teams;
using Xunit;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer.IntegrationTests.Clubs
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class SqlServerClubRepositoryTests : IDisposable
    {
        private readonly SqlServerTestDataFixture _databaseFixture;
        private readonly TransactionScope _scope;

        public SqlServerClubRepositoryTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
            _scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        }

        private class ClubVersion
        {
            internal string ClubName { get; set; }
            internal string ComparableName { get; set; }
            internal DateTimeOffset FromDate { get; set; }
        }

        [Fact]
        public async Task Create_club_throws_ArgumentNullException_if_club_is_null()
        {
            var repo = new SqlServerClubRepository(_databaseFixture.ConnectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger>(), Mock.Of<IRouteGenerator>(), Mock.Of<IRedirectsRepository>(), Mock.Of<IStoolballEntityCopier>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.CreateClub(null, Guid.NewGuid(), "Member name").ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Create_club_throws_ArgumentNullException_if_memberName_is_null()
        {
            var repo = new SqlServerClubRepository(_databaseFixture.ConnectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger>(), Mock.Of<IRouteGenerator>(), Mock.Of<IRedirectsRepository>(), Mock.Of<IStoolballEntityCopier>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.CreateClub(new Club(), Guid.NewGuid(), null).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Create_club_throws_ArgumentNullException_if_memberName_is_empty_string()
        {
            var repo = new SqlServerClubRepository(_databaseFixture.ConnectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger>(), Mock.Of<IRouteGenerator>(), Mock.Of<IRedirectsRepository>(), Mock.Of<IStoolballEntityCopier>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.CreateClub(new Club(), Guid.NewGuid(), string.Empty).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Create_club_succeeds()
        {
            var club = new Club
            {
                ClubName = "New club " + Guid.NewGuid(),
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = "Test group"
            };

            var route = "/clubs/" + Guid.NewGuid();
            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute("/clubs", club.ClubName, NoiseWords.ClubRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(route));
            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(club)).Returns(club);

            var repo = new SqlServerClubRepository(_databaseFixture.ConnectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger>(), routeGenerator.Object, Mock.Of<IRedirectsRepository>(), copier.Object);

            var createdClub = await repo.CreateClub(club, Guid.NewGuid(), "Person 1").ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var clubResult = await connection.QuerySingleOrDefaultAsync<Club>($"SELECT ClubId, MemberGroupKey, MemberGroupName, ClubRoute FROM {Tables.Club} WHERE ClubId = @ClubId", new { createdClub.ClubId }).ConfigureAwait(false);
                Assert.NotNull(clubResult);
                Assert.Equal(club.MemberGroupKey, clubResult.MemberGroupKey);
                Assert.Equal(club.MemberGroupName, clubResult.MemberGroupName);
                Assert.Equal(club.ClubRoute, clubResult.ClubRoute);

                var versionResult = await connection.QuerySingleOrDefaultAsync<ClubVersion>($"SELECT ClubName, ComparableName, FromDate FROM {Tables.ClubVersion} WHERE ClubId = @ClubId", new { createdClub.ClubId }).ConfigureAwait(false);
                Assert.NotNull(versionResult);
                Assert.Equal(club.ClubName, versionResult.ClubName);
                Assert.Equal(club.ComparableName(), versionResult.ComparableName);
                Assert.Equal(DateTime.UtcNow.Date, versionResult.FromDate.Date);
            }
        }

        [Fact]
        public async Task Create_club_returns_a_copy()
        {
            var club = new Club
            {
                ClubName = "New club " + Guid.NewGuid(),
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = "Test group"
            };

            var copyClub = new Club
            {
                ClubName = club.ClubName,
                MemberGroupKey = club.MemberGroupKey,
                MemberGroupName = club.MemberGroupName
            };

            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute("/clubs", club.ClubName, NoiseWords.ClubRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult("/clubs/" + Guid.NewGuid()));
            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(club)).Returns(copyClub);

            var repo = new SqlServerClubRepository(_databaseFixture.ConnectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger>(), routeGenerator.Object, Mock.Of<IRedirectsRepository>(), copier.Object);

            var createdClub = await repo.CreateClub(club, Guid.NewGuid(), "Person 1").ConfigureAwait(false);

            Assert.Equal(copyClub, createdClub);
        }

        [Fact]
        public async Task Create_club_adds_teams_only_if_no_existing_club()
        {
            var club = new Club
            {
                ClubName = "New club " + Guid.NewGuid(),
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = "Test group"
            };

            var route = "/clubs/" + Guid.NewGuid();
            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute("/clubs", club.ClubName, NoiseWords.ClubRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(route));
            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(club)).Returns(club);

            var teamsToIgnore = _databaseFixture.TestData.Teams.Where(x => x.Club != null).Select(x => new Team { TeamId = x.TeamId }).Take(3);
            var teamsToAdd = _databaseFixture.TestData.Teams.Where(x => x.Club == null).Select(x => new Team { TeamId = x.TeamId }).Take(3);
            club.Teams.AddRange(teamsToIgnore);
            club.Teams.AddRange(teamsToAdd);

            var repo = new SqlServerClubRepository(_databaseFixture.ConnectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger>(), routeGenerator.Object, Mock.Of<IRedirectsRepository>(), copier.Object);

            var createdClub = await repo.CreateClub(club, Guid.NewGuid(), "Person 1").ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var results = await connection.QueryAsync<Guid>($"SELECT TeamId FROM {Tables.Team} WHERE ClubId = @ClubId", new { createdClub.ClubId }).ConfigureAwait(false);
                Assert.Equal(teamsToAdd.Count(), results.Count());
                foreach (var teamId in results)
                {
                    Assert.Contains(teamId, teamsToAdd.Select(x => x.TeamId.Value));
                }
            }
        }

        [Fact]
        public async Task Create_club_audits_and_logs()
        {
            var club = new Club
            {
                ClubName = "New club " + Guid.NewGuid(),
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = "Test group"
            };

            var auditable = new Club
            {
                ClubName = club.ClubName,
                MemberGroupKey = club.MemberGroupKey,
                MemberGroupName = club.MemberGroupName
            };

            var route = "/clubs/" + Guid.NewGuid();
            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute("/clubs", club.ClubName, NoiseWords.ClubRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(route));
            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(club)).Returns(auditable);
            var auditRepository = new Mock<IAuditRepository>();
            var logger = new Mock<ILogger>();

            var repo = new SqlServerClubRepository(_databaseFixture.ConnectionFactory, auditRepository.Object, logger.Object, routeGenerator.Object, Mock.Of<IRedirectsRepository>(), copier.Object);
            var memberKey = Guid.NewGuid();
            var memberName = "Person 1";

            var createdClub = await repo.CreateClub(club, memberKey, memberName).ConfigureAwait(false);

            auditRepository.Verify(x => x.CreateAudit(It.IsAny<AuditRecord>(), It.IsAny<IDbTransaction>()), Times.Once);
            logger.Verify(x => x.Info(typeof(SqlServerClubRepository), LoggingTemplates.Created, auditable, memberName, memberKey, typeof(SqlServerClubRepository), nameof(SqlServerClubRepository.CreateClub)));
        }

        [Fact]
        public async Task Update_club_throws_ArgumentNullException_if_club_is_null()
        {
            var repo = new SqlServerClubRepository(_databaseFixture.ConnectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger>(), Mock.Of<IRouteGenerator>(), Mock.Of<IRedirectsRepository>(), Mock.Of<IStoolballEntityCopier>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.UpdateClub(null, Guid.NewGuid(), "Member name").ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Update_club_throws_ArgumentNullException_if_memberName_is_null()
        {
            var repo = new SqlServerClubRepository(_databaseFixture.ConnectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger>(), Mock.Of<IRouteGenerator>(), Mock.Of<IRedirectsRepository>(), Mock.Of<IStoolballEntityCopier>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.UpdateClub(new Club(), Guid.NewGuid(), null).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Update_club_throws_ArgumentNullException_if_memberName_is_empty_string()
        {
            var repo = new SqlServerClubRepository(_databaseFixture.ConnectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger>(), Mock.Of<IRouteGenerator>(), Mock.Of<IRedirectsRepository>(), Mock.Of<IStoolballEntityCopier>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.UpdateClub(new Club(), Guid.NewGuid(), string.Empty).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact(Skip = "Club versioning is not implemented yet. See https://github.com/stoolball-england/stoolball-org-uk/issues/471")]
        public async Task Update_club_adds_club_version_if_name_changes()
        {
            var club = _databaseFixture.TestData.Teams.Where(x => x.Club != null).First().Club;
            var auditable = new Club
            {
                ClubId = club.ClubId,
                ClubName = club.ClubName + " changed",
                ClubRoute = club.ClubRoute
            };

            int? existingVersions = null;
            Guid? currentVersionId = null;
            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                existingVersions = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.ClubVersion} WHERE ClubId = @ClubId", auditable).ConfigureAwait(false);
                currentVersionId = await connection.ExecuteScalarAsync<Guid>($"SELECT ClubVersionId FROM {Tables.ClubVersion} WHERE ClubId = @ClubId AND UntilDate IS NULL", auditable).ConfigureAwait(false);
            }

            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(club.ClubRoute, "/clubs", club.ClubName, NoiseWords.ClubRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(club.ClubRoute));
            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(club)).Returns(auditable);

            var repo = new SqlServerClubRepository(_databaseFixture.ConnectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger>(), routeGenerator.Object, Mock.Of<IRedirectsRepository>(), copier.Object);

            var updated = await repo.UpdateClub(club, Guid.NewGuid(), "Person 1").ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var totalVersions = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.ClubVersion} WHERE ClubId = @ClubId", auditable).ConfigureAwait(false);
                Assert.Equal(existingVersions + 1, totalVersions);

                var versionResult = await connection.QuerySingleAsync<(Guid clubVersionId, string clubName, string comparableName, DateTimeOffset? fromDate)>(
                    $"SELECT ClubVersionId, ClubName, ComparableName, FromDate FROM {Tables.ClubVersion} WHERE ClubId = @ClubId AND UntilDate IS NULL", auditable).ConfigureAwait(false);

                Assert.NotEqual(currentVersionId, versionResult.clubVersionId);
                Assert.Equal(auditable.ClubName, versionResult.clubName);
                Assert.Equal(auditable.ComparableName(), versionResult.comparableName);
                Assert.Equal(DateTime.UtcNow.Date, versionResult.fromDate.Value);
            }
        }

        [Fact]
        public async Task Update_club_does_not_add_club_version_if_name_is_unchanged()
        {
            var club = _databaseFixture.TestData.Teams.Where(x => x.Club != null).First().Club;
            var auditable = new Club
            {
                ClubId = club.ClubId,
                ClubName = club.ClubName,
                ClubRoute = club.ClubRoute
            };

            int? existingVersions = null;
            (Guid? clubVersionId, string clubName, string comparableName, DateTimeOffset? fromDate) currentVersion = (null, null, null, null);
            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                existingVersions = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.ClubVersion} WHERE ClubId = @ClubId", auditable).ConfigureAwait(false);
                currentVersion = await connection.QuerySingleAsync<(Guid clubVersionId, string clubName, string comparableName, DateTimeOffset? fromDate)>(
                    $"SELECT ClubVersionId, ClubName, ComparableName, FromDate FROM {Tables.ClubVersion} WHERE ClubId = @ClubId AND UntilDate IS NULL", auditable).ConfigureAwait(false);
            }

            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(club.ClubRoute, "/clubs", club.ClubName, NoiseWords.ClubRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(club.ClubRoute));
            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(club)).Returns(auditable);

            var repo = new SqlServerClubRepository(_databaseFixture.ConnectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger>(), routeGenerator.Object, Mock.Of<IRedirectsRepository>(), copier.Object);

            var updated = await repo.UpdateClub(club, Guid.NewGuid(), "Person 1").ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var totalVersions = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.ClubVersion} WHERE ClubId = @ClubId", auditable).ConfigureAwait(false);
                Assert.Equal(existingVersions, totalVersions);

                var versionResult = await connection.QuerySingleAsync<(Guid clubVersionId, string clubName, string comparableName, DateTimeOffset? fromDate)>(
                    $"SELECT ClubVersionId, ClubName, ComparableName, FromDate FROM {Tables.ClubVersion} WHERE ClubId = @ClubId AND UntilDate IS NULL", auditable).ConfigureAwait(false);

                Assert.Equal(currentVersion.clubVersionId, versionResult.clubVersionId);
                Assert.Equal(auditable.ClubName, versionResult.clubName);
                Assert.Equal(auditable.ComparableName(), versionResult.comparableName);
                Assert.Equal(currentVersion.fromDate.Value, versionResult.fromDate.Value);
            }
        }

        [Fact]
        public async Task Update_club_updates_name_and_route()
        {
            var club = _databaseFixture.TestData.Teams.Where(x => x.Club != null).First().Club;
            var auditable = new Club
            {
                ClubId = club.ClubId,
                ClubName = club.ClubName + " changed",
                ClubRoute = club.ClubRoute,
                Teams = club.Teams.Select(x => new Team { TeamId = x.TeamId }).ToList()
            };
            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(club.ClubRoute, "/clubs", auditable.ClubName, NoiseWords.ClubRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(club.ClubRoute + "-123"));
            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(club)).Returns(auditable);

            var repo = new SqlServerClubRepository(_databaseFixture.ConnectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger>(), routeGenerator.Object, Mock.Of<IRedirectsRepository>(), copier.Object);
            var memberKey = Guid.NewGuid();
            var memberName = "Person 1";

            var updatedClub = await repo.UpdateClub(club, memberKey, memberName).ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var clubResult = await connection.QuerySingleOrDefaultAsync<Club>($"SELECT ClubRoute FROM {Tables.Club} WHERE ClubId = @ClubId", new { updatedClub.ClubId }).ConfigureAwait(false);
                Assert.NotNull(clubResult);
                Assert.Equal(club.ClubRoute + "-123", clubResult.ClubRoute);

                var versionResult = await connection.QuerySingleOrDefaultAsync<ClubVersion>($"SELECT ClubName, ComparableName FROM {Tables.ClubVersion} WHERE ClubId = @ClubId", new { updatedClub.ClubId }).ConfigureAwait(false);
                Assert.NotNull(versionResult);
                Assert.Equal(auditable.ClubName, versionResult.ClubName);
                Assert.Equal(auditable.ComparableName(), versionResult.ComparableName);
            }
        }

        [Fact]
        public async Task Update_club_updates_teams_respecting_existing_clubs()
        {
            var club = _databaseFixture.TestData.Teams.Last(x => x.Club != null).Club;
            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(club.ClubRoute, "/clubs", club.ClubName, NoiseWords.ClubRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(club.ClubRoute));

            var teamRemoved = club.Teams.First();
            club.Teams.Remove(teamRemoved);
            var teamAdded = _databaseFixture.TestData.Teams.First(x => x.Club == null);
            club.Teams.Add(teamAdded);
            var teamTakenFromAnotherClub = _databaseFixture.TestData.Teams.First(x => x.Club != null && x.Club.ClubId != club.ClubId);
            club.Teams.Add(teamTakenFromAnotherClub);

            // Copy the club /after/ it's been modified
            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(club)).Returns(new Club { ClubId = club.ClubId, ClubName = club.ClubName, ClubRoute = club.ClubRoute, Teams = club.Teams.Select(x => new Team { TeamId = x.TeamId }).ToList() });

            var repo = new SqlServerClubRepository(_databaseFixture.ConnectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger>(), routeGenerator.Object, Mock.Of<IRedirectsRepository>(), copier.Object);
            var memberKey = Guid.NewGuid();
            var memberName = "Person 1";

            var updatedClub = await repo.UpdateClub(club, memberKey, memberName).ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var teamsResult = await connection.QueryAsync<Guid>($"SELECT TeamId FROM {Tables.Team} WHERE ClubId = @ClubId", new { updatedClub.ClubId }).ConfigureAwait(false);
                Assert.Equal(club.Teams.Count - 1, teamsResult.Count());
                Assert.DoesNotContain(teamRemoved.TeamId.Value, teamsResult);
                Assert.Contains(teamAdded.TeamId.Value, teamsResult);
                Assert.DoesNotContain(teamTakenFromAnotherClub.TeamId.Value, teamsResult);
            }
        }

        [Fact]
        public async Task Update_club_inserts_redirect()
        {
            var club = _databaseFixture.TestData.Teams.Where(x => x.Club != null).First().Club;
            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(club.ClubRoute, "/clubs", club.ClubName, NoiseWords.ClubRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(club.ClubRoute + "-123"));
            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(club)).Returns(new Club { ClubId = club.ClubId, ClubName = club.ClubName, ClubRoute = club.ClubRoute });
            var redirectsRepository = new Mock<IRedirectsRepository>();

            var repo = new SqlServerClubRepository(_databaseFixture.ConnectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger>(), routeGenerator.Object, redirectsRepository.Object, copier.Object);
            var memberKey = Guid.NewGuid();
            var memberName = "Person 1";

            var updatedClub = await repo.UpdateClub(club, memberKey, memberName).ConfigureAwait(false);

            redirectsRepository.Verify(x => x.InsertRedirect(club.ClubRoute, club.ClubRoute + "-123", null, It.IsAny<IDbTransaction>()), Times.Once);
        }


        [Fact]
        public async Task Update_club_does_not_redirect_unchanged_route()
        {
            var club = _databaseFixture.TestData.Teams.Where(x => x.Club != null).First().Club;
            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(club.ClubRoute, "/clubs", club.ClubName, NoiseWords.ClubRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(club.ClubRoute));
            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(club)).Returns(new Club { ClubId = club.ClubId, ClubName = club.ClubName, ClubRoute = club.ClubRoute });
            var redirectsRepository = new Mock<IRedirectsRepository>();

            var repo = new SqlServerClubRepository(_databaseFixture.ConnectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger>(), routeGenerator.Object, redirectsRepository.Object, copier.Object);
            var memberKey = Guid.NewGuid();
            var memberName = "Person 1";

            var updatedClub = await repo.UpdateClub(club, memberKey, memberName).ConfigureAwait(false);

            redirectsRepository.Verify(x => x.InsertRedirect(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDbTransaction>()), Times.Never);
        }

        [Fact]
        public async Task Update_club_audits_and_logs()
        {
            var club = _databaseFixture.TestData.Teams.Where(x => x.Club != null).First().Club;
            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(club.ClubRoute, "/clubs", club.ClubName, NoiseWords.ClubRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(club.ClubRoute));
            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(club)).Returns(new Club { ClubId = club.ClubId, ClubName = club.ClubName, ClubRoute = club.ClubRoute, Teams = club.Teams.Select(x => new Team { TeamId = x.TeamId }).ToList() });
            var auditRepository = new Mock<IAuditRepository>();
            var logger = new Mock<ILogger>();

            var repo = new SqlServerClubRepository(_databaseFixture.ConnectionFactory, auditRepository.Object, logger.Object, routeGenerator.Object, Mock.Of<IRedirectsRepository>(), copier.Object);
            var memberKey = Guid.NewGuid();
            var memberName = "Person 1";

            var updatedClub = await repo.UpdateClub(club, memberKey, memberName).ConfigureAwait(false);

            auditRepository.Verify(x => x.CreateAudit(It.IsAny<AuditRecord>(), It.IsAny<IDbTransaction>()), Times.Once);
            logger.Verify(x => x.Info(typeof(SqlServerClubRepository), LoggingTemplates.Updated, It.IsAny<Club>(), memberName, memberKey, typeof(SqlServerClubRepository), nameof(SqlServerClubRepository.UpdateClub)));
        }

        [Fact]
        public async Task Delete_club_succeeds()
        {
            var memberKey = Guid.NewGuid();
            var memberName = "Dee Leeter";
            var club = _databaseFixture.TestData.TeamWithFullDetails.Club;
            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(club)).Returns(new Club { ClubId = club.ClubId, ClubName = club.ClubName, ClubRoute = club.ClubRoute, Teams = club.Teams.Select(x => new Team { TeamId = x.TeamId }).ToList() });

            var repo = new SqlServerClubRepository(_databaseFixture.ConnectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger>(), Mock.Of<IRouteGenerator>(), Mock.Of<IRedirectsRepository>(), copier.Object);

            await repo.DeleteClub(club, memberKey, memberName).ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<Guid?>($"SELECT ClubId FROM {Tables.Club} WHERE ClubId = @ClubId", new { club.ClubId }).ConfigureAwait(false);
                Assert.Null(result);
            }
        }

        public void Dispose() => _scope.Dispose();
    }
}
