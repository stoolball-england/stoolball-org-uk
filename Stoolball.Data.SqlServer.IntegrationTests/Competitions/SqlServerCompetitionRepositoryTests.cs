using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using AngleSharp.Css.Dom;
using Dapper;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Html;
using Stoolball.Logging;
using Stoolball.Routing;
using Stoolball.Teams;
using Xunit;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer.IntegrationTests.Competitions
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class SqlServerCompetitionRepositoryTests : IDisposable
    {
        private readonly SqlServerTestDataFixture _databaseFixture;
        private readonly TransactionScope _scope;
        private readonly Mock<IRouteGenerator> _routeGenerator = new();
        private readonly Mock<IStoolballEntityCopier> _copier = new();
        private readonly Mock<IHtmlSanitizer> _sanitizer = new();
        private readonly Mock<IUrlFormatter> _urlFormatter = new();
        private readonly Mock<ISocialMediaAccountFormatter> _socialMediaFormatter = new();
        private readonly Mock<IAuditRepository> _auditRepository = new();
        private readonly Mock<ILogger<SqlServerCompetitionRepository>> _logger = new();
        private readonly Mock<ISeasonRepository> _seasonRepository = new();
        private readonly Mock<IRedirectsRepository> _redirectsRepository = new();

        public SqlServerCompetitionRepositoryTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
            _scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        }

        private SqlServerCompetitionRepository CreateRepository(ISeasonRepository? seasonRepository = null)
        {
            return new SqlServerCompetitionRepository(
                                     _databaseFixture.ConnectionFactory,
                                     _auditRepository.Object,
                                     _logger.Object,
                                     seasonRepository != null ? seasonRepository : _seasonRepository.Object,
                                     _routeGenerator.Object,
                                     _redirectsRepository.Object,
                                     _sanitizer.Object,
                                     _copier.Object,
                                     _urlFormatter.Object,
                                     _socialMediaFormatter.Object);
        }

#nullable disable
        [Fact]
        public async Task Create_competition_throws_ArgumentNullException_if_competition_is_null()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.CreateCompetition(null, Guid.NewGuid(), "Member name").ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Create_competition_throws_ArgumentNullException_if_memberName_is_null()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.CreateCompetition(new Competition(), Guid.NewGuid(), null).ConfigureAwait(false)).ConfigureAwait(false);
        }
