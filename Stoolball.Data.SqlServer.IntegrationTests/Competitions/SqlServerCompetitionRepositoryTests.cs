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

        public SqlServerCompetitionRepositoryTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
            _scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        }

        [Fact]
        public async Task Create_competition_throws_ArgumentNullException_if_competition_is_null()
        {
            var repo = new SqlServerCompetitionRepository(
                _databaseFixture.ConnectionFactory,
                Mock.Of<IAuditRepository>(),
                Mock.Of<ILogger>(),
                Mock.Of<ISeasonRepository>(),
                Mock.Of<IRouteGenerator>(),
                Mock.Of<IRedirectsRepository>(),
                Mock.Of<IHtmlSanitizer>(),
                Mock.Of<IStoolballEntityCopier>(),
                Mock.Of<IUrlFormatter>(),
                Mock.Of<ISocialMediaAccountFormatter>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.CreateCompetition(null, Guid.NewGuid(), "Member name").ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Create_competition_throws_ArgumentNullException_if_memberName_is_null()
        {
            var repo = new SqlServerCompetitionRepository(
               _databaseFixture.ConnectionFactory,
               Mock.Of<IAuditRepository>(),
               Mock.Of<ILogger>(),
               Mock.Of<ISeasonRepository>(),
               Mock.Of<IRouteGenerator>(),
               Mock.Of<IRedirectsRepository>(),
               Mock.Of<IHtmlSanitizer>(),
               Mock.Of<IStoolballEntityCopier>(),
               Mock.Of<IUrlFormatter>(),
               Mock.Of<ISocialMediaAccountFormatter>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.CreateCompetition(new Competition(), Guid.NewGuid(), null).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Create_competition_throws_ArgumentNullException_if_memberName_is_empty_string()
        {
            var repo = new SqlServerCompetitionRepository(
                  _databaseFixture.ConnectionFactory,
                  Mock.Of<IAuditRepository>(),
                  Mock.Of<ILogger>(),
                  Mock.Of<ISeasonRepository>(),
                  Mock.Of<IRouteGenerator>(),
                  Mock.Of<IRedirectsRepository>(),
                  Mock.Of<IHtmlSanitizer>(),
                  Mock.Of<IStoolballEntityCopier>(),
                  Mock.Of<IUrlFormatter>(),
                  Mock.Of<ISocialMediaAccountFormatter>());

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
            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute("/competitions", competition.CompetitionName, NoiseWords.CompetitionRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(route));

            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(competition)).Returns(competition);

            var repo = new SqlServerCompetitionRepository(
                            _databaseFixture.ConnectionFactory,
                            Mock.Of<IAuditRepository>(),
                            Mock.Of<ILogger>(),
                            Mock.Of<ISeasonRepository>(),
                            routeGenerator.Object,
                            Mock.Of<IRedirectsRepository>(),
                            Mock.Of<IHtmlSanitizer>(),
                            copier.Object,
                            Mock.Of<IUrlFormatter>(),
                            Mock.Of<ISocialMediaAccountFormatter>());
            var created = await repo.CreateCompetition(competition, Guid.NewGuid(), "Member name").ConfigureAwait(false);

            routeGenerator.Verify(x => x.GenerateUniqueRoute("/competitions", competition.CompetitionName, NoiseWords.CompetitionRoute, It.IsAny<Func<string, Task<int>>>()), Times.Once);

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
                Assert.Equal(DateTime.UtcNow.Date, versionResult.fromDate.Value.Date);
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
            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute("/competitions", auditable.CompetitionName, NoiseWords.CompetitionRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(route));

            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(competition)).Returns(auditable);

            var sanitizer = new Mock<IHtmlSanitizer>();
            sanitizer.Setup(x => x.Sanitize(auditable.PrivateContactDetails)).Returns(sanitisedPrivateContact);
            sanitizer.Setup(x => x.Sanitize(auditable.PublicContactDetails)).Returns(sanitisedPublicContact);
            sanitizer.Setup(x => x.Sanitize(auditable.Introduction)).Returns(sanitisedIntro);

            var urlFormatter = new Mock<IUrlFormatter>();
            urlFormatter.Setup(x => x.PrefixHttpsProtocol(auditable.Facebook)).Returns(new Uri("https://" + auditable.Facebook));
            urlFormatter.Setup(x => x.PrefixHttpsProtocol(auditable.YouTube)).Returns(new Uri("https://" + auditable.YouTube));
            urlFormatter.Setup(x => x.PrefixHttpsProtocol(auditable.Website)).Returns(new Uri("https://" + auditable.Website));

            var socialMediaFormatter = new Mock<ISocialMediaAccountFormatter>();
            socialMediaFormatter.Setup(x => x.PrefixAtSign(auditable.Instagram)).Returns("@" + auditable.Instagram);
            socialMediaFormatter.Setup(x => x.PrefixAtSign(auditable.Twitter)).Returns("@" + auditable.Twitter);

            var repo = new SqlServerCompetitionRepository(
                         _databaseFixture.ConnectionFactory,
                         Mock.Of<IAuditRepository>(),
                         Mock.Of<ILogger>(),
                         Mock.Of<ISeasonRepository>(),
                         routeGenerator.Object,
                         Mock.Of<IRedirectsRepository>(),
                         sanitizer.Object,
                         copier.Object,
                         urlFormatter.Object,
                         socialMediaFormatter.Object);

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

                sanitizer.Verify(x => x.Sanitize(originalIntro));
                Assert.Equal(sanitisedIntro, competitionResult.Introduction);
                Assert.Equal(competition.FromYear, competitionResult.FromYear);
                Assert.Equal(competition.UntilYear, competitionResult.UntilYear);
                Assert.Equal(competition.PlayerType, competitionResult.PlayerType);
                urlFormatter.Verify(x => x.PrefixHttpsProtocol(competition.Facebook), Times.Once);
                Assert.Equal("https://" + competition.Facebook, competitionResult.Facebook);
                socialMediaFormatter.Verify(x => x.PrefixAtSign(competition.Instagram), Times.Once);
                Assert.Equal("@" + competition.Instagram, competitionResult.Instagram);
                urlFormatter.Verify(x => x.PrefixHttpsProtocol(competition.YouTube), Times.Once);
                Assert.Equal("https://" + competition.YouTube, competitionResult.YouTube);
                socialMediaFormatter.Verify(x => x.PrefixAtSign(competition.Twitter), Times.Once);
                Assert.Equal("@" + competition.Twitter, competitionResult.Twitter);
                urlFormatter.Verify(x => x.PrefixHttpsProtocol(competition.Website), Times.Once);
                Assert.Equal("https://" + competition.Website, competitionResult.Website);
                sanitizer.Verify(x => x.Sanitize(originalPrivateContact));
                Assert.Equal(sanitisedPrivateContact, competitionResult.PrivateContactDetails);
                sanitizer.Verify(x => x.Sanitize(originalPublicContact));
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

            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute("/competitions", copyCompetition.CompetitionName, NoiseWords.CompetitionRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult("/competitions/" + Guid.NewGuid()));

            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(competition)).Returns(copyCompetition);

            var repo = new SqlServerCompetitionRepository(
                            _databaseFixture.ConnectionFactory,
                            Mock.Of<IAuditRepository>(),
                            Mock.Of<ILogger>(),
                            Mock.Of<ISeasonRepository>(),
                            routeGenerator.Object,
                            Mock.Of<IRedirectsRepository>(),
                            Mock.Of<IHtmlSanitizer>(),
                            copier.Object,
                            Mock.Of<IUrlFormatter>(),
                            Mock.Of<ISocialMediaAccountFormatter>());

            var created = await repo.CreateCompetition(competition, Guid.NewGuid(), "Member name").ConfigureAwait(false);

            copier.Verify(x => x.CreateAuditableCopy(competition), Times.Once);
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
            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute("/competitions", auditable.CompetitionName, NoiseWords.CompetitionRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(route));
            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(location)).Returns(auditable);
            copier.Setup(x => x.CreateRedactedCopy(auditable)).Returns(redacted);
            var auditRepository = new Mock<IAuditRepository>();
            var logger = new Mock<ILogger>();

            var repo = new SqlServerCompetitionRepository(
                        _databaseFixture.ConnectionFactory,
                        auditRepository.Object,
                        logger.Object,
                        Mock.Of<ISeasonRepository>(),
                        routeGenerator.Object,
                        Mock.Of<IRedirectsRepository>(),
                        Mock.Of<IHtmlSanitizer>(),
                        copier.Object,
                        Mock.Of<IUrlFormatter>(),
                        Mock.Of<ISocialMediaAccountFormatter>());
            var memberKey = Guid.NewGuid();
            var memberName = "Person 1";

            var created = await repo.CreateCompetition(location, memberKey, memberName).ConfigureAwait(false);

            copier.Verify(x => x.CreateRedactedCopy(auditable), Times.Once);
            auditRepository.Verify(x => x.CreateAudit(It.IsAny<AuditRecord>(), It.IsAny<IDbTransaction>()), Times.Once);
            logger.Verify(x => x.Info(typeof(SqlServerCompetitionRepository), LoggingTemplates.Created, redacted, memberName, memberKey, typeof(SqlServerCompetitionRepository), nameof(SqlServerCompetitionRepository.CreateCompetition)));
        }


        [Fact]
        public async Task Update_competition_throws_ArgumentNullException_if_competition_is_null()
        {
            var repo = new SqlServerCompetitionRepository(
                        _databaseFixture.ConnectionFactory,
                        Mock.Of<IAuditRepository>(),
                        Mock.Of<ILogger>(),
                        Mock.Of<ISeasonRepository>(),
                        Mock.Of<IRouteGenerator>(),
                        Mock.Of<IRedirectsRepository>(),
                        Mock.Of<IHtmlSanitizer>(),
                        Mock.Of<IStoolballEntityCopier>(),
                        Mock.Of<IUrlFormatter>(),
                        Mock.Of<ISocialMediaAccountFormatter>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.UpdateCompetition(null, Guid.NewGuid(), "Member name").ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Update_competition_throws_ArgumentNullException_if_memberName_is_null()
        {
            var repo = new SqlServerCompetitionRepository(
                        _databaseFixture.ConnectionFactory,
                        Mock.Of<IAuditRepository>(),
                        Mock.Of<ILogger>(),
                        Mock.Of<ISeasonRepository>(),
                        Mock.Of<IRouteGenerator>(),
                        Mock.Of<IRedirectsRepository>(),
                        Mock.Of<IHtmlSanitizer>(),
                        Mock.Of<IStoolballEntityCopier>(),
                        Mock.Of<IUrlFormatter>(),
                        Mock.Of<ISocialMediaAccountFormatter>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.UpdateCompetition(new Competition(), Guid.NewGuid(), null).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Update_competition_throws_ArgumentNullException_if_memberName_is_empty_string()
        {
            var repo = new SqlServerCompetitionRepository(
                          _databaseFixture.ConnectionFactory,
                          Mock.Of<IAuditRepository>(),
                          Mock.Of<ILogger>(),
                          Mock.Of<ISeasonRepository>(),
                          Mock.Of<IRouteGenerator>(),
                          Mock.Of<IRedirectsRepository>(),
                          Mock.Of<IHtmlSanitizer>(),
                          Mock.Of<IStoolballEntityCopier>(),
                          Mock.Of<IUrlFormatter>(),
                          Mock.Of<ISocialMediaAccountFormatter>());

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

            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(competition.CompetitionRoute, "/competitions", copyCompetition.CompetitionName, NoiseWords.CompetitionRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult("/competitions/" + Guid.NewGuid()));

            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(competition)).Returns(copyCompetition);

            var repo = new SqlServerCompetitionRepository(
                    _databaseFixture.ConnectionFactory,
                    Mock.Of<IAuditRepository>(),
                    Mock.Of<ILogger>(),
                    Mock.Of<ISeasonRepository>(),
                    routeGenerator.Object,
                    Mock.Of<IRedirectsRepository>(),
                    Mock.Of<IHtmlSanitizer>(),
                    copier.Object,
                    Mock.Of<IUrlFormatter>(),
                    Mock.Of<ISocialMediaAccountFormatter>());

            var updated = await repo.UpdateCompetition(competition, Guid.NewGuid(), "Member name").ConfigureAwait(false);

            copier.Verify(x => x.CreateAuditableCopy(competition), Times.Once);
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

            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(competition.CompetitionRoute, "/competitions", auditable.CompetitionName, NoiseWords.CompetitionRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(updatedRoute));

            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(competition)).Returns(auditable);

            var sanitizer = new Mock<IHtmlSanitizer>();
            sanitizer.Setup(x => x.Sanitize(auditable.PrivateContactDetails)).Returns(sanitisedPrivateContact);
            sanitizer.Setup(x => x.Sanitize(auditable.PublicContactDetails)).Returns(sanitisedPublicContact);
            sanitizer.Setup(x => x.Sanitize(auditable.Introduction)).Returns(sanitisedIntro);

            var urlFormatter = new Mock<IUrlFormatter>();
            urlFormatter.Setup(x => x.PrefixHttpsProtocol(auditable.Facebook)).Returns(new Uri("https://" + auditable.Facebook));
            urlFormatter.Setup(x => x.PrefixHttpsProtocol(auditable.YouTube)).Returns(new Uri("https://" + auditable.YouTube));
            urlFormatter.Setup(x => x.PrefixHttpsProtocol(auditable.Website)).Returns(new Uri("https://" + auditable.Website));

            var socialMediaFormatter = new Mock<ISocialMediaAccountFormatter>();
            socialMediaFormatter.Setup(x => x.PrefixAtSign(auditable.Instagram)).Returns("@" + auditable.Instagram);
            socialMediaFormatter.Setup(x => x.PrefixAtSign(auditable.Twitter)).Returns("@" + auditable.Twitter);

            var repo = new SqlServerCompetitionRepository(
                         _databaseFixture.ConnectionFactory,
                         Mock.Of<IAuditRepository>(),
                         Mock.Of<ILogger>(),
                         Mock.Of<ISeasonRepository>(),
                         routeGenerator.Object,
                         Mock.Of<IRedirectsRepository>(),
                         sanitizer.Object,
                         copier.Object,
                         urlFormatter.Object,
                         socialMediaFormatter.Object);

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
                sanitizer.Verify(x => x.Sanitize(originalIntro));
                Assert.Equal(sanitisedIntro, competitionResult.Introduction);
                urlFormatter.Verify(x => x.PrefixHttpsProtocol(originalFacebook), Times.Once);
                Assert.Equal(auditable.Facebook, competitionResult.Facebook);
                Assert.StartsWith("https://", auditable.Facebook);
                socialMediaFormatter.Verify(x => x.PrefixAtSign(originalInstagram), Times.Once);
                Assert.Equal(auditable.Instagram, competitionResult.Instagram);
                Assert.StartsWith("@", auditable.Instagram);
                urlFormatter.Verify(x => x.PrefixHttpsProtocol(originalYouTube), Times.Once);
                Assert.Equal(auditable.YouTube, competitionResult.YouTube);
                Assert.StartsWith("https://", auditable.YouTube);
                socialMediaFormatter.Verify(x => x.PrefixAtSign(originalTwitter), Times.Once);
                Assert.Equal(auditable.Twitter, competitionResult.Twitter);
                Assert.StartsWith("@", auditable.Twitter);
                urlFormatter.Verify(x => x.PrefixHttpsProtocol(originalWebsite), Times.Once);
                Assert.Equal(auditable.Website, competitionResult.Website);
                Assert.StartsWith("https://", auditable.Website);
                sanitizer.Verify(x => x.Sanitize(originalPrivateContact));
                Assert.Equal(sanitisedPrivateContact, competitionResult.PrivateContactDetails);
                sanitizer.Verify(x => x.Sanitize(originalPublicContact));
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

            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(competition.CompetitionRoute, "/competitions", auditable.CompetitionName, NoiseWords.CompetitionRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(competition.CompetitionRoute));

            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(competition)).Returns(auditable);

            var repo = new SqlServerCompetitionRepository(
                         _databaseFixture.ConnectionFactory,
                         Mock.Of<IAuditRepository>(),
                         Mock.Of<ILogger>(),
                         Mock.Of<ISeasonRepository>(),
                         routeGenerator.Object,
                         Mock.Of<IRedirectsRepository>(),
                         Mock.Of<IHtmlSanitizer>(),
                         copier.Object,
                         Mock.Of<IUrlFormatter>(),
                         Mock.Of<ISocialMediaAccountFormatter>());

            var updated = await repo.UpdateCompetition(competition, Guid.NewGuid(), "Person 1").ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var totalVersions = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.CompetitionVersion} WHERE CompetitionId = @CompetitionId", auditable).ConfigureAwait(false);
                Assert.Equal(existingVersions + 1, totalVersions);

                var versionResult = await connection.QuerySingleAsync<(Guid competitionVersionId, string competitionName, string comparableName, DateTimeOffset? fromDate)>(
                    $"SELECT CompetitionVersionId, CompetitionName, ComparableName, FromDate FROM {Tables.CompetitionVersion} WHERE CompetitionId = @CompetitionId AND UntilDate IS NULL", auditable).ConfigureAwait(false);

                Assert.NotEqual(currentVersionId, versionResult.competitionVersionId);
                Assert.Equal(auditable.CompetitionName, versionResult.competitionName);
                Assert.Equal(competition.ComparableName(), versionResult.comparableName);
                Assert.Equal(DateTime.UtcNow.Date, versionResult.fromDate.Value.Date);

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
            (Guid? competitionVersionId, string competitionName, string comparableName, DateTimeOffset? fromDate) currentVersion = (null, null, null, null);
            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                existingVersions = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.CompetitionVersion} WHERE CompetitionId = @CompetitionId", auditable).ConfigureAwait(false);
                currentVersion = await connection.QuerySingleAsync<(Guid competitionVersionId, string competitionName, string comparableName, DateTimeOffset? fromDate)>(
                    $"SELECT CompetitionVersionId, CompetitionName, ComparableName, FromDate FROM {Tables.CompetitionVersion} WHERE CompetitionId = @CompetitionId AND UntilDate IS NULL", auditable).ConfigureAwait(false);
            }

            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(competition.CompetitionRoute, "/competitions", auditable.CompetitionName, NoiseWords.CompetitionRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(competition.CompetitionRoute));

            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(competition)).Returns(auditable);

            var repo = new SqlServerCompetitionRepository(
                         _databaseFixture.ConnectionFactory,
                         Mock.Of<IAuditRepository>(),
                         Mock.Of<ILogger>(),
                         Mock.Of<ISeasonRepository>(),
                         routeGenerator.Object,
                         Mock.Of<IRedirectsRepository>(),
                         Mock.Of<IHtmlSanitizer>(),
                         copier.Object,
                         Mock.Of<IUrlFormatter>(),
                         Mock.Of<ISocialMediaAccountFormatter>());

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
                Assert.Equal(currentVersion.fromDate.Value, versionResult.fromDate.Value);
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
            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(competition.CompetitionRoute, "/competitions", auditable.CompetitionName, NoiseWords.CompetitionRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(updatedRoute));
            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(competition)).Returns(auditable);
            var redirects = new Mock<IRedirectsRepository>();

            var repo = new SqlServerCompetitionRepository(
                           _databaseFixture.ConnectionFactory,
                           Mock.Of<IAuditRepository>(),
                           Mock.Of<ILogger>(),
                           Mock.Of<ISeasonRepository>(),
                           routeGenerator.Object,
                           redirects.Object,
                           Mock.Of<IHtmlSanitizer>(),
                           copier.Object,
                           Mock.Of<IUrlFormatter>(),
                           Mock.Of<ISocialMediaAccountFormatter>());

            var updated = await repo.UpdateCompetition(competition, Guid.NewGuid(), "Person 1").ConfigureAwait(false);

            routeGenerator.Verify(x => x.GenerateUniqueRoute(competition.CompetitionRoute, "/competitions", auditable.CompetitionName, NoiseWords.CompetitionRoute, It.IsAny<Func<string, Task<int>>>()), Times.Once);
            redirects.Verify(x => x.InsertRedirect(competition.CompetitionRoute, updatedRoute, null, It.IsAny<IDbTransaction>()), Times.Once);
        }

        [Fact]
        public async Task Update_competition_updates_season_routes_and_inserts_redirects()
        {
            var competition = _databaseFixture.TestData.Competitions.First(x => x.Seasons.Count > 1);
            var auditable = new Competition
            {
                CompetitionId = competition.CompetitionId,
                CompetitionName = competition.CompetitionName,
                CompetitionRoute = competition.CompetitionRoute,
                Seasons = competition.Seasons.Select(x => new Season { SeasonId = x.SeasonId, SeasonRoute = x.SeasonRoute }).ToList()
            };

            var updatedRoute = competition.CompetitionRoute + "-updated";
            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(competition.CompetitionRoute, "/competitions", auditable.CompetitionName, NoiseWords.CompetitionRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(updatedRoute));
            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(competition)).Returns(auditable);
            var redirects = new Mock<IRedirectsRepository>();

            var repo = new SqlServerCompetitionRepository(
                           _databaseFixture.ConnectionFactory,
                           Mock.Of<IAuditRepository>(),
                           Mock.Of<ILogger>(),
                           Mock.Of<ISeasonRepository>(),
                           routeGenerator.Object,
                           redirects.Object,
                           Mock.Of<IHtmlSanitizer>(),
                           copier.Object,
                           Mock.Of<IUrlFormatter>(),
                           Mock.Of<ISocialMediaAccountFormatter>());

            var updated = await repo.UpdateCompetition(competition, Guid.NewGuid(), "Person 1").ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                foreach (var season in competition.Seasons)
                {
                    var updatedSeasonRoute = updatedRoute + season.SeasonRoute.Substring(competition.CompetitionRoute.Length);

                    var seasonRouteResult = await connection.ExecuteScalarAsync<string>($"SELECT SeasonRoute FROM {Tables.Season} WHERE SeasonId = @SeasonId", season).ConfigureAwait(false);
                    Assert.Equal(updatedSeasonRoute, seasonRouteResult);

                    redirects.Verify(x => x.InsertRedirect(season.SeasonRoute, updatedSeasonRoute, null, It.IsAny<IDbTransaction>()), Times.Once);
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

            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(auditable.CompetitionRoute, "/competitions", auditable.CompetitionName, NoiseWords.CompetitionRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(competition.CompetitionRoute));
            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(competition)).Returns(auditable);
            var redirects = new Mock<IRedirectsRepository>();

            var repo = new SqlServerCompetitionRepository(
                           _databaseFixture.ConnectionFactory,
                           Mock.Of<IAuditRepository>(),
                           Mock.Of<ILogger>(),
                           Mock.Of<ISeasonRepository>(),
                           routeGenerator.Object,
                           redirects.Object,
                           Mock.Of<IHtmlSanitizer>(),
                           copier.Object,
                           Mock.Of<IUrlFormatter>(),
                           Mock.Of<ISocialMediaAccountFormatter>());

            var updated = await repo.UpdateCompetition(competition, Guid.NewGuid(), "Person 1").ConfigureAwait(false);

            redirects.Verify(x => x.InsertRedirect(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDbTransaction>()), Times.Never);
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

            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(location.CompetitionRoute, "/competitions", auditable.CompetitionName, NoiseWords.CompetitionRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(location.CompetitionRoute));
            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(location)).Returns(auditable);
            copier.Setup(x => x.CreateRedactedCopy(auditable)).Returns(redacted);
            var auditRepository = new Mock<IAuditRepository>();
            var logger = new Mock<ILogger>();

            var repo = new SqlServerCompetitionRepository(
                    _databaseFixture.ConnectionFactory,
                    auditRepository.Object,
                    logger.Object,
                    Mock.Of<ISeasonRepository>(),
                    routeGenerator.Object,
                    Mock.Of<IRedirectsRepository>(),
                    Mock.Of<IHtmlSanitizer>(),
                    copier.Object,
                    Mock.Of<IUrlFormatter>(),
                    Mock.Of<ISocialMediaAccountFormatter>());
            var memberKey = Guid.NewGuid();
            var memberName = "Person 1";

            var updated = await repo.UpdateCompetition(location, memberKey, memberName).ConfigureAwait(false);

            copier.Verify(x => x.CreateRedactedCopy(auditable), Times.Once);
            auditRepository.Verify(x => x.CreateAudit(It.IsAny<AuditRecord>(), It.IsAny<IDbTransaction>()), Times.Once);
            logger.Verify(x => x.Info(typeof(SqlServerCompetitionRepository), LoggingTemplates.Updated, redacted, memberName, memberKey, typeof(SqlServerCompetitionRepository), nameof(SqlServerCompetitionRepository.UpdateCompetition)));
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

            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(_databaseFixture.TestData.CompetitionWithFullDetails)).Returns(new Competition
            {
                CompetitionId = _databaseFixture.TestData.CompetitionWithFullDetails.CompetitionId,
                Seasons = _databaseFixture.TestData.CompetitionWithFullDetails.Seasons.Select(x => new Season { SeasonId = x.SeasonId }).ToList()
            });
            foreach (var season in _databaseFixture.TestData.CompetitionWithFullDetails.Seasons)
            {
                copier.Setup(x => x.CreateAuditableCopy(season)).Returns(new Season { SeasonId = season.SeasonId });
            }

            var seasonRepository = new SqlServerSeasonRepository(
                _databaseFixture.ConnectionFactory,
                Mock.Of<IAuditRepository>(),
                Mock.Of<ILogger>(),
                sanitizer.Object,
                Mock.Of<IRedirectsRepository>(),
                copier.Object
                );

            var competitionRepository = new SqlServerCompetitionRepository(
                _databaseFixture.ConnectionFactory,
                Mock.Of<IAuditRepository>(),
                Mock.Of<ILogger>(),
                seasonRepository,
                Mock.Of<IRouteGenerator>(),
                Mock.Of<IRedirectsRepository>(),
                Mock.Of<IHtmlSanitizer>(),
                copier.Object,
                Mock.Of<IUrlFormatter>(),
                Mock.Of<ISocialMediaAccountFormatter>());

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
