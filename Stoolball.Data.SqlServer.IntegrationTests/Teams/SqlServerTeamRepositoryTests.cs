using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Moq;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Html;
using Stoolball.Logging;
using Stoolball.MatchLocations;
using Stoolball.Routing;
using Stoolball.Security;
using Stoolball.Teams;
using Xunit;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer.IntegrationTests.Teams
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class SqlServerTeamRepositoryTests : IDisposable
    {
        private readonly SqlServerTestDataFixture _databaseFixture;
        private readonly TransactionScope _scope;

        public SqlServerTeamRepositoryTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
            _scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        }

        [Fact]
        public async Task Create_team_throws_ArgumentNullException_if_team_is_null()
        {
            var repo = new SqlServerTeamRepository(
           _databaseFixture.ConnectionFactory,
                       Mock.Of<IAuditRepository>(),
                       Mock.Of<ILogger<SqlServerTeamRepository>>(),
                       Mock.Of<IRouteGenerator>(),
                       Mock.Of<IRedirectsRepository>(),
                       Mock.Of<IMemberGroupHelper>(),
                       Mock.Of<IHtmlSanitizer>(),
                       Mock.Of<IStoolballEntityCopier>(),
                       Mock.Of<IUrlFormatter>(),
                       Mock.Of<ISocialMediaAccountFormatter>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.CreateTeam(null, Guid.NewGuid(), "Username", "Member name").ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Create_team_throws_ArgumentNullException_if_username_is_null()
        {
            var repo = new SqlServerTeamRepository(
                       _databaseFixture.ConnectionFactory,
                       Mock.Of<IAuditRepository>(),
                       Mock.Of<ILogger<SqlServerTeamRepository>>(),
                       Mock.Of<IRouteGenerator>(),
                       Mock.Of<IRedirectsRepository>(),
                       Mock.Of<IMemberGroupHelper>(),
                       Mock.Of<IHtmlSanitizer>(),
                       Mock.Of<IStoolballEntityCopier>(),
                       Mock.Of<IUrlFormatter>(),
                       Mock.Of<ISocialMediaAccountFormatter>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.CreateTeam(new Team(), Guid.NewGuid(), null, "Member name").ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Create_team_throws_ArgumentNullException_if_username_is_empty_string()
        {
            var repo = new SqlServerTeamRepository(
                       _databaseFixture.ConnectionFactory,
                       Mock.Of<IAuditRepository>(),
                       Mock.Of<ILogger<SqlServerTeamRepository>>(),
                       Mock.Of<IRouteGenerator>(),
                       Mock.Of<IRedirectsRepository>(),
                       Mock.Of<IMemberGroupHelper>(),
                       Mock.Of<IHtmlSanitizer>(),
                       Mock.Of<IStoolballEntityCopier>(),
                       Mock.Of<IUrlFormatter>(),
                       Mock.Of<ISocialMediaAccountFormatter>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.CreateTeam(new Team(), Guid.NewGuid(), string.Empty, "Member name").ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Create_team_with_transaction_throws_ArgumentNullException_if_team_is_null()
        {
            var repo = new SqlServerTeamRepository(
           _databaseFixture.ConnectionFactory,
                       Mock.Of<IAuditRepository>(),
                       Mock.Of<ILogger<SqlServerTeamRepository>>(),
                       Mock.Of<IRouteGenerator>(),
                       Mock.Of<IRedirectsRepository>(),
                       Mock.Of<IMemberGroupHelper>(),
                       Mock.Of<IHtmlSanitizer>(),
                       Mock.Of<IStoolballEntityCopier>(),
                       Mock.Of<IUrlFormatter>(),
                       Mock.Of<ISocialMediaAccountFormatter>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.CreateTeam(null, Mock.Of<IDbTransaction>(), "Username").ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Create_team_with_transaction_throws_ArgumentNullException_if_transaction_is_null()
        {
            var repo = new SqlServerTeamRepository(
           _databaseFixture.ConnectionFactory,
                       Mock.Of<IAuditRepository>(),
                       Mock.Of<ILogger<SqlServerTeamRepository>>(),
                       Mock.Of<IRouteGenerator>(),
                       Mock.Of<IRedirectsRepository>(),
                       Mock.Of<IMemberGroupHelper>(),
                       Mock.Of<IHtmlSanitizer>(),
                       Mock.Of<IStoolballEntityCopier>(),
                       Mock.Of<IUrlFormatter>(),
                       Mock.Of<ISocialMediaAccountFormatter>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.CreateTeam(new Team(), null, "Username").ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Create_team_with_transaction_throws_ArgumentNullException_if_username_is_null()
        {
            var repo = new SqlServerTeamRepository(
                       _databaseFixture.ConnectionFactory,
                       Mock.Of<IAuditRepository>(),
                       Mock.Of<ILogger<SqlServerTeamRepository>>(),
                       Mock.Of<IRouteGenerator>(),
                       Mock.Of<IRedirectsRepository>(),
                       Mock.Of<IMemberGroupHelper>(),
                       Mock.Of<IHtmlSanitizer>(),
                       Mock.Of<IStoolballEntityCopier>(),
                       Mock.Of<IUrlFormatter>(),
                       Mock.Of<ISocialMediaAccountFormatter>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.CreateTeam(new Team(), Mock.Of<IDbTransaction>(), null).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Create_team_with_transaction_throws_ArgumentNullException_if_username_is_empty_string()
        {
            var repo = new SqlServerTeamRepository(
                       _databaseFixture.ConnectionFactory,
                       Mock.Of<IAuditRepository>(),
                       Mock.Of<ILogger<SqlServerTeamRepository>>(),
                       Mock.Of<IRouteGenerator>(),
                       Mock.Of<IRedirectsRepository>(),
                       Mock.Of<IMemberGroupHelper>(),
                       Mock.Of<IHtmlSanitizer>(),
                       Mock.Of<IStoolballEntityCopier>(),
                       Mock.Of<IUrlFormatter>(),
                       Mock.Of<ISocialMediaAccountFormatter>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.CreateTeam(new Team(), Mock.Of<IDbTransaction>(), string.Empty).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Create_team_throws_ArgumentNullException_if_memberName_is_null()
        {
            var repo = new SqlServerTeamRepository(
                       _databaseFixture.ConnectionFactory,
                       Mock.Of<IAuditRepository>(),
                       Mock.Of<ILogger<SqlServerTeamRepository>>(),
                       Mock.Of<IRouteGenerator>(),
                       Mock.Of<IRedirectsRepository>(),
                       Mock.Of<IMemberGroupHelper>(),
                       Mock.Of<IHtmlSanitizer>(),
                       Mock.Of<IStoolballEntityCopier>(),
                       Mock.Of<IUrlFormatter>(),
                       Mock.Of<ISocialMediaAccountFormatter>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.CreateTeam(new Team(), Guid.NewGuid(), "Username", null).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Create_team_throws_ArgumentNullException_if_memberName_is_empty_string()
        {
            var repo = new SqlServerTeamRepository(
                       _databaseFixture.ConnectionFactory,
                       Mock.Of<IAuditRepository>(),
                       Mock.Of<ILogger<SqlServerTeamRepository>>(),
                       Mock.Of<IRouteGenerator>(),
                       Mock.Of<IRedirectsRepository>(),
                       Mock.Of<IMemberGroupHelper>(),
                       Mock.Of<IHtmlSanitizer>(),
                       Mock.Of<IStoolballEntityCopier>(),
                       Mock.Of<IUrlFormatter>(),
                       Mock.Of<ISocialMediaAccountFormatter>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.CreateTeam(new Team(), Guid.NewGuid(), "Username", string.Empty).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Create_minimal_team_succeeds()
        {
            var team = new Team
            {
                TeamName = "New team " + Guid.NewGuid(),
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = "Test group"
            };

            var route = "/teams/" + Guid.NewGuid();
            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute("/teams", team.TeamName, NoiseWords.TeamRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(route));

            var groupHelper = new Mock<IMemberGroupHelper>();
            groupHelper.Setup(x => x.CreateOrFindGroup("team", team.TeamName, NoiseWords.TeamRoute)).Returns(new SecurityGroup { Key = Guid.NewGuid(), Name = "Group name" });

            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(team)).Returns(team);
            copier.Setup(x => x.CreateRedactedCopy(team)).Returns(team);

            var repo = new SqlServerTeamRepository(
                _databaseFixture.ConnectionFactory,
                Mock.Of<IAuditRepository>(),
                Mock.Of<ILogger<SqlServerTeamRepository>>(),
                routeGenerator.Object,
                Mock.Of<IRedirectsRepository>(),
                groupHelper.Object,
                Mock.Of<IHtmlSanitizer>(),
                copier.Object,
                Mock.Of<IUrlFormatter>(),
                Mock.Of<ISocialMediaAccountFormatter>());

            var memberKey = Guid.NewGuid();
            var memberUserName = "example@example.org";
            var memberName = "Person 1";

            var created = await repo.CreateTeam(team, memberKey, memberUserName, memberName).ConfigureAwait(false);

            routeGenerator.Verify(x => x.GenerateUniqueRoute("/teams", team.TeamName, NoiseWords.TeamRoute, It.IsAny<Func<string, Task<int>>>()), Times.Once);
            groupHelper.Verify(x => x.CreateOrFindGroup("team", team.TeamName, NoiseWords.TeamRoute), Times.Once);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var teamResult = await connection.QuerySingleOrDefaultAsync<Team>(
                    @$"SELECT MemberGroupKey, MemberGroupName, TeamRoute, PlayerType, TeamType
                            FROM {Tables.Team} 
                            WHERE TeamId = @TeamId",
                    new
                    {
                        created.TeamId
                    }).ConfigureAwait(false);

                Assert.NotNull(teamResult);
                Assert.Equal(team.MemberGroupKey, teamResult.MemberGroupKey);
                Assert.Equal(team.MemberGroupName, teamResult.MemberGroupName);
                Assert.Equal(team.TeamRoute, teamResult.TeamRoute);
                Assert.Equal(team.PlayerType, teamResult.PlayerType);
                Assert.Equal(team.TeamType, teamResult.TeamType);

                var versionResult = await connection.QuerySingleAsync<(string TeamName, string comparableName, DateTimeOffset? fromDate, DateTimeOffset? untilDate)>(
                    $"SELECT TeamName, ComparableName, FromDate, UntilDate FROM {Tables.TeamVersion} WHERE TeamId = @TeamId",
                    new { created.TeamId }
                ).ConfigureAwait(false);

                Assert.Equal(team.TeamName, versionResult.TeamName);
                Assert.Equal(team.ComparableName(), versionResult.comparableName);
                Assert.Equal(DateTime.UtcNow.Date, versionResult.fromDate.Value.Date);
                Assert.Null(versionResult.untilDate);
            }
        }

        [Fact]
        public async Task Create_complete_team_succeeds()
        {
            var originalIntro = "<p>This is the intro</p>";
            var sanitisedIntro = "<p>This is the sanitised intro</p>";
            var originalPlayingTimes = "<p>Original playing times</p>";
            var sanitisedPlayingTimes = "<p>Sanitised playing times</p>";
            var originalCost = "<p>Original cost</p>";
            var sanitisedCost = "<p>Sanitised cost</p>";
            var originalPrivateContact = "<p>Private contact details</p>";
            var sanitisedPrivateContact = "<p>Sanitised private details</p>";
            var originalPublicContact = "<p>Public contact details</p>";
            var sanitisedPublicContact = "<p>Sanitised public details</p>";

            List<Guid> matchLocations;
            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                matchLocations = (await connection.QueryAsync<Guid>($"SELECT TOP 2 MatchLocationId FROM {Tables.MatchLocation}").ConfigureAwait(false)).ToList();
            }

            var team = new Team
            {
                Introduction = originalIntro,
                PlayingTimes = originalPlayingTimes,
                Cost = originalCost,
                TeamType = TeamType.LimitedMembership,
                PlayerType = PlayerType.JuniorGirls,
                TeamName = "Example Team",
                UntilYear = 2021,
                AgeRangeLower = 21,
                AgeRangeUpper = 70,
                Facebook = "facebook.com/example",
                Instagram = "exampleinsta",
                YouTube = "youtube.com/example",
                Twitter = "exampletweeter",
                Website = "example.org/",
                ClubMark = true,
                PrivateContactDetails = originalPrivateContact,
                PublicContactDetails = originalPublicContact,
                MatchLocations = matchLocations.Select(x => new MatchLocation { MatchLocationId = x }).ToList()
            };

            var auditable = new Team
            {
                Introduction = team.Introduction,
                PlayingTimes = team.PlayingTimes,
                Cost = team.Cost,
                TeamType = team.TeamType,
                PlayerType = team.PlayerType,
                TeamName = team.TeamName,
                UntilYear = team.UntilYear,
                AgeRangeLower = team.AgeRangeLower,
                AgeRangeUpper = team.AgeRangeUpper,
                Facebook = team.Facebook,
                Instagram = team.Instagram,
                YouTube = team.YouTube,
                Twitter = team.Twitter,
                Website = team.Website,
                ClubMark = team.ClubMark,
                PrivateContactDetails = team.PrivateContactDetails,
                PublicContactDetails = team.PublicContactDetails,
                MatchLocations = team.MatchLocations
            };

            var route = "/teams/" + Guid.NewGuid();
            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute("/teams", auditable.TeamName, NoiseWords.TeamRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(route));

            var group = new SecurityGroup { Key = Guid.NewGuid(), Name = "Group name" };
            var groupHelper = new Mock<IMemberGroupHelper>();
            groupHelper.Setup(x => x.CreateOrFindGroup("team", team.TeamName, NoiseWords.TeamRoute)).Returns(group);

            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(team)).Returns(auditable);

            var sanitizer = new Mock<IHtmlSanitizer>();
            sanitizer.Setup(x => x.Sanitize(auditable.PrivateContactDetails)).Returns(sanitisedPrivateContact);
            sanitizer.Setup(x => x.Sanitize(auditable.PublicContactDetails)).Returns(sanitisedPublicContact);
            sanitizer.Setup(x => x.Sanitize(auditable.Introduction)).Returns(sanitisedIntro);
            sanitizer.Setup(x => x.Sanitize(auditable.PlayingTimes)).Returns(sanitisedPlayingTimes);
            sanitizer.Setup(x => x.Sanitize(auditable.Cost)).Returns(sanitisedCost);

            var urlFormatter = new Mock<IUrlFormatter>();
            urlFormatter.Setup(x => x.PrefixHttpsProtocol(auditable.Facebook)).Returns(new Uri("https://" + auditable.Facebook));
            urlFormatter.Setup(x => x.PrefixHttpsProtocol(auditable.YouTube)).Returns(new Uri("https://" + auditable.YouTube));
            urlFormatter.Setup(x => x.PrefixHttpsProtocol(auditable.Website)).Returns(new Uri("https://" + auditable.Website));

            var socialMediaFormatter = new Mock<ISocialMediaAccountFormatter>();
            socialMediaFormatter.Setup(x => x.PrefixAtSign(auditable.Instagram)).Returns("@" + auditable.Instagram);
            socialMediaFormatter.Setup(x => x.PrefixAtSign(auditable.Twitter)).Returns("@" + auditable.Twitter);

            var userName = "Example username";

            var repo = new SqlServerTeamRepository(
                       _databaseFixture.ConnectionFactory,
                       Mock.Of<IAuditRepository>(),
                       Mock.Of<ILogger<SqlServerTeamRepository>>(),
                       routeGenerator.Object,
                       Mock.Of<IRedirectsRepository>(),
                       groupHelper.Object,
                       sanitizer.Object,
                       copier.Object,
                       urlFormatter.Object,
                       socialMediaFormatter.Object);

            var created = await repo.CreateTeam(team, Guid.NewGuid(), userName, "Member name").ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var teamResult = await connection.QuerySingleOrDefaultAsync<Team>(
                    $@"SELECT TeamType, PlayerType, Introduction, AgeRangeLower, AgeRangeUpper, PlayingTimes, Cost,
                                Facebook, Instagram, YouTube, Twitter, Website, ClubMark,
                                PrivateContactDetails, PublicContactDetails, MemberGroupKey, MemberGroupName, TeamRoute
                                FROM {Tables.Team} 
                                WHERE TeamId = @TeamId",
                    new { created.TeamId }).ConfigureAwait(false);
                Assert.NotNull(teamResult);

                sanitizer.Verify(x => x.Sanitize(originalIntro));
                Assert.Equal(sanitisedIntro, teamResult.Introduction);
                sanitizer.Verify(x => x.Sanitize(originalPlayingTimes));
                Assert.Equal(sanitisedPlayingTimes, teamResult.PlayingTimes);
                sanitizer.Verify(x => x.Sanitize(originalCost));
                Assert.Equal(sanitisedCost, teamResult.Cost);
                Assert.Equal(team.TeamType, teamResult.TeamType);
                Assert.Equal(team.PlayerType, teamResult.PlayerType);
                Assert.Equal(team.AgeRangeLower, teamResult.AgeRangeLower);
                Assert.Equal(team.AgeRangeUpper, teamResult.AgeRangeUpper);
                urlFormatter.Verify(x => x.PrefixHttpsProtocol(team.Facebook), Times.Once);
                Assert.Equal("https://" + team.Facebook, teamResult.Facebook);
                socialMediaFormatter.Verify(x => x.PrefixAtSign(team.Instagram), Times.Once);
                Assert.Equal("@" + team.Instagram, teamResult.Instagram);
                urlFormatter.Verify(x => x.PrefixHttpsProtocol(team.YouTube), Times.Once);
                Assert.Equal("https://" + team.YouTube, teamResult.YouTube);
                socialMediaFormatter.Verify(x => x.PrefixAtSign(team.Twitter), Times.Once);
                Assert.Equal("@" + team.Twitter, teamResult.Twitter);
                urlFormatter.Verify(x => x.PrefixHttpsProtocol(team.Website), Times.Once);
                Assert.Equal("https://" + team.Website, teamResult.Website);
                Assert.Equal(team.ClubMark, teamResult.ClubMark);
                sanitizer.Verify(x => x.Sanitize(originalPrivateContact));
                Assert.Equal(sanitisedPrivateContact, teamResult.PrivateContactDetails);
                sanitizer.Verify(x => x.Sanitize(originalPublicContact));
                Assert.Equal(sanitisedPublicContact, teamResult.PublicContactDetails);

                groupHelper.Verify(x => x.MemberIsAdministrator(userName), Times.Once);
                groupHelper.Verify(x => x.AssignRole(userName, group.Name), Times.Once);
                Assert.Equal(group.Key, teamResult.MemberGroupKey);
                Assert.Equal(group.Name, teamResult.MemberGroupName);

                Assert.Equal(route, teamResult.TeamRoute);

                var versionResult = await connection.QuerySingleAsync<(string TeamName, string comparableName, DateTimeOffset? fromDate, DateTimeOffset? untilDate)>(
                    $"SELECT TeamName, ComparableName, FromDate, UntilDate FROM {Tables.TeamVersion} WHERE TeamId = @TeamId",
                    new { created.TeamId }
                ).ConfigureAwait(false);

                Assert.Equal(team.TeamName, versionResult.TeamName);
                Assert.Equal(team.ComparableName(), versionResult.comparableName);
                Assert.Equal(DateTime.UtcNow.Date, versionResult.fromDate.Value.Date);
                Assert.Equal(new DateTimeOffset(team.UntilYear.Value, 12, 31, 0, 0, 0, TimeSpan.Zero), versionResult.untilDate);

                var savedMatchLocations = (await connection.QueryAsync<(Guid matchLocationId, DateTimeOffset fromDate)>(
                    $"SELECT MatchLocationId, FromDate FROM {Tables.TeamMatchLocation} WHERE TeamId = @TeamId",
                    new { created.TeamId }
                    ).ConfigureAwait(false)).ToList();

                Assert.Equal(team.MatchLocations.Count, savedMatchLocations.Count);
                foreach (var location in team.MatchLocations)
                {
                    var resultLocation = savedMatchLocations.SingleOrDefault(x => x.matchLocationId == location.MatchLocationId);
                    Assert.NotEqual(default((Guid, DateTimeOffset)), resultLocation);
                    Assert.Equal(DateTime.UtcNow.Date, resultLocation.fromDate.Date);
                }
            }
        }

        [Fact]
        public async Task Create_team_returns_a_copy()
        {
            var team = new Team
            {
                TeamName = "Example Team",
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = "Test group"
            };

            var copyTeam = new Team
            {
                TeamName = "Example Team copy",
                MemberGroupKey = team.MemberGroupKey,
                MemberGroupName = team.MemberGroupName
            };

            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute("/teams", copyTeam.TeamName, NoiseWords.TeamRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult("/teams/" + Guid.NewGuid()));

            var groupHelper = new Mock<IMemberGroupHelper>();
            groupHelper.Setup(x => x.CreateOrFindGroup("team", copyTeam.TeamName, NoiseWords.TeamRoute)).Returns(new SecurityGroup { Key = Guid.NewGuid(), Name = "Group name" });

            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(team)).Returns(copyTeam);

            var repo = new SqlServerTeamRepository(
                     _databaseFixture.ConnectionFactory,
                     Mock.Of<IAuditRepository>(),
                     Mock.Of<ILogger<SqlServerTeamRepository>>(),
                     routeGenerator.Object,
                     Mock.Of<IRedirectsRepository>(),
                     groupHelper.Object,
                     Mock.Of<IHtmlSanitizer>(),
                     copier.Object,
                     Mock.Of<IUrlFormatter>(),
                     Mock.Of<ISocialMediaAccountFormatter>());

            var created = await repo.CreateTeam(team, Guid.NewGuid(), "Username", "Member name").ConfigureAwait(false);
            copier.Verify(x => x.CreateAuditableCopy(team), Times.Once);
            Assert.Equal(copyTeam, created);
        }


        [Fact]
        public async Task Create_team_with_transaction_returns_a_copy()
        {
            var team = new Team
            {
                TeamName = "Example Team",
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = "Test group"
            };

            var copyTeam = new Team
            {
                TeamName = "Example Team copy",
                MemberGroupKey = team.MemberGroupKey,
                MemberGroupName = team.MemberGroupName
            };

            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute("/teams", copyTeam.TeamName, NoiseWords.TeamRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult("/teams/" + Guid.NewGuid()));

            var groupHelper = new Mock<IMemberGroupHelper>();
            groupHelper.Setup(x => x.CreateOrFindGroup("team", copyTeam.TeamName, NoiseWords.TeamRoute)).Returns(new SecurityGroup { Key = Guid.NewGuid(), Name = "Group name" });

            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(team)).Returns(copyTeam);

            var repo = new SqlServerTeamRepository(
                     _databaseFixture.ConnectionFactory,
                     Mock.Of<IAuditRepository>(),
                     Mock.Of<ILogger<SqlServerTeamRepository>>(),
                     routeGenerator.Object,
                     Mock.Of<IRedirectsRepository>(),
                     groupHelper.Object,
                     Mock.Of<IHtmlSanitizer>(),
                     copier.Object,
                     Mock.Of<IUrlFormatter>(),
                     Mock.Of<ISocialMediaAccountFormatter>());

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var created = await repo.CreateTeam(team, transaction, "Username").ConfigureAwait(false);

                    copier.Verify(x => x.CreateAuditableCopy(team), Times.Once);
                    Assert.Equal(copyTeam, created);

                    transaction.Rollback();
                }
            }
        }

        [Fact]
        public async Task Create_team_audits_and_logs()
        {
            var team = new Team
            {
                TeamName = "New Team " + Guid.NewGuid(),
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = "Test group"
            };

            var auditable = new Team
            {
                TeamName = "New Team " + Guid.NewGuid(),
                MemberGroupKey = team.MemberGroupKey,
                MemberGroupName = team.MemberGroupName
            };

            var redacted = new Team
            {
                TeamName = "New Team " + Guid.NewGuid(),
                MemberGroupKey = team.MemberGroupKey,
                MemberGroupName = team.MemberGroupName
            };

            var route = "/teams/" + Guid.NewGuid();
            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute("/teams", auditable.TeamName, NoiseWords.TeamRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(route));

            var groupHelper = new Mock<IMemberGroupHelper>();
            groupHelper.Setup(x => x.CreateOrFindGroup("team", auditable.TeamName, NoiseWords.TeamRoute)).Returns(new SecurityGroup { Key = Guid.NewGuid(), Name = "Group name" });

            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(team)).Returns(auditable);
            copier.Setup(x => x.CreateRedactedCopy(auditable)).Returns(redacted);

            var auditRepository = new Mock<IAuditRepository>();
            var logger = new Mock<ILogger<SqlServerTeamRepository>>();

            var repo = new SqlServerTeamRepository(
                 _databaseFixture.ConnectionFactory,
                 auditRepository.Object,
                 logger.Object,
                 routeGenerator.Object,
                 Mock.Of<IRedirectsRepository>(),
                 groupHelper.Object,
                 Mock.Of<IHtmlSanitizer>(),
                 copier.Object,
                 Mock.Of<IUrlFormatter>(),
                 Mock.Of<ISocialMediaAccountFormatter>());
            var memberKey = Guid.NewGuid();
            var memberName = "Person 1";

            var created = await repo.CreateTeam(team, memberKey, "Username", memberName).ConfigureAwait(false);

            copier.Verify(x => x.CreateRedactedCopy(auditable), Times.Once);
            auditRepository.Verify(x => x.CreateAudit(It.IsAny<AuditRecord>(), It.IsAny<IDbTransaction>()), Times.Once);
            logger.Verify(x => x.Info(LoggingTemplates.Created, redacted, memberName, memberKey, typeof(SqlServerTeamRepository), nameof(SqlServerTeamRepository.CreateTeam)), Times.Once);
        }

        [Fact]
        public async Task Create_team_with_transaction_does_not_audit_or_log()
        {
            var team = new Team
            {
                TeamName = "New Team " + Guid.NewGuid(),
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = "Test group"
            };

            var route = "/teams/" + Guid.NewGuid();
            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute("/teams", team.TeamName, NoiseWords.TeamRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(route));

            var groupHelper = new Mock<IMemberGroupHelper>();
            groupHelper.Setup(x => x.CreateOrFindGroup("team", team.TeamName, NoiseWords.TeamRoute)).Returns(new SecurityGroup { Key = Guid.NewGuid(), Name = "Group name" });

            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(team)).Returns(team);

            var auditRepository = new Mock<IAuditRepository>();
            var logger = new Mock<ILogger<SqlServerTeamRepository>>();

            var repo = new SqlServerTeamRepository(
                 _databaseFixture.ConnectionFactory,
                 auditRepository.Object,
                 logger.Object,
                 routeGenerator.Object,
                 Mock.Of<IRedirectsRepository>(),
                 groupHelper.Object,
                 Mock.Of<IHtmlSanitizer>(),
                 copier.Object,
                 Mock.Of<IUrlFormatter>(),
                 Mock.Of<ISocialMediaAccountFormatter>());

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var created = await repo.CreateTeam(team, transaction, "Username").ConfigureAwait(false);

                    copier.Verify(x => x.CreateRedactedCopy(It.IsAny<Team>()), Times.Never);
                    auditRepository.Verify(x => x.CreateAudit(It.IsAny<AuditRecord>(), It.IsAny<IDbTransaction>()), Times.Never);
                    logger.Verify(x => x.Info(LoggingTemplates.Created, It.IsAny<Team>(), It.IsAny<string>(), It.IsAny<Guid>(), typeof(SqlServerTeamRepository), nameof(SqlServerTeamRepository.CreateTeam)), Times.Never);
                }
            }
        }

        [Fact]
        public async Task Update_team_throws_ArgumentNullException_if_team_is_null()
        {
            var repo = new SqlServerTeamRepository(
           _databaseFixture.ConnectionFactory,
                       Mock.Of<IAuditRepository>(),
                       Mock.Of<ILogger<SqlServerTeamRepository>>(),
                       Mock.Of<IRouteGenerator>(),
                       Mock.Of<IRedirectsRepository>(),
                       Mock.Of<IMemberGroupHelper>(),
                       Mock.Of<IHtmlSanitizer>(),
                       Mock.Of<IStoolballEntityCopier>(),
                       Mock.Of<IUrlFormatter>(),
                       Mock.Of<ISocialMediaAccountFormatter>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.UpdateTeam(null, Guid.NewGuid(), "Member name").ConfigureAwait(false)).ConfigureAwait(false);
        }


        [Fact]
        public async Task Update_transient_team_throws_ArgumentNullException_if_team_is_null()
        {
            var repo = new SqlServerTeamRepository(
           _databaseFixture.ConnectionFactory,
                       Mock.Of<IAuditRepository>(),
                       Mock.Of<ILogger<SqlServerTeamRepository>>(),
                       Mock.Of<IRouteGenerator>(),
                       Mock.Of<IRedirectsRepository>(),
                       Mock.Of<IMemberGroupHelper>(),
                       Mock.Of<IHtmlSanitizer>(),
                       Mock.Of<IStoolballEntityCopier>(),
                       Mock.Of<IUrlFormatter>(),
                       Mock.Of<ISocialMediaAccountFormatter>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.UpdateTransientTeam(null, Guid.NewGuid(), "Member name").ConfigureAwait(false)).ConfigureAwait(false);
        }


        [Fact]
        public async Task Update_team_throws_ArgumentNullException_if_memberName_is_null()
        {
            var repo = new SqlServerTeamRepository(
                       _databaseFixture.ConnectionFactory,
                       Mock.Of<IAuditRepository>(),
                       Mock.Of<ILogger<SqlServerTeamRepository>>(),
                       Mock.Of<IRouteGenerator>(),
                       Mock.Of<IRedirectsRepository>(),
                       Mock.Of<IMemberGroupHelper>(),
                       Mock.Of<IHtmlSanitizer>(),
                       Mock.Of<IStoolballEntityCopier>(),
                       Mock.Of<IUrlFormatter>(),
                       Mock.Of<ISocialMediaAccountFormatter>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.UpdateTeam(new Team(), Guid.NewGuid(), null).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Update_team_throws_ArgumentNullException_if_memberName_is_empty_string()
        {
            var repo = new SqlServerTeamRepository(
                       _databaseFixture.ConnectionFactory,
                       Mock.Of<IAuditRepository>(),
                       Mock.Of<ILogger<SqlServerTeamRepository>>(),
                       Mock.Of<IRouteGenerator>(),
                       Mock.Of<IRedirectsRepository>(),
                       Mock.Of<IMemberGroupHelper>(),
                       Mock.Of<IHtmlSanitizer>(),
                       Mock.Of<IStoolballEntityCopier>(),
                       Mock.Of<IUrlFormatter>(),
                       Mock.Of<ISocialMediaAccountFormatter>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.UpdateTeam(new Team(), Guid.NewGuid(), string.Empty).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Update_transient_team_throws_ArgumentNullException_if_memberName_is_null()
        {
            var repo = new SqlServerTeamRepository(
                       _databaseFixture.ConnectionFactory,
                       Mock.Of<IAuditRepository>(),
                       Mock.Of<ILogger<SqlServerTeamRepository>>(),
                       Mock.Of<IRouteGenerator>(),
                       Mock.Of<IRedirectsRepository>(),
                       Mock.Of<IMemberGroupHelper>(),
                       Mock.Of<IHtmlSanitizer>(),
                       Mock.Of<IStoolballEntityCopier>(),
                       Mock.Of<IUrlFormatter>(),
                       Mock.Of<ISocialMediaAccountFormatter>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.UpdateTeam(new Team(), Guid.NewGuid(), null).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Update_transient_team_throws_ArgumentNullException_if_memberName_is_empty_string()
        {
            var repo = new SqlServerTeamRepository(
                       _databaseFixture.ConnectionFactory,
                       Mock.Of<IAuditRepository>(),
                       Mock.Of<ILogger<SqlServerTeamRepository>>(),
                       Mock.Of<IRouteGenerator>(),
                       Mock.Of<IRedirectsRepository>(),
                       Mock.Of<IMemberGroupHelper>(),
                       Mock.Of<IHtmlSanitizer>(),
                       Mock.Of<IStoolballEntityCopier>(),
                       Mock.Of<IUrlFormatter>(),
                       Mock.Of<ISocialMediaAccountFormatter>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.UpdateTransientTeam(new Team(), Guid.NewGuid(), string.Empty).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Update_team_succeeds()
        {
            var unsanitisedIntro = Guid.NewGuid().ToString();
            var sanitisedIntro = $"<p>This is the sanitised intro {unsanitisedIntro}</p>";
            var unsanitisedPlayingTimes = Guid.NewGuid().ToString();
            var sanitisedPlayingTimes = $"<p>Sanitised playing times {unsanitisedPlayingTimes}</p>";
            var unsanitisedCost = Guid.NewGuid().ToString();
            var sanitisedCost = $"<p>Sanitised cost {unsanitisedCost}</p>";
            var unsanitisedPrivateContact = Guid.NewGuid().ToString();
            var sanitisedPrivateContact = $"<p>Sanitised private details {unsanitisedPrivateContact}</p>";
            var unsanitisedPublicContact = Guid.NewGuid().ToString();
            var sanitisedPublicContact = $"<p>Sanitised public details {unsanitisedPublicContact}</p>";
            var updatedFacebook = $"www.facebook.com/{Guid.NewGuid()}";
            var updatedInstagram = Guid.NewGuid().ToString().Substring(0, 15);
            var updatedTwitter = Guid.NewGuid().ToString().Substring(0, 15);
            var updatedYouTube = $"www.youtube.com/{Guid.NewGuid()}";
            var updatedWebsite = $"www.example.org/{Guid.NewGuid()}";

            var team = _databaseFixture.TestData.TeamWithFullDetails;

            var auditable = new Team
            {
                TeamId = team.TeamId,
                TeamRoute = team.TeamRoute,
                Introduction = unsanitisedIntro,
                PlayingTimes = unsanitisedPlayingTimes,
                Cost = unsanitisedCost,
                TeamType = team.TeamType == TeamType.Occasional ? TeamType.LimitedMembership : TeamType.Occasional,
                PlayerType = team.PlayerType == PlayerType.Ladies ? PlayerType.Mixed : PlayerType.Ladies,
                TeamName = Guid.NewGuid().ToString(),
                UntilYear = team.UntilYear.HasValue ? team.UntilYear.Value + 1 : 2021,
                AgeRangeLower = team.AgeRangeLower == 11 ? 18 : 11,
                AgeRangeUpper = team.AgeRangeUpper == 70 ? 75 : 70,
                Facebook = updatedFacebook,
                Instagram = updatedInstagram,
                YouTube = updatedYouTube,
                Twitter = updatedTwitter,
                Website = updatedWebsite,
                ClubMark = !team.ClubMark,
                PrivateContactDetails = unsanitisedPrivateContact,
                PublicContactDetails = unsanitisedPublicContact,
            };

            var route = "/teams/" + Guid.NewGuid();
            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(team.TeamRoute, "/teams", auditable.TeamName, NoiseWords.TeamRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(route));

            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(team)).Returns(auditable);

            var sanitizer = new Mock<IHtmlSanitizer>();
            sanitizer.Setup(x => x.Sanitize(auditable.PrivateContactDetails)).Returns(sanitisedPrivateContact);
            sanitizer.Setup(x => x.Sanitize(auditable.PublicContactDetails)).Returns(sanitisedPublicContact);
            sanitizer.Setup(x => x.Sanitize(auditable.Introduction)).Returns(sanitisedIntro);
            sanitizer.Setup(x => x.Sanitize(auditable.PlayingTimes)).Returns(sanitisedPlayingTimes);
            sanitizer.Setup(x => x.Sanitize(auditable.Cost)).Returns(sanitisedCost);

            var urlFormatter = new Mock<IUrlFormatter>();
            urlFormatter.Setup(x => x.PrefixHttpsProtocol(auditable.Facebook)).Returns(new Uri("https://" + auditable.Facebook));
            urlFormatter.Setup(x => x.PrefixHttpsProtocol(auditable.YouTube)).Returns(new Uri("https://" + auditable.YouTube));
            urlFormatter.Setup(x => x.PrefixHttpsProtocol(auditable.Website)).Returns(new Uri("https://" + auditable.Website));

            var socialMediaFormatter = new Mock<ISocialMediaAccountFormatter>();
            socialMediaFormatter.Setup(x => x.PrefixAtSign(auditable.Instagram)).Returns("@" + auditable.Instagram);
            socialMediaFormatter.Setup(x => x.PrefixAtSign(auditable.Twitter)).Returns("@" + auditable.Twitter);

            var repo = new SqlServerTeamRepository(
                       _databaseFixture.ConnectionFactory,
                       Mock.Of<IAuditRepository>(),
                       Mock.Of<ILogger<SqlServerTeamRepository>>(),
                       routeGenerator.Object,
                       Mock.Of<IRedirectsRepository>(),
                       Mock.Of<IMemberGroupHelper>(),
                       sanitizer.Object,
                       copier.Object,
                       urlFormatter.Object,
                       socialMediaFormatter.Object);

            var updated = await repo.UpdateTeam(team, Guid.NewGuid(), "Member name").ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var teamResult = await connection.QuerySingleOrDefaultAsync<Team>(
                    $@"SELECT TeamType, PlayerType, Introduction, AgeRangeLower, AgeRangeUpper, PlayingTimes, Cost,
                                Facebook, Instagram, YouTube, Twitter, Website, ClubMark,
                                PrivateContactDetails, PublicContactDetails, TeamRoute
                                FROM {Tables.Team} 
                                WHERE TeamId = @TeamId",
                    new { team.TeamId }).ConfigureAwait(false);
                Assert.NotNull(teamResult);

                sanitizer.Verify(x => x.Sanitize(unsanitisedIntro));
                Assert.Equal(sanitisedIntro, teamResult.Introduction);
                sanitizer.Verify(x => x.Sanitize(unsanitisedPlayingTimes));
                Assert.Equal(sanitisedPlayingTimes, teamResult.PlayingTimes);
                sanitizer.Verify(x => x.Sanitize(unsanitisedCost));
                Assert.Equal(sanitisedCost, teamResult.Cost);
                Assert.Equal(auditable.TeamType, teamResult.TeamType);
                Assert.Equal(auditable.PlayerType, teamResult.PlayerType);
                Assert.Equal(auditable.AgeRangeLower, teamResult.AgeRangeLower);
                Assert.Equal(auditable.AgeRangeUpper, teamResult.AgeRangeUpper);
                urlFormatter.Verify(x => x.PrefixHttpsProtocol(updatedFacebook), Times.Once);
                Assert.Equal("https://" + updatedFacebook, teamResult.Facebook);
                socialMediaFormatter.Verify(x => x.PrefixAtSign(updatedInstagram), Times.Once);
                Assert.Equal("@" + updatedInstagram, teamResult.Instagram);
                urlFormatter.Verify(x => x.PrefixHttpsProtocol(updatedYouTube), Times.Once);
                Assert.Equal("https://" + updatedYouTube, teamResult.YouTube);
                socialMediaFormatter.Verify(x => x.PrefixAtSign(updatedTwitter), Times.Once);
                Assert.Equal("@" + updatedTwitter, teamResult.Twitter);
                urlFormatter.Verify(x => x.PrefixHttpsProtocol(updatedWebsite), Times.Once);
                Assert.Equal("https://" + updatedWebsite, teamResult.Website);
                sanitizer.Verify(x => x.Sanitize(unsanitisedPrivateContact));
                Assert.Equal(sanitisedPrivateContact, teamResult.PrivateContactDetails);
                sanitizer.Verify(x => x.Sanitize(unsanitisedPublicContact));
                Assert.Equal(sanitisedPublicContact, teamResult.PublicContactDetails);
                Assert.Equal(route, teamResult.TeamRoute);

                // Test that these fields are NOT updated
                Assert.Equal(team.ClubMark, teamResult.ClubMark);

                var versionResult = await connection.QuerySingleAsync<(string TeamName, string comparableName)>(
                     $"SELECT TOP 1 TeamName, ComparableName FROM {Tables.TeamVersion} WHERE TeamId = @TeamId ORDER BY FromDate DESC",
                       new { updated.TeamId }
                     ).ConfigureAwait(false);

                Assert.Equal(auditable.TeamName, versionResult.TeamName);
                Assert.Equal(auditable.ComparableName(), versionResult.comparableName);
            }
        }

        [Fact(Skip = "Team versioning is not implemented yet. See https://github.com/stoolball-england/stoolball-org-uk/issues/474")]
        public async Task Update_team_adds_team_version_if_name_changes()
        {
            var team = _databaseFixture.TestData.Teams.First(x => x.TeamType != TeamType.Transient && !x.UntilYear.HasValue);
            var auditable = new Team
            {
                TeamId = team.TeamId,
                TeamName = team.TeamName + Guid.NewGuid().ToString(),
                TeamRoute = team.TeamRoute
            };

            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(team.TeamRoute, "/teams", team.TeamName, NoiseWords.TeamRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(team.TeamRoute));
            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(team)).Returns(auditable);

            var repo = new SqlServerTeamRepository(
                     _databaseFixture.ConnectionFactory,
                     Mock.Of<IAuditRepository>(),
                     Mock.Of<ILogger<SqlServerTeamRepository>>(),
                     routeGenerator.Object,
                     Mock.Of<IRedirectsRepository>(),
                     Mock.Of<IMemberGroupHelper>(),
                     Mock.Of<IHtmlSanitizer>(),
                     copier.Object,
                     Mock.Of<IUrlFormatter>(),
                     Mock.Of<ISocialMediaAccountFormatter>());

            await UpdateTeamAddsTeamVersionIfNameChanges(team, auditable, (team, memberKey, memberName) => repo.UpdateTeam(team, memberKey, memberName)).ConfigureAwait(false);
        }

        private async Task UpdateTeamAddsTeamVersionIfNameChanges(Team team, Team auditable, Func<Team, Guid, string, Task<Team>> updateMethod)
        {
            int? existingVersions = null;
            Guid? currentVersionId = null;
            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                existingVersions = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.TeamVersion} WHERE TeamId = @TeamId", auditable).ConfigureAwait(false);
                currentVersionId = await connection.ExecuteScalarAsync<Guid>($"SELECT TeamVersionId FROM {Tables.TeamVersion} WHERE TeamId = @TeamId AND UntilDate IS NULL", auditable).ConfigureAwait(false);
            }

            _ = await updateMethod(team, Guid.NewGuid(), "Person 1").ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var totalVersions = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.TeamVersion} WHERE TeamId = @TeamId", auditable).ConfigureAwait(false);
                Assert.Equal(existingVersions + 1, totalVersions);

                var versionResult = await connection.QuerySingleAsync<(Guid teamVersionId, string teamName, string comparableName, DateTimeOffset? fromDate)>(
                    $"SELECT TeamVersionId, TeamName, ComparableName, FromDate FROM {Tables.TeamVersion} WHERE TeamId = @TeamId AND UntilDate IS NULL", auditable).ConfigureAwait(false);

                Assert.NotEqual(currentVersionId, versionResult.teamVersionId);
                Assert.Equal(auditable.TeamName, versionResult.teamName);
                Assert.Equal(auditable.ComparableName(), versionResult.comparableName);
                Assert.Equal(DateTime.UtcNow.Date, versionResult.fromDate.Value);
            }
        }

        [Fact]
        public async Task Update_team_does_not_add_team_version_if_name_is_unchanged()
        {
            var team = _databaseFixture.TestData.Teams.First(x => x.TeamType != TeamType.Transient && !x.UntilYear.HasValue);
            var auditable = new Team
            {
                TeamId = team.TeamId,
                TeamName = team.TeamName,
                TeamRoute = team.TeamRoute
            };

            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(team.TeamRoute, "/teams", team.TeamName, NoiseWords.TeamRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(team.TeamRoute));
            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(team)).Returns(auditable);

            var repo = new SqlServerTeamRepository(
               _databaseFixture.ConnectionFactory,
               Mock.Of<IAuditRepository>(),
               Mock.Of<ILogger<SqlServerTeamRepository>>(),
               routeGenerator.Object,
               Mock.Of<IRedirectsRepository>(),
               Mock.Of<IMemberGroupHelper>(),
               Mock.Of<IHtmlSanitizer>(),
               copier.Object,
               Mock.Of<IUrlFormatter>(),
               Mock.Of<ISocialMediaAccountFormatter>());

            await UpdateTeamDoesNotAddTeamVersionIfNameIsUnchanged(team, auditable, (team, memberKey, memberName) => repo.UpdateTeam(team, memberKey, memberName)).ConfigureAwait(false);
        }

        private async Task UpdateTeamDoesNotAddTeamVersionIfNameIsUnchanged(Team team, Team auditable, Func<Team, Guid, string, Task<Team>> updateMethod)
        {
            int? existingVersions = null;
            (Guid? teamVersionId, string teamName, string comparableName, DateTimeOffset? fromDate) currentVersion = (null, null, null, null);
            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                existingVersions = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.TeamVersion} WHERE TeamId = @TeamId", auditable).ConfigureAwait(false);
                currentVersion = await connection.QuerySingleAsync<(Guid teamVersionId, string teamName, string comparableName, DateTimeOffset? fromDate)>(
                    $"SELECT TeamVersionId, TeamName, ComparableName, FromDate FROM {Tables.TeamVersion} WHERE TeamId = @TeamId AND UntilDate IS NULL", auditable).ConfigureAwait(false);
            }

            _ = await updateMethod(team, Guid.NewGuid(), "Person 1").ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var totalVersions = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.TeamVersion} WHERE TeamId = @TeamId", auditable).ConfigureAwait(false);
                Assert.Equal(existingVersions, totalVersions);

                var versionResult = await connection.QuerySingleAsync<(Guid teamVersionId, string teamName, string comparableName, DateTimeOffset? fromDate)>(
                    $"SELECT TeamVersionId, TeamName, ComparableName, FromDate FROM {Tables.TeamVersion} WHERE TeamId = @TeamId AND UntilDate IS NULL", auditable).ConfigureAwait(false);

                Assert.Equal(currentVersion.teamVersionId, versionResult.teamVersionId);
                Assert.Equal(auditable.TeamName, versionResult.teamName);
                Assert.Equal(auditable.ComparableName(), versionResult.comparableName);
                Assert.Equal(currentVersion.fromDate.Value, versionResult.fromDate.Value);
            }
        }

        [Fact]
        public async Task Update_team_inserts_redirect()
        {
            var team = _databaseFixture.TestData.Teams.First();

            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(team.TeamRoute, "/teams", team.TeamName, NoiseWords.TeamRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(team.TeamRoute + "-123"));

            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(team)).Returns(new Team { TeamId = team.TeamId, TeamName = team.TeamName, TeamRoute = team.TeamRoute });

            var redirectsRepository = new Mock<IRedirectsRepository>();

            var repo = new SqlServerTeamRepository(
                     _databaseFixture.ConnectionFactory,
                     Mock.Of<IAuditRepository>(),
                     Mock.Of<ILogger<SqlServerTeamRepository>>(),
                     routeGenerator.Object,
                     redirectsRepository.Object,
                     Mock.Of<IMemberGroupHelper>(),
                     Mock.Of<IHtmlSanitizer>(),
                     copier.Object,
                     Mock.Of<IUrlFormatter>(),
                     Mock.Of<ISocialMediaAccountFormatter>());

            _ = await repo.UpdateTeam(team, Guid.NewGuid(), "Person 1").ConfigureAwait(false);

            redirectsRepository.Verify(x => x.InsertRedirect(team.TeamRoute, team.TeamRoute + "-123", null, It.IsAny<IDbTransaction>()), Times.Once);
        }


        [Fact]
        public async Task Update_team_does_not_redirect_unchanged_route()
        {
            var team = _databaseFixture.TestData.Teams.First();
            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(team.TeamRoute, "/teams", team.TeamName, NoiseWords.TeamRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(team.TeamRoute));
            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(team)).Returns(new Team { TeamId = team.TeamId, TeamName = team.TeamName, TeamRoute = team.TeamRoute });
            var redirectsRepository = new Mock<IRedirectsRepository>();

            var repo = new SqlServerTeamRepository(
               _databaseFixture.ConnectionFactory,
               Mock.Of<IAuditRepository>(),
               Mock.Of<ILogger<SqlServerTeamRepository>>(),
               routeGenerator.Object,
               Mock.Of<IRedirectsRepository>(),
               Mock.Of<IMemberGroupHelper>(),
               Mock.Of<IHtmlSanitizer>(),
               copier.Object,
               Mock.Of<IUrlFormatter>(),
               Mock.Of<ISocialMediaAccountFormatter>());

            _ = await repo.UpdateTeam(team, Guid.NewGuid(), "Person 1").ConfigureAwait(false);

            redirectsRepository.Verify(x => x.InsertRedirect(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDbTransaction>()), Times.Never);
        }

        [Fact]
        public async Task Update_team_audits_and_logs()
        {
            var team = _databaseFixture.TestData.Teams.First();

            var auditable = new Team
            {
                TeamName = team.TeamName + Guid.NewGuid(),
                TeamRoute = team.TeamRoute,
                MemberGroupKey = team.MemberGroupKey,
                MemberGroupName = team.MemberGroupName
            };

            var redacted = new Team
            {
                TeamName = team.TeamName + Guid.NewGuid(),
                TeamRoute = team.TeamRoute,
                MemberGroupKey = team.MemberGroupKey,
                MemberGroupName = team.MemberGroupName
            };

            var route = "/teams/" + Guid.NewGuid();
            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(team.TeamRoute, "/teams", auditable.TeamName, NoiseWords.TeamRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(route));

            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(team)).Returns(auditable);
            copier.Setup(x => x.CreateRedactedCopy(auditable)).Returns(redacted);

            var auditRepository = new Mock<IAuditRepository>();
            var logger = new Mock<ILogger<SqlServerTeamRepository>>();

            var repo = new SqlServerTeamRepository(
                 _databaseFixture.ConnectionFactory,
                 auditRepository.Object,
                 logger.Object,
                 routeGenerator.Object,
                 Mock.Of<IRedirectsRepository>(),
                 Mock.Of<IMemberGroupHelper>(),
                 Mock.Of<IHtmlSanitizer>(),
                 copier.Object,
                 Mock.Of<IUrlFormatter>(),
                 Mock.Of<ISocialMediaAccountFormatter>());
            var memberKey = Guid.NewGuid();
            var memberName = "Person 1";

            _ = await repo.UpdateTeam(team, memberKey, memberName).ConfigureAwait(false);

            copier.Verify(x => x.CreateRedactedCopy(auditable), Times.Once);
            auditRepository.Verify(x => x.CreateAudit(It.IsAny<AuditRecord>(), It.IsAny<IDbTransaction>()), Times.Once);
            logger.Verify(x => x.Info(LoggingTemplates.Updated, redacted, memberName, memberKey, typeof(SqlServerTeamRepository), nameof(SqlServerTeamRepository.UpdateTeam)), Times.Once);
        }

        [Fact]
        public async Task Update_team_returns_a_copy()
        {
            var team = _databaseFixture.TestData.Teams.First();

            var copyTeam = new Team
            {
                TeamName = team.TeamName + " copy",
                MemberGroupKey = team.MemberGroupKey,
                MemberGroupName = team.MemberGroupName
            };

            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(team.TeamName, "/teams", copyTeam.TeamName, NoiseWords.TeamRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult("/teams/" + Guid.NewGuid()));

            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(team)).Returns(copyTeam);

            var repo = new SqlServerTeamRepository(
                     _databaseFixture.ConnectionFactory,
                     Mock.Of<IAuditRepository>(),
                     Mock.Of<ILogger<SqlServerTeamRepository>>(),
                     routeGenerator.Object,
                     Mock.Of<IRedirectsRepository>(),
                     Mock.Of<IMemberGroupHelper>(),
                     Mock.Of<IHtmlSanitizer>(),
                     copier.Object,
                     Mock.Of<IUrlFormatter>(),
                     Mock.Of<ISocialMediaAccountFormatter>());

            var updated = await repo.UpdateTeam(team, Guid.NewGuid(), "Member name").ConfigureAwait(false);
            copier.Verify(x => x.CreateAuditableCopy(team), Times.Once);
            Assert.Equal(copyTeam, updated);
        }

        [Fact]
        public async Task Update_team_inserts_new_match_location()
        {
            var team = _databaseFixture.TestData.Teams.First(x => x.MatchLocations.Any());
            var locationToAdd = _databaseFixture.TestData.MatchLocations.First(x => !team.MatchLocations.Select(ml => ml.MatchLocationId).Contains(x.MatchLocationId));

            var auditable = new Team
            {
                TeamId = team.TeamId,
                TeamName = team.TeamName,
                TeamRoute = team.TeamRoute,
                MatchLocations = team.MatchLocations.Select(x => new MatchLocation { MatchLocationId = x.MatchLocationId }).ToList()
            };
            auditable.MatchLocations.Add(new MatchLocation { MatchLocationId = locationToAdd.MatchLocationId });

            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(team.TeamRoute, "/teams", team.TeamName, NoiseWords.TeamRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(team.TeamRoute));

            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(team)).Returns(auditable);

            var repo = new SqlServerTeamRepository(
                     _databaseFixture.ConnectionFactory,
                     Mock.Of<IAuditRepository>(),
                     Mock.Of<ILogger<SqlServerTeamRepository>>(),
                     routeGenerator.Object,
                     Mock.Of<IRedirectsRepository>(),
                     Mock.Of<IMemberGroupHelper>(),
                     Mock.Of<IHtmlSanitizer>(),
                     copier.Object,
                     Mock.Of<IUrlFormatter>(),
                     Mock.Of<ISocialMediaAccountFormatter>());

            _ = await repo.UpdateTeam(team, Guid.NewGuid(), "Member name").ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var totalLocations = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.TeamMatchLocation} WHERE TeamId = @TeamId AND UntilDate IS NULL", auditable).ConfigureAwait(false);
                Assert.Equal(auditable.MatchLocations.Count, totalLocations);

                var locationResult = await connection.QuerySingleOrDefaultAsync<(Guid teamMatchLocationId, DateTimeOffset? fromDate)>(
                    $"SELECT TeamMatchLocationId, FromDate FROM {Tables.TeamMatchLocation} WHERE TeamId = @TeamId AND MatchLocationId = @MatchLocationId AND UntilDate IS NULL", new { auditable.TeamId, locationToAdd.MatchLocationId }).ConfigureAwait(false);

                Assert.Equal(DateTimeOffset.UtcNow.Date, locationResult.fromDate.Value.UtcDateTime);
            }
        }

        [Fact]
        public async Task Update_team_does_not_alter_unchanged_match_location()
        {
            var team = _databaseFixture.TestData.Teams.First(x => x.MatchLocations.Any());

            var auditable = new Team
            {
                TeamId = team.TeamId,
                TeamName = team.TeamName,
                TeamRoute = team.TeamRoute,
                MatchLocations = team.MatchLocations.Select(x => new MatchLocation { MatchLocationId = x.MatchLocationId }).ToList()
            };

            var before = new List<(Guid teamMatchLocationId, Guid matchLocationId, DateTimeOffset fromDate, DateTimeOffset? untilDate)>();
            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                before = (await connection.QueryAsync<(Guid teamMatchLocationId, Guid matchLocationId, DateTimeOffset fromDate, DateTimeOffset? untilDate)>(
                    $"SELECT TeamMatchLocationId, MatchLocationId, FromDate, UntilDate FROM {Tables.TeamMatchLocation} WHERE TeamId = @TeamId", auditable)
                    .ConfigureAwait(false)).ToList();
            }

            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(team.TeamRoute, "/teams", team.TeamName, NoiseWords.TeamRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(team.TeamRoute));

            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(team)).Returns(auditable);

            var repo = new SqlServerTeamRepository(
                     _databaseFixture.ConnectionFactory,
                     Mock.Of<IAuditRepository>(),
                     Mock.Of<ILogger<SqlServerTeamRepository>>(),
                     routeGenerator.Object,
                     Mock.Of<IRedirectsRepository>(),
                     Mock.Of<IMemberGroupHelper>(),
                     Mock.Of<IHtmlSanitizer>(),
                     copier.Object,
                     Mock.Of<IUrlFormatter>(),
                     Mock.Of<ISocialMediaAccountFormatter>());

            _ = await repo.UpdateTeam(team, Guid.NewGuid(), "Member name").ConfigureAwait(false);

            var after = new List<(Guid teamMatchLocationId, Guid matchLocationId, DateTimeOffset fromDate, DateTimeOffset? untilDate)>();
            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                after = (await connection.QueryAsync<(Guid teamMatchLocationId, Guid matchLocationId, DateTimeOffset fromDate, DateTimeOffset? untilDate)>(
                    $"SELECT TeamMatchLocationId, MatchLocationId, FromDate, UntilDate FROM {Tables.TeamMatchLocation} WHERE TeamId = @TeamId", auditable)
                    .ConfigureAwait(false)).ToList();
            }

            Assert.Equal(before.Count, after.Count);
            foreach (var fromBefore in before)
            {
                var fromAfter = after.SingleOrDefault(x => x.teamMatchLocationId == fromBefore.teamMatchLocationId);
                Assert.Equal(fromBefore.matchLocationId, fromAfter.matchLocationId);
                Assert.Equal(fromBefore.fromDate, fromAfter.fromDate);
                Assert.Equal(fromBefore.untilDate, fromAfter.untilDate);
            }
        }

        [Fact]
        public async Task Update_team_sets_until_date_for_removed_match_location()
        {
            var team = _databaseFixture.TestData.Teams.First(x => x.MatchLocations.Count > 1);
            var locationToRemove = team.MatchLocations.First();

            var auditable = new Team
            {
                TeamId = team.TeamId,
                TeamName = team.TeamName,
                TeamRoute = team.TeamRoute,
                MatchLocations = team.MatchLocations.Where(x => x.MatchLocationId != locationToRemove.MatchLocationId).Select(x => new MatchLocation { MatchLocationId = x.MatchLocationId }).ToList()
            };

            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(team.TeamRoute, "/teams", team.TeamName, NoiseWords.TeamRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(team.TeamRoute));

            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(team)).Returns(auditable);

            var repo = new SqlServerTeamRepository(
                     _databaseFixture.ConnectionFactory,
                     Mock.Of<IAuditRepository>(),
                     Mock.Of<ILogger<SqlServerTeamRepository>>(),
                     routeGenerator.Object,
                     Mock.Of<IRedirectsRepository>(),
                     Mock.Of<IMemberGroupHelper>(),
                     Mock.Of<IHtmlSanitizer>(),
                     copier.Object,
                     Mock.Of<IUrlFormatter>(),
                     Mock.Of<ISocialMediaAccountFormatter>());

            _ = await repo.UpdateTeam(team, Guid.NewGuid(), "Member name").ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var totalLocations = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.TeamMatchLocation} WHERE TeamId = @TeamId AND UntilDate IS NULL", auditable).ConfigureAwait(false);
                Assert.Equal(team.MatchLocations.Count - 1, totalLocations);
                Assert.Equal(auditable.MatchLocations.Count, totalLocations);

                var locationResult = await connection.QuerySingleOrDefaultAsync<(Guid teamMatchLocationId, DateTimeOffset? untilDate)>(
                    $"SELECT TOP 1 TeamMatchLocationId, UntilDate FROM {Tables.TeamMatchLocation} WHERE TeamId = @TeamId AND MatchLocationId = @MatchLocationId AND UntilDate IS NOT NULL ORDER BY UntilDate DESC",
                    new { auditable.TeamId, locationToRemove.MatchLocationId }
                ).ConfigureAwait(false);

                Assert.Equal(DateTimeOffset.UtcNow.Date.AddDays(1).AddSeconds(-1), locationResult.untilDate.Value.UtcDateTime);
            }
        }

        [Fact]
        public async Task Update_transient_team_succeeds()
        {
            var unsanitisedIntro = Guid.NewGuid().ToString();
            var sanitisedIntro = $"<p>This is the sanitised intro {unsanitisedIntro}</p>";
            var unsanitisedPlayingTimes = Guid.NewGuid().ToString();
            var unsanitisedCost = Guid.NewGuid().ToString();
            var sanitisedCost = $"<p>Sanitised cost {unsanitisedCost}</p>";
            var unsanitisedPrivateContact = Guid.NewGuid().ToString();
            var sanitisedPrivateContact = $"<p>Sanitised private details {unsanitisedPrivateContact}</p>";
            var unsanitisedPublicContact = Guid.NewGuid().ToString();
            var sanitisedPublicContact = $"<p>Sanitised public details {unsanitisedPublicContact}</p>";
            var updatedFacebook = $"www.facebook.com/{Guid.NewGuid()}";
            var updatedInstagram = Guid.NewGuid().ToString().Substring(0, 15);
            var updatedTwitter = Guid.NewGuid().ToString().Substring(0, 15);
            var updatedYouTube = $"www.youtube.com/{Guid.NewGuid()}";
            var updatedWebsite = $"www.example.org/{Guid.NewGuid()}";

            var team = _databaseFixture.TestData.TeamWithFullDetails;

            var auditable = new Team
            {
                TeamId = team.TeamId,
                TeamRoute = team.TeamRoute,
                Introduction = unsanitisedIntro,
                PlayingTimes = unsanitisedPlayingTimes,
                Cost = unsanitisedCost,
                TeamType = team.TeamType == TeamType.Occasional ? TeamType.LimitedMembership : TeamType.Occasional,
                PlayerType = team.PlayerType == PlayerType.Ladies ? PlayerType.Mixed : PlayerType.Ladies,
                TeamName = Guid.NewGuid().ToString(),
                UntilYear = team.UntilYear.HasValue ? team.UntilYear.Value + 1 : 2021,
                AgeRangeLower = team.AgeRangeLower == 11 ? 18 : 11,
                AgeRangeUpper = team.AgeRangeUpper == 70 ? 75 : 70,
                Facebook = updatedFacebook,
                Instagram = updatedInstagram,
                YouTube = updatedYouTube,
                Twitter = updatedTwitter,
                Website = updatedWebsite,
                ClubMark = !team.ClubMark,
                PrivateContactDetails = unsanitisedPrivateContact,
                PublicContactDetails = unsanitisedPublicContact,
            };

            var route = "/teams/" + Guid.NewGuid();
            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(team.TeamRoute, Regex.Match(auditable.TeamRoute, @"^\/tournaments\/[a-z0-9-]+\/teams").Value, auditable.TeamName, NoiseWords.TeamRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(route));

            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(team)).Returns(auditable);

            var sanitizer = new Mock<IHtmlSanitizer>();
            sanitizer.Setup(x => x.Sanitize(auditable.PrivateContactDetails)).Returns(sanitisedPrivateContact);
            sanitizer.Setup(x => x.Sanitize(auditable.PublicContactDetails)).Returns(sanitisedPublicContact);
            sanitizer.Setup(x => x.Sanitize(auditable.Introduction)).Returns(sanitisedIntro);
            sanitizer.Setup(x => x.Sanitize(auditable.Cost)).Returns(sanitisedCost);

            var urlFormatter = new Mock<IUrlFormatter>();
            urlFormatter.Setup(x => x.PrefixHttpsProtocol(auditable.Facebook)).Returns(new Uri("https://" + auditable.Facebook));
            urlFormatter.Setup(x => x.PrefixHttpsProtocol(auditable.YouTube)).Returns(new Uri("https://" + auditable.YouTube));
            urlFormatter.Setup(x => x.PrefixHttpsProtocol(auditable.Website)).Returns(new Uri("https://" + auditable.Website));

            var socialMediaFormatter = new Mock<ISocialMediaAccountFormatter>();
            socialMediaFormatter.Setup(x => x.PrefixAtSign(auditable.Instagram)).Returns("@" + auditable.Instagram);
            socialMediaFormatter.Setup(x => x.PrefixAtSign(auditable.Twitter)).Returns("@" + auditable.Twitter);

            var repo = new SqlServerTeamRepository(
                       _databaseFixture.ConnectionFactory,
                       Mock.Of<IAuditRepository>(),
                       Mock.Of<ILogger<SqlServerTeamRepository>>(),
                       routeGenerator.Object,
                       Mock.Of<IRedirectsRepository>(),
                       Mock.Of<IMemberGroupHelper>(),
                       sanitizer.Object,
                       copier.Object,
                       urlFormatter.Object,
                       socialMediaFormatter.Object);

            var updated = await repo.UpdateTransientTeam(team, Guid.NewGuid(), "Member name").ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var teamResult = await connection.QuerySingleOrDefaultAsync<Team>(
                    $@"SELECT TeamType, PlayerType, Introduction, AgeRangeLower, AgeRangeUpper, PlayingTimes, Cost,
                                Facebook, Instagram, YouTube, Twitter, Website, ClubMark,
                                PrivateContactDetails, PublicContactDetails, TeamRoute,
                                YEAR(FromDate) AS FromYear, YEAR(UntilDate) AS UntilYear
                                FROM {Tables.Team} co INNER JOIN {Tables.TeamVersion} cv ON co.TeamId = cv.TeamId
                                WHERE co.TeamId = @TeamId",
                    new { team.TeamId }).ConfigureAwait(false);
                Assert.NotNull(teamResult);

                sanitizer.Verify(x => x.Sanitize(unsanitisedIntro));
                Assert.Equal(sanitisedIntro, teamResult.Introduction);
                sanitizer.Verify(x => x.Sanitize(unsanitisedCost));
                Assert.Equal(sanitisedCost, teamResult.Cost);
                Assert.Equal(auditable.PlayerType, teamResult.PlayerType);
                Assert.Equal(auditable.AgeRangeLower, teamResult.AgeRangeLower);
                Assert.Equal(auditable.AgeRangeUpper, teamResult.AgeRangeUpper);
                urlFormatter.Verify(x => x.PrefixHttpsProtocol(updatedFacebook), Times.Once);
                Assert.Equal("https://" + updatedFacebook, teamResult.Facebook);
                socialMediaFormatter.Verify(x => x.PrefixAtSign(updatedInstagram), Times.Once);
                Assert.Equal("@" + updatedInstagram, teamResult.Instagram);
                urlFormatter.Verify(x => x.PrefixHttpsProtocol(updatedYouTube), Times.Once);
                Assert.Equal("https://" + updatedYouTube, teamResult.YouTube);
                socialMediaFormatter.Verify(x => x.PrefixAtSign(updatedTwitter), Times.Once);
                Assert.Equal("@" + updatedTwitter, teamResult.Twitter);
                urlFormatter.Verify(x => x.PrefixHttpsProtocol(updatedWebsite), Times.Once);
                Assert.Equal("https://" + updatedWebsite, teamResult.Website);
                sanitizer.Verify(x => x.Sanitize(unsanitisedPrivateContact));
                Assert.Equal(sanitisedPrivateContact, teamResult.PrivateContactDetails);
                sanitizer.Verify(x => x.Sanitize(unsanitisedPublicContact));
                Assert.Equal(sanitisedPublicContact, teamResult.PublicContactDetails);
                Assert.Equal(route, teamResult.TeamRoute);

                // Test that these fields are NOT altered
                Assert.Equal(team.TeamType, teamResult.TeamType);
                Assert.Equal(team.PlayingTimes, teamResult.PlayingTimes);
                Assert.Equal(team.UntilYear, teamResult.UntilYear);
                Assert.Equal(team.ClubMark, teamResult.ClubMark);

                var versionResult = await connection.QuerySingleAsync<(string TeamName, string comparableName)>(
                     $"SELECT TOP 1 TeamName, ComparableName FROM {Tables.TeamVersion} WHERE TeamId = @TeamId ORDER BY FromDate DESC",
                       new { updated.TeamId }
                     ).ConfigureAwait(false);

                Assert.Equal(auditable.TeamName, versionResult.TeamName);
                Assert.Equal(auditable.ComparableName(), versionResult.comparableName);
            }
        }

        [Fact(Skip = "Team versioning is not implemented yet. See https://github.com/stoolball-england/stoolball-org-uk/issues/474")]
        public async Task Update_transient_team_adds_team_version_if_name_changes()
        {
            var team = _databaseFixture.TestData.Teams.First(x => x.TeamType == TeamType.Transient && !x.UntilYear.HasValue);
            var auditable = new Team
            {
                TeamId = team.TeamId,
                TeamName = team.TeamName + Guid.NewGuid().ToString(),
                TeamRoute = team.TeamRoute
            };

            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(team.TeamRoute, Regex.Match(auditable.TeamRoute, @"^\/tournaments\/[a-z0-9-]+\/teams").Value, team.TeamName, NoiseWords.TeamRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(team.TeamRoute));
            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(team)).Returns(auditable);

            var repo = new SqlServerTeamRepository(
                     _databaseFixture.ConnectionFactory,
                     Mock.Of<IAuditRepository>(),
                     Mock.Of<ILogger<SqlServerTeamRepository>>(),
                     routeGenerator.Object,
                     Mock.Of<IRedirectsRepository>(),
                     Mock.Of<IMemberGroupHelper>(),
                     Mock.Of<IHtmlSanitizer>(),
                     copier.Object,
                     Mock.Of<IUrlFormatter>(),
                     Mock.Of<ISocialMediaAccountFormatter>());

            await UpdateTeamAddsTeamVersionIfNameChanges(team, auditable, (team, memberKey, memberName) => repo.UpdateTransientTeam(team, memberKey, memberName)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Update_transient_team_does_not_add_team_version_if_name_is_unchanged()
        {
            var team = _databaseFixture.TestData.Teams.First(x => !x.UntilYear.HasValue);
            var auditable = new Team
            {
                TeamId = team.TeamId,
                TeamName = team.TeamName,
                TeamRoute = team.TeamRoute
            };

            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(team.TeamRoute, Regex.Match(auditable.TeamRoute, @"^\/tournaments\/[a-z0-9-]+\/teams").Value, team.TeamName, NoiseWords.TeamRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(team.TeamRoute));

            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(team)).Returns(auditable);

            var repo = new SqlServerTeamRepository(
               _databaseFixture.ConnectionFactory,
               Mock.Of<IAuditRepository>(),
               Mock.Of<ILogger<SqlServerTeamRepository>>(),
               routeGenerator.Object,
               Mock.Of<IRedirectsRepository>(),
               Mock.Of<IMemberGroupHelper>(),
               Mock.Of<IHtmlSanitizer>(),
               copier.Object,
               Mock.Of<IUrlFormatter>(),
               Mock.Of<ISocialMediaAccountFormatter>());

            await UpdateTeamDoesNotAddTeamVersionIfNameIsUnchanged(team, auditable, (team, memberKey, memberName) => repo.UpdateTransientTeam(team, memberKey, memberName)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Update_transient_team_inserts_redirect()
        {
            var team = _databaseFixture.TestData.Teams.First();

            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(team.TeamRoute, Regex.Match(team.TeamRoute, @"^\/tournaments\/[a-z0-9-]+\/teams").Value, team.TeamName, NoiseWords.TeamRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(team.TeamRoute + "-123"));

            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(team)).Returns(new Team { TeamId = team.TeamId, TeamName = team.TeamName, TeamRoute = team.TeamRoute });

            var redirectsRepository = new Mock<IRedirectsRepository>();

            var repo = new SqlServerTeamRepository(
                     _databaseFixture.ConnectionFactory,
                     Mock.Of<IAuditRepository>(),
                     Mock.Of<ILogger<SqlServerTeamRepository>>(),
                     routeGenerator.Object,
                     redirectsRepository.Object,
                     Mock.Of<IMemberGroupHelper>(),
                     Mock.Of<IHtmlSanitizer>(),
                     copier.Object,
                     Mock.Of<IUrlFormatter>(),
                     Mock.Of<ISocialMediaAccountFormatter>());

            _ = await repo.UpdateTransientTeam(team, Guid.NewGuid(), "Person 1").ConfigureAwait(false);

            redirectsRepository.Verify(x => x.InsertRedirect(team.TeamRoute, team.TeamRoute + "-123", null, It.IsAny<IDbTransaction>()), Times.Once);
        }

        [Fact]
        public async Task Update__transient_team_does_not_redirect_unchanged_route()
        {
            var team = _databaseFixture.TestData.Teams.First();
            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(team.TeamRoute, Regex.Match(team.TeamRoute, @"^\/tournaments\/[a-z0-9-]+\/teams").Value, team.TeamName, NoiseWords.TeamRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(team.TeamRoute));
            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(team)).Returns(new Team { TeamId = team.TeamId, TeamName = team.TeamName, TeamRoute = team.TeamRoute });
            var redirectsRepository = new Mock<IRedirectsRepository>();

            var repo = new SqlServerTeamRepository(
               _databaseFixture.ConnectionFactory,
               Mock.Of<IAuditRepository>(),
               Mock.Of<ILogger<SqlServerTeamRepository>>(),
               routeGenerator.Object,
               Mock.Of<IRedirectsRepository>(),
               Mock.Of<IMemberGroupHelper>(),
               Mock.Of<IHtmlSanitizer>(),
               copier.Object,
               Mock.Of<IUrlFormatter>(),
               Mock.Of<ISocialMediaAccountFormatter>());

            _ = await repo.UpdateTransientTeam(team, Guid.NewGuid(), "Person 1").ConfigureAwait(false);

            redirectsRepository.Verify(x => x.InsertRedirect(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDbTransaction>()), Times.Never);
        }

        [Fact]
        public async Task Update_transient_team_audits_and_logs()
        {
            var team = _databaseFixture.TestData.Teams.First();

            var auditable = new Team
            {
                TeamName = team.TeamName + Guid.NewGuid(),
                TeamRoute = team.TeamRoute,
                MemberGroupKey = team.MemberGroupKey,
                MemberGroupName = team.MemberGroupName
            };

            var redacted = new Team
            {
                TeamName = team.TeamName + Guid.NewGuid(),
                TeamRoute = team.TeamRoute,
                MemberGroupKey = team.MemberGroupKey,
                MemberGroupName = team.MemberGroupName
            };

            var route = "/teams/" + Guid.NewGuid();
            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(team.TeamRoute, Regex.Match(auditable.TeamRoute, @"^\/tournaments\/[a-z0-9-]+\/teams").Value, auditable.TeamName, NoiseWords.TeamRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(route));

            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(team)).Returns(auditable);
            copier.Setup(x => x.CreateRedactedCopy(auditable)).Returns(redacted);

            var auditRepository = new Mock<IAuditRepository>();
            var logger = new Mock<ILogger<SqlServerTeamRepository>>();

            var repo = new SqlServerTeamRepository(
                 _databaseFixture.ConnectionFactory,
                 auditRepository.Object,
                 logger.Object,
                 routeGenerator.Object,
                 Mock.Of<IRedirectsRepository>(),
                 Mock.Of<IMemberGroupHelper>(),
                 Mock.Of<IHtmlSanitizer>(),
                 copier.Object,
                 Mock.Of<IUrlFormatter>(),
                 Mock.Of<ISocialMediaAccountFormatter>());
            var memberKey = Guid.NewGuid();
            var memberName = "Person 1";

            _ = await repo.UpdateTransientTeam(team, memberKey, memberName).ConfigureAwait(false);

            copier.Verify(x => x.CreateRedactedCopy(auditable), Times.Once);
            auditRepository.Verify(x => x.CreateAudit(It.IsAny<AuditRecord>(), It.IsAny<IDbTransaction>()), Times.Once);
            logger.Verify(x => x.Info(LoggingTemplates.Updated, redacted, memberName, memberKey, typeof(SqlServerTeamRepository), nameof(SqlServerTeamRepository.UpdateTransientTeam)), Times.Once);
        }

        [Fact]
        public async Task Update_transient_team_returns_a_copy()
        {
            var team = _databaseFixture.TestData.Teams.First();

            var copyTeam = new Team
            {
                TeamName = team.TeamName + " copy",
                TeamRoute = team.TeamRoute,
                MemberGroupKey = team.MemberGroupKey,
                MemberGroupName = team.MemberGroupName
            };

            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(team.TeamName, "/teams", copyTeam.TeamName, NoiseWords.TeamRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult("/teams/" + Guid.NewGuid()));

            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(team)).Returns(copyTeam);

            var repo = new SqlServerTeamRepository(
                     _databaseFixture.ConnectionFactory,
                     Mock.Of<IAuditRepository>(),
                     Mock.Of<ILogger<SqlServerTeamRepository>>(),
                     routeGenerator.Object,
                     Mock.Of<IRedirectsRepository>(),
                     Mock.Of<IMemberGroupHelper>(),
                     Mock.Of<IHtmlSanitizer>(),
                     copier.Object,
                     Mock.Of<IUrlFormatter>(),
                     Mock.Of<ISocialMediaAccountFormatter>());

            var updated = await repo.UpdateTransientTeam(team, Guid.NewGuid(), "Member name").ConfigureAwait(false);
            copier.Verify(x => x.CreateAuditableCopy(team), Times.Once);
            Assert.Equal(copyTeam, updated);
        }

        [Fact]
        public async Task Delete_team_succeeds()
        {
            var auditable = new Team { TeamId = _databaseFixture.TestData.TeamWithFullDetails.TeamId };
            var redacted = new Team { TeamId = _databaseFixture.TestData.TeamWithFullDetails.TeamId };

            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(_databaseFixture.TestData.TeamWithFullDetails)).Returns(auditable);
            copier.Setup(x => x.CreateAuditableCopy(auditable)).Returns(redacted);

            var memberKey = Guid.NewGuid();
            var memberName = "Dee Leeter";

            var repo = new SqlServerTeamRepository(
                _databaseFixture.ConnectionFactory,
                Mock.Of<IAuditRepository>(),
                Mock.Of<ILogger<SqlServerTeamRepository>>(),
                Mock.Of<IRouteGenerator>(),
                Mock.Of<IRedirectsRepository>(),
                Mock.Of<IMemberGroupHelper>(),
                Mock.Of<IHtmlSanitizer>(),
                copier.Object,
                Mock.Of<IUrlFormatter>(),
                Mock.Of<ISocialMediaAccountFormatter>());

            await repo.DeleteTeam(_databaseFixture.TestData.TeamWithFullDetails, memberKey, memberName).ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<Guid?>($"SELECT TeamId FROM {Tables.Team} WHERE TeamId = @TeamId", new { _databaseFixture.TestData.TeamWithFullDetails.TeamId }).ConfigureAwait(false);
                Assert.Null(result);
            }
        }

        public void Dispose() => _scope.Dispose();
    }
}