#nullable enable

        [Fact]
        public async Task Create_competition_throws_ArgumentNullException_if_memberName_is_empty_string()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.CreateCompetition(new Competition(), Guid.NewGuid(), string.Empty).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Create_minimal_competition_succeeds()
        {
            var competition = new Competition
            {
                CompetitionName = "Example competition",
                PlayerType = PlayerType.JuniorMixed,
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = "Test group"
            };

            var route = "/competitions/" + Guid.NewGuid();
            _routeGenerator.Setup(x => x.GenerateUniqueRoute("/competitions", competition.CompetitionName, NoiseWords.CompetitionRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(route));

            _copier.Setup(x => x.CreateAuditableCopy(competition)).Returns(competition);

            var repo = CreateRepository();
            var created = await repo.CreateCompetition(competition, Guid.NewGuid(), "Member name").ConfigureAwait(false);

            _routeGenerator.Verify(x => x.GenerateUniqueRoute("/competitions", competition.CompetitionName, NoiseWords.CompetitionRoute, It.IsAny<Func<string, Task<int>>>()), Times.Once);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var competitionResult = await connection.QuerySingleOrDefaultAsync<Competition>(
                    @$"SELECT MemberGroupKey, MemberGroupName, CompetitionRoute, PlayerType
                        FROM {Tables.Competition} 
                        WHERE CompetitionId = @CompetitionId",
                    new
                    {
                        created.CompetitionId
                    }).ConfigureAwait(false);

                Assert.NotNull(competitionResult);
                Assert.Equal(competition.MemberGroupKey, competitionResult.MemberGroupKey);
                Assert.Equal(competition.MemberGroupName, competitionResult.MemberGroupName);
                Assert.Equal(competition.CompetitionRoute, competitionResult.CompetitionRoute);
                Assert.Equal(competition.PlayerType, competitionResult.PlayerType);

                var versionResult = await connection.QuerySingleAsync<(string competitionName, string comparableName, DateTimeOffset? fromDate)>(
                    $"SELECT CompetitionName, ComparableName, FromDate FROM {Tables.CompetitionVersion} WHERE CompetitionId = @CompetitionId",
                    new { created.CompetitionId }
                ).ConfigureAwait(false);

                Assert.Equal(competition.CompetitionName, versionResult.competitionName);
                Assert.Equal(competition.ComparableName(), versionResult.comparableName);
                Assert.Equal(DateTime.UtcNow.Date, versionResult.fromDate?.Date);
            }
        }

        [Fact]
        public async Task Create_complete_competition_succeeds()
        {
            var originalIntro = "<p>This is the intro</p>";
            var sanitisedIntro = "<p>This is the sanitised intro</p>";
            var originalPrivateContact = "<p>Private contact details</p>";
            var sanitisedPrivateContact = "<p>Sanitised private details</p>";
            var originalPublicContact = "<p>Public contact details</p>";
            var sanitisedPublicContact = "<p>Sanitised public details</p>";

            var competition = new Competition
            {
                Introduction = originalIntro,
                PlayerType = PlayerType.JuniorGirls,
                CompetitionName = "Example competition",
                FromYear = 1999,
                UntilYear = 2021,
                Facebook = "facebook.com/example",
                Instagram = "exampleinsta",
                YouTube = "youtube.com/example",
                Twitter = "exampletweeter",
                Website = "example.org/",
                PrivateContactDetails = originalPrivateContact,
                PublicContactDetails = originalPublicContact,
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = "Test group"
            };

            var auditable = new Competition
            {
                Introduction = competition.Introduction,
                PlayerType = competition.PlayerType,
                CompetitionName = competition.CompetitionName,
                FromYear = competition.FromYear,
                UntilYear = competition.UntilYear,
                Facebook = competition.Facebook,
                Instagram = competition.Instagram,
                YouTube = competition.YouTube,
                Twitter = competition.Twitter,
                Website = competition.Website,
                PrivateContactDetails = competition.PrivateContactDetails,
                PublicContactDetails = competition.PublicContactDetails,
                MemberGroupKey = competition.MemberGroupKey,
                MemberGroupName = competition.MemberGroupName
            };

            var route = "/competitions/" + Guid.NewGuid();
            _routeGenerator.Setup(x => x.GenerateUniqueRoute("/competitions", auditable.CompetitionName, NoiseWords.CompetitionRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(route));

            _copier.Setup(x => x.CreateAuditableCopy(competition)).Returns(auditable);

            _sanitizer.Setup(x => x.Sanitize(auditable.PrivateContactDetails)).Returns(sanitisedPrivateContact);
            _sanitizer.Setup(x => x.Sanitize(auditable.PublicContactDetails)).Returns(sanitisedPublicContact);
            _sanitizer.Setup(x => x.Sanitize(auditable.Introduction)).Returns(sanitisedIntro);

            _urlFormatter.Setup(x => x.PrefixHttpsProtocol(auditable.Facebook)).Returns(new Uri("https://" + auditable.Facebook));
            _urlFormatter.Setup(x => x.PrefixHttpsProtocol(auditable.YouTube)).Returns(new Uri("https://" + auditable.YouTube));
            _urlFormatter.Setup(x => x.PrefixHttpsProtocol(auditable.Website)).Returns(new Uri("https://" + auditable.Website));

            _socialMediaFormatter.Setup(x => x.PrefixAtSign(auditable.Instagram)).Returns("@" + auditable.Instagram);
            _socialMediaFormatter.Setup(x => x.PrefixAtSign(auditable.Twitter)).Returns("@" + auditable.Twitter);

            var repo = CreateRepository();

            var created = await repo.CreateCompetition(competition, Guid.NewGuid(), "Member name").ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var competitionResult = await connection.QuerySingleOrDefaultAsync<Competition>(
                    $@"SELECT PlayerType, Introduction, Facebook, Instagram, YouTube, Twitter, Website, 
                                PrivateContactDetails, PublicContactDetails, MemberGroupKey, MemberGroupName, CompetitionRoute,
                                YEAR(FromDate) AS FromYear, YEAR(UntilDate) AS UntilYear
                                FROM {Tables.Competition} co INNER JOIN {Tables.CompetitionVersion} cv ON co.CompetitionId = cv.CompetitionId
                                WHERE co.CompetitionId = @CompetitionId",
                    new { created.CompetitionId }).ConfigureAwait(false);
                Assert.NotNull(competitionResult);

                _sanitizer.Verify(x => x.Sanitize(originalIntro));
                Assert.Equal(sanitisedIntro, competitionResult.Introduction);
                Assert.Equal(competition.FromYear, competitionResult.FromYear);
                Assert.Equal(competition.UntilYear, competitionResult.UntilYear);
                Assert.Equal(competition.PlayerType, competitionResult.PlayerType);
                _urlFormatter.Verify(x => x.PrefixHttpsProtocol(competition.Facebook), Times.Once);
                Assert.Equal("https://" + competition.Facebook, competitionResult.Facebook);
                _socialMediaFormatter.Verify(x => x.PrefixAtSign(competition.Instagram), Times.Once);
                Assert.Equal("@" + competition.Instagram, competitionResult.Instagram);
                _urlFormatter.Verify(x => x.PrefixHttpsProtocol(competition.YouTube), Times.Once);
                Assert.Equal("https://" + competition.YouTube, competitionResult.YouTube);
                _socialMediaFormatter.Verify(x => x.PrefixAtSign(competition.Twitter), Times.Once);
                Assert.Equal("@" + competition.Twitter, competitionResult.Twitter);
                _urlFormatter.Verify(x => x.PrefixHttpsProtocol(competition.Website), Times.Once);
                Assert.Equal("https://" + competition.Website, competitionResult.Website);
                _sanitizer.Verify(x => x.Sanitize(originalPrivateContact));
                Assert.Equal(sanitisedPrivateContact, competitionResult.PrivateContactDetails);
                _sanitizer.Verify(x => x.Sanitize(originalPublicContact));
                Assert.Equal(sanitisedPublicContact, competitionResult.PublicContactDetails);
                Assert.Equal(competition.MemberGroupKey, competitionResult.MemberGroupKey);
                Assert.Equal(competition.MemberGroupName, competitionResult.MemberGroupName);
                Assert.Equal(route, competitionResult.CompetitionRoute);
            }
        }

        [Fact]
        public async Task Create_competition_returns_a_copy()
        {
            var competition = new Competition
            {
                CompetitionName = "Example competition",
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = "Test group"
            };

            var copyCompetition = new Competition
            {
                CompetitionName = "Example competition copy",
                MemberGroupKey = competition.MemberGroupKey,
                MemberGroupName = competition.MemberGroupName
            };

            _routeGenerator.Setup(x => x.GenerateUniqueRoute("/competitions", copyCompetition.CompetitionName, NoiseWords.CompetitionRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult("/competitions/" + Guid.NewGuid()));

            _copier.Setup(x => x.CreateAuditableCopy(competition)).Returns(copyCompetition);

            var repo = CreateRepository();

            var created = await repo.CreateCompetition(competition, Guid.NewGuid(), "Member name").ConfigureAwait(false);

            _copier.Verify(x => x.CreateAuditableCopy(competition), Times.Once);
            Assert.Equal(copyCompetition, created);
        }

        [Fact]
        public async Task Create_competition_audits_and_logs()
        {
            var location = new Competition
            {
                CompetitionName = "New competition " + Guid.NewGuid(),
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = "Test group"
            };

            var auditable = new Competition
            {
                CompetitionName = "New competition " + Guid.NewGuid(),
                MemberGroupKey = location.MemberGroupKey,
                MemberGroupName = location.MemberGroupName
            };

            var redacted = new Competition
            {
                CompetitionName = "New competition " + Guid.NewGuid(),
                MemberGroupKey = location.MemberGroupKey,
                MemberGroupName = location.MemberGroupName
            };

            var route = "/competitions/" + Guid.NewGuid();
            _routeGenerator.Setup(x => x.GenerateUniqueRoute("/competitions", auditable.CompetitionName, NoiseWords.CompetitionRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(route));
            _copier.Setup(x => x.CreateAuditableCopy(location)).Returns(auditable);
            _copier.Setup(x => x.CreateRedactedCopy(auditable)).Returns(redacted);

            var repo = CreateRepository();
            var memberKey = Guid.NewGuid();
            var memberName = "Person 1";

            var created = await repo.CreateCompetition(location, memberKey, memberName).ConfigureAwait(false);

            _copier.Verify(x => x.CreateRedactedCopy(auditable), Times.Once);
            _auditRepository.Verify(x => x.CreateAudit(It.IsAny<AuditRecord>(), It.IsAny<IDbTransaction>()), Times.Once);
            _logger.Verify(x => x.Info(LoggingTemplates.Created, redacted, memberName, memberKey, typeof(SqlServerCompetitionRepository), nameof(SqlServerCompetitionRepository.CreateCompetition)));
        }

#nullable disable
        [Fact]
        public async Task Update_competition_throws_ArgumentNullException_if_competition_is_null()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.UpdateCompetition(null, Guid.NewGuid(), "Member name").ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Update_competition_throws_ArgumentNullException_if_memberName_is_null()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.UpdateCompetition(new Competition(), Guid.NewGuid(), null).ConfigureAwait(false)).ConfigureAwait(false);
        }
#nullable enable

        [Fact]
        public async Task Update_competition_throws_ArgumentNullException_if_memberName_is_empty_string()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.UpdateCompetition(new Competition(), Guid.NewGuid(), string.Empty).ConfigureAwait(false)).ConfigureAwait(false);
        }


        [Fact]
        public async Task Update_competition_returns_a_copy()
        {
            var competition = new Competition
            {
                CompetitionName = "Example competition",
                CompetitionRoute = "/competitions/example-competition",
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = "Test group"
            };

            var copyCompetition = new Competition
            {
                CompetitionName = competition.CompetitionName,
                MemberGroupKey = competition.MemberGroupKey,
                MemberGroupName = competition.MemberGroupName
            };

            _routeGenerator.Setup(x => x.GenerateUniqueRoute(competition.CompetitionRoute, "/competitions", copyCompetition.CompetitionName, NoiseWords.CompetitionRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult("/competitions/" + Guid.NewGuid()));

            _copier.Setup(x => x.CreateAuditableCopy(competition)).Returns(copyCompetition);

            var repo = CreateRepository();

            var updated = await repo.UpdateCompetition(competition, Guid.NewGuid(), "Member name").ConfigureAwait(false);

            _copier.Verify(x => x.CreateAuditableCopy(competition), Times.Once);
            Assert.Equal(copyCompetition, updated);
        }

        [Fact]
        public async Task Update_competition_succeeds()
        {
            var competition = _databaseFixture.TestData.Competitions.First();

            var originalIntro = string.IsNullOrEmpty(competition.Introduction) ? "Unsanitised introduction" : competition.Introduction;
            var originalPrivateContact = string.IsNullOrEmpty(competition.PrivateContactDetails) ? "Unsanitised private details" : competition.PrivateContactDetails;
            var originalPublicContact = string.IsNullOrEmpty(competition.PublicContactDetails) ? "Unsanitised public details" : competition.PublicContactDetails;
            var sanitisedIntro = "<p>This is the sanitised intro</p>";
            var sanitisedPrivateContact = "<p>Sanitised private details</p>";
            var sanitisedPublicContact = "<p>Sanitised public details</p>";
            var originalFacebook = "facebook.com/" + Guid.NewGuid().ToString();
            var originalInstagram = Guid.NewGuid().ToString();
            var originalYouTube = "youtube.com/example" + Guid.NewGuid().ToString();
            var originalTwitter = Guid.NewGuid().ToString();
            var originalWebsite = "example.org/" + Guid.NewGuid().ToString();

            var auditable = new Competition
            {
                CompetitionId = competition.CompetitionId,

                Introduction = originalIntro,
                PlayerType = competition.PlayerType == PlayerType.JuniorGirls ? PlayerType.Ladies : PlayerType.JuniorGirls,
                CompetitionName = Guid.NewGuid().ToString(),
                FromYear = competition.FromYear.HasValue ? competition.FromYear + 1 : 1999,
                UntilYear = competition.UntilYear.HasValue ? competition.UntilYear + 1 : 2021,
                Facebook = originalFacebook,
                Instagram = originalInstagram,
                YouTube = originalYouTube,
                Twitter = originalTwitter,
                Website = originalWebsite,
                PrivateContactDetails = originalPrivateContact,
                PublicContactDetails = originalPublicContact,
            };

            var updatedRoute = competition.CompetitionRoute + Guid.NewGuid();

            _routeGenerator.Setup(x => x.GenerateUniqueRoute(competition.CompetitionRoute, "/competitions", auditable.CompetitionName, NoiseWords.CompetitionRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(updatedRoute));

            _copier.Setup(x => x.CreateAuditableCopy(competition)).Returns(auditable);

            _sanitizer.Setup(x => x.Sanitize(auditable.PrivateContactDetails)).Returns(sanitisedPrivateContact);
            _sanitizer.Setup(x => x.Sanitize(auditable.PublicContactDetails)).Returns(sanitisedPublicContact);
            _sanitizer.Setup(x => x.Sanitize(auditable.Introduction)).Returns(sanitisedIntro);

            _urlFormatter.Setup(x => x.PrefixHttpsProtocol(auditable.Facebook)).Returns(new Uri("https://" + auditable.Facebook));
            _urlFormatter.Setup(x => x.PrefixHttpsProtocol(auditable.YouTube)).Returns(new Uri("https://" + auditable.YouTube));
            _urlFormatter.Setup(x => x.PrefixHttpsProtocol(auditable.Website)).Returns(new Uri("https://" + auditable.Website));

            _socialMediaFormatter.Setup(x => x.PrefixAtSign(auditable.Instagram)).Returns("@" + auditable.Instagram);
            _socialMediaFormatter.Setup(x => x.PrefixAtSign(auditable.Twitter)).Returns("@" + auditable.Twitter);

            var repo = CreateRepository();

            var updated = await repo.UpdateCompetition(competition, Guid.NewGuid(), "Person 1").ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var competitionResult = await connection.QuerySingleOrDefaultAsync<Competition>(
                        $@"SELECT PlayerType, Introduction, Facebook, Instagram, YouTube, Twitter, Website, 
                                PrivateContactDetails, PublicContactDetails, CompetitionRoute
                                FROM {Tables.Competition} 
                                WHERE CompetitionId = @CompetitionId",
                        new { updated.CompetitionId }).ConfigureAwait(false);
                Assert.NotNull(competitionResult);

                Assert.Equal(auditable.PlayerType, competitionResult.PlayerType);
                _sanitizer.Verify(x => x.Sanitize(originalIntro));
                Assert.Equal(sanitisedIntro, competitionResult.Introduction);
                _urlFormatter.Verify(x => x.PrefixHttpsProtocol(originalFacebook), Times.Once);
                Assert.Equal(auditable.Facebook, competitionResult.Facebook);
                Assert.StartsWith("https://", auditable.Facebook);
                _socialMediaFormatter.Verify(x => x.PrefixAtSign(originalInstagram), Times.Once);
                Assert.Equal(auditable.Instagram, competitionResult.Instagram);
                Assert.StartsWith("@", auditable.Instagram);
                _urlFormatter.Verify(x => x.PrefixHttpsProtocol(originalYouTube), Times.Once);
                Assert.Equal(auditable.YouTube, competitionResult.YouTube);
                Assert.StartsWith("https://", auditable.YouTube);
                _socialMediaFormatter.Verify(x => x.PrefixAtSign(originalTwitter), Times.Once);
                Assert.Equal(auditable.Twitter, competitionResult.Twitter);
                Assert.StartsWith("@", auditable.Twitter);
                _urlFormatter.Verify(x => x.PrefixHttpsProtocol(originalWebsite), Times.Once);
                Assert.Equal(auditable.Website, competitionResult.Website);
                Assert.StartsWith("https://", auditable.Website);
                _sanitizer.Verify(x => x.Sanitize(originalPrivateContact));
                Assert.Equal(sanitisedPrivateContact, competitionResult.PrivateContactDetails);
                _sanitizer.Verify(x => x.Sanitize(originalPublicContact));
                Assert.Equal(sanitisedPublicContact, competitionResult.PublicContactDetails);
                Assert.Equal(updatedRoute, competitionResult.CompetitionRoute);

                var versionResult = await connection.QuerySingleOrDefaultAsync<(string competitionName, string comparableName)>(
                    $"SELECT CompetitionName, ComparableName FROM {Tables.CompetitionVersion} WHERE CompetitionId = @CompetitionId", new { updated.CompetitionId }).ConfigureAwait(false);
                Assert.Equal(auditable.CompetitionName, versionResult.competitionName);
                Assert.Equal(auditable.ComparableName(), versionResult.comparableName);
            }
        }

        [Fact(Skip = "Competition versioning is not implemented yet. See https://github.com/stoolball-england/stoolball-org-uk/issues/473")]
        public async Task Update_competition_adds_competition_version_if_name_changes()
        {
            var competition = _databaseFixture.TestData.Competitions.First(x => !x.UntilYear.HasValue);

            var auditable = new Competition
            {
                CompetitionId = competition.CompetitionId,
                CompetitionName = competition.CompetitionName + " changed",
            };

            int? existingVersions = null;
            Guid? currentVersionId = null;
            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                existingVersions = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.CompetitionVersion} WHERE CompetitionId = @CompetitionId", auditable).ConfigureAwait(false);
                currentVersionId = await connection.ExecuteScalarAsync<Guid>($"SELECT CompetitionVersionId FROM {Tables.CompetitionVersion} WHERE CompetitionId = @CompetitionId AND UntilDate IS NULL", auditable).ConfigureAwait(false);
            }

            _routeGenerator.Setup(x => x.GenerateUniqueRoute(competition.CompetitionRoute, "/competitions", auditable.CompetitionName, NoiseWords.CompetitionRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(competition.CompetitionRoute));

            _copier.Setup(x => x.CreateAuditableCopy(competition)).Returns(auditable);

            var repo = CreateRepository();

            var updated = await repo.UpdateCompetition(competition, Guid.NewGuid(), "Person 1").ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var totalVersions = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.CompetitionVersion} WHERE CompetitionId = @CompetitionId", auditable).ConfigureAwait(false);
                Assert.Equal(existingVersions + 1, totalVersions);

                var versionResult = await connection.QuerySingleAsync<(Guid competitionVersionId, string competitionName, string comparableName, DateTimeOffset? fromDate)>(
                    $"SELECT CompetitionVersionId, CompetitionName, ComparableName, FromDate FROM {Tables.CompetitionVersion} WHERE CompetitionId = @CompetitionId AND UntilDate IS NULL", auditable).ConfigureAwait(false);

                Assert.NotEqual(currentVersionId, versionResult.competitionVersionId);
                Assert.Equal(auditable.CompetitionName, versionResult.competitionName);
                Assert.Equal(auditable.ComparableName(), versionResult.comparableName);
                Assert.Equal(DateTime.UtcNow.Date, versionResult.fromDate?.Date);

            }
        }

        [Fact]
        public async Task Update_competition_does_not_add_competition_version_if_name_is_unchanged()
        {
            var competition = _databaseFixture.TestData.Competitions.First(x => !x.UntilYear.HasValue);

            var auditable = new Competition
            {
                CompetitionId = competition.CompetitionId,
                CompetitionName = competition.CompetitionName,
            };

            int? existingVersions = null;
            (Guid? competitionVersionId, string competitionName, string comparableName, DateTimeOffset? fromDate) currentVersion;
            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                existingVersions = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.CompetitionVersion} WHERE CompetitionId = @CompetitionId", auditable).ConfigureAwait(false);
                currentVersion = await connection.QuerySingleAsync<(Guid competitionVersionId, string competitionName, string comparableName, DateTimeOffset? fromDate)>(
                    $"SELECT CompetitionVersionId, CompetitionName, ComparableName, FromDate FROM {Tables.CompetitionVersion} WHERE CompetitionId = @CompetitionId AND UntilDate IS NULL", auditable).ConfigureAwait(false);
            }

            _routeGenerator.Setup(x => x.GenerateUniqueRoute(competition.CompetitionRoute, "/competitions", auditable.CompetitionName, NoiseWords.CompetitionRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(competition.CompetitionRoute));

            _copier.Setup(x => x.CreateAuditableCopy(competition)).Returns(auditable);

            var repo = CreateRepository();

            var updated = await repo.UpdateCompetition(competition, Guid.NewGuid(), "Person 1").ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var totalVersions = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.CompetitionVersion} WHERE CompetitionId = @CompetitionId", auditable).ConfigureAwait(false);
                Assert.Equal(existingVersions, totalVersions);

                var versionResult = await connection.QuerySingleAsync<(Guid competitionVersionId, string competitionName, string comparableName, DateTimeOffset? fromDate)>(
                    $"SELECT CompetitionVersionId, CompetitionName, ComparableName, FromDate FROM {Tables.CompetitionVersion} WHERE CompetitionId = @CompetitionId AND UntilDate IS NULL", auditable).ConfigureAwait(false);

                Assert.Equal(currentVersion.competitionVersionId, versionResult.competitionVersionId);
                Assert.Equal(auditable.CompetitionName, versionResult.competitionName);
                Assert.Equal(auditable.ComparableName(), versionResult.comparableName);
                Assert.Equal(currentVersion.fromDate, versionResult.fromDate);
            }
        }

        [Fact]
        public async Task Update_competition_updates_route_and_inserts_redirect()
        {

            var competition = _databaseFixture.TestData.Competitions.First();
            var auditable = new Competition
            {
                CompetitionId = competition.CompetitionId,
                CompetitionName = competition.CompetitionName,
                CompetitionRoute = competition.CompetitionRoute
            };

            var updatedRoute = competition.CompetitionRoute + "-updated";
            _routeGenerator.Setup(x => x.GenerateUniqueRoute(competition.CompetitionRoute, "/competitions", auditable.CompetitionName, NoiseWords.CompetitionRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(updatedRoute));
            _copier.Setup(x => x.CreateAuditableCopy(competition)).Returns(auditable);

            var repo = CreateRepository();

            var updated = await repo.UpdateCompetition(competition, Guid.NewGuid(), "Person 1").ConfigureAwait(false);

            _routeGenerator.Verify(x => x.GenerateUniqueRoute(competition.CompetitionRoute, "/competitions", auditable.CompetitionName, NoiseWords.CompetitionRoute, It.IsAny<Func<string, Task<int>>>()), Times.Once);
            _redirectsRepository.Verify(x => x.InsertRedirect(competition.CompetitionRoute, updatedRoute, null, It.IsAny<IDbTransaction>()), Times.Once);
        }

        [Fact]
        public async Task Update_competition_updates_season_routes_and_inserts_redirects()
        {
            var competition = _databaseFixture.TestData.Competitions.First(x => x.Seasons.Count > 1);
            var auditable = new Competition
            {
                CompetitionId = competition.CompetitionId,
                CompetitionName = competition.CompetitionName,
                CompetitionRoute = competition.CompetitionRoute
            };

            var updatedRoute = competition.CompetitionRoute + "-updated";
            _routeGenerator.Setup(x => x.GenerateUniqueRoute(competition.CompetitionRoute, "/competitions", auditable.CompetitionName, NoiseWords.CompetitionRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(updatedRoute));
            _copier.Setup(x => x.CreateAuditableCopy(competition)).Returns(auditable);

            var repo = CreateRepository();

            var updated = await repo.UpdateCompetition(competition, Guid.NewGuid(), "Person 1").ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var seasonRoutes = await connection.QueryAsync<string>($"SELECT SeasonRoute FROM {Tables.Season} WHERE CompetitionId = @CompetitionId", competition).ConfigureAwait(false);
                foreach (var route in seasonRoutes)
                {
                    Assert.Matches("^" + updatedRoute.Replace("/", @"\/") + @"\/[0-9]{4}(-[0-9]{2,4})?$", route);

                    _redirectsRepository.Verify(x => x.InsertRedirect(competition.CompetitionRoute + route.Substring(route.LastIndexOf("/", StringComparison.OrdinalIgnoreCase)), route, null, It.IsAny<IDbTransaction>()), Times.Once);
                }
            }
        }

        [Fact]
        public async Task Update_competition_does_not_redirect_unchanged_route()
        {

            var competition = _databaseFixture.TestData.Competitions.First();

            var auditable = new Competition
            {
                CompetitionId = competition.CompetitionId,
                CompetitionName = competition.CompetitionName,
                CompetitionRoute = competition.CompetitionRoute
            };

            _routeGenerator.Setup(x => x.GenerateUniqueRoute(auditable.CompetitionRoute, "/competitions", auditable.CompetitionName, NoiseWords.CompetitionRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(competition.CompetitionRoute));
            _copier.Setup(x => x.CreateAuditableCopy(competition)).Returns(auditable);

            var repo = CreateRepository();

            var updated = await repo.UpdateCompetition(competition, Guid.NewGuid(), "Person 1").ConfigureAwait(false);

            _redirectsRepository.Verify(x => x.InsertRedirect(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDbTransaction>()), Times.Never);
        }

        [Fact]
        public async Task Update_competition_audits_and_logs()
        {
            var location = _databaseFixture.TestData.Competitions.First();
            var auditable = new Competition
            {
                CompetitionId = location.CompetitionId,
                CompetitionRoute = location.CompetitionRoute,
                CompetitionName = location.CompetitionName,
            };
            var redacted = new Competition
            {
                CompetitionId = location.CompetitionId,
                CompetitionRoute = location.CompetitionRoute,
                CompetitionName = location.CompetitionName
            };

            _routeGenerator.Setup(x => x.GenerateUniqueRoute(location.CompetitionRoute, "/competitions", auditable.CompetitionName, NoiseWords.CompetitionRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(location.CompetitionRoute));
            _copier.Setup(x => x.CreateAuditableCopy(location)).Returns(auditable);
            _copier.Setup(x => x.CreateRedactedCopy(auditable)).Returns(redacted);

            var repo = CreateRepository();
            var memberKey = Guid.NewGuid();
            var memberName = "Person 1";

            var updated = await repo.UpdateCompetition(location, memberKey, memberName).ConfigureAwait(false);

            _copier.Verify(x => x.CreateRedactedCopy(auditable), Times.Once);
            _auditRepository.Verify(x => x.CreateAudit(It.IsAny<AuditRecord>(), It.IsAny<IDbTransaction>()), Times.Once);
            _logger.Verify(x => x.Info(LoggingTemplates.Updated, redacted, memberName, memberKey, typeof(SqlServerCompetitionRepository), nameof(SqlServerCompetitionRepository.UpdateCompetition)));
        }

        [Fact]
        public async Task Delete_competition_succeeds()
        {
            var sanitizer = new Mock<Ganss.XSS.IHtmlSanitizer>();
            sanitizer.Setup(x => x.AllowedTags).Returns(new HashSet<string>());
            sanitizer.Setup(x => x.AllowedAttributes).Returns(new HashSet<string>());
            sanitizer.Setup(x => x.AllowedCssProperties).Returns(new HashSet<string>());
            sanitizer.Setup(x => x.AllowedAtRules).Returns(new HashSet<CssRuleType>());

            var memberKey = Guid.NewGuid();
            var memberName = "Dee Leeter";

            _copier.Setup(x => x.CreateAuditableCopy(_databaseFixture.TestData.CompetitionWithFullDetails)).Returns(new Competition
            {
                CompetitionId = _databaseFixture.TestData.CompetitionWithFullDetails!.CompetitionId,
                Seasons = _databaseFixture.TestData.CompetitionWithFullDetails.Seasons.Select(x => new Season { SeasonId = x.SeasonId }).ToList()
            });
            foreach (var season in _databaseFixture.TestData.CompetitionWithFullDetails.Seasons)
            {
                _copier.Setup(x => x.CreateAuditableCopy(season)).Returns(new Season { SeasonId = season.SeasonId });
            }

            var seasonRepository = new SqlServerSeasonRepository(
                _databaseFixture.ConnectionFactory,
                Mock.Of<IAuditRepository>(),
                Mock.Of<ILogger<SqlServerSeasonRepository>>(),
                sanitizer.Object,
                Mock.Of<IRedirectsRepository>(),
                _copier.Object
                );

            var competitionRepository = CreateRepository(seasonRepository);

            await competitionRepository.DeleteCompetition(_databaseFixture.TestData.CompetitionWithFullDetails, memberKey, memberName).ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<Guid?>($"SELECT CompetitionId FROM {Tables.Competition} WHERE CompetitionId = @CompetitionId", new { _databaseFixture.TestData.CompetitionWithFullDetails.CompetitionId }).ConfigureAwait(false);
                Assert.Null(result);
            }
        }
        public void Dispose() => _scope.Dispose();
    }
}
