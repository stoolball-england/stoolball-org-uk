using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AngleSharp.Dom.Css;
using Dapper;
using Ganss.XSS;
using Moq;
using Stoolball.Logging;
using Stoolball.Routing;
using Stoolball.Security;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Competitions
{
    [Collection(IntegrationTestConstants.RepositoryIntegrationTestCollection)]
    public class SqlServerCompetitionRepositoryTests
    {
        private readonly SqlServerRepositoryFixture _databaseFixture;

        public SqlServerCompetitionRepositoryTests(SqlServerRepositoryFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Delete_competition_succeeds()
        {
            var sanitizer = new Mock<IHtmlSanitizer>();
            sanitizer.Setup(x => x.AllowedTags).Returns(new HashSet<string>());
            sanitizer.Setup(x => x.AllowedAttributes).Returns(new HashSet<string>());
            sanitizer.Setup(x => x.AllowedCssProperties).Returns(new HashSet<string>());
            sanitizer.Setup(x => x.AllowedAtRules).Returns(new HashSet<CssRuleType>());

            var memberKey = Guid.NewGuid();
            var memberName = "Dee Leeter";

            var seasonRepository = new SqlServerSeasonRepository(
                _databaseFixture.ConnectionFactory,
                Mock.Of<IAuditRepository>(),
                Mock.Of<ILogger>(),
                sanitizer.Object,
                Mock.Of<IRedirectsRepository>(),
                Mock.Of<IDataRedactor>()
                );

            var competitionRepository = new SqlServerCompetitionRepository(
                _databaseFixture.ConnectionFactory,
                Mock.Of<IAuditRepository>(),
                Mock.Of<ILogger>(),
                seasonRepository,
                Mock.Of<IRouteGenerator>(),
                Mock.Of<IRedirectsRepository>(),
                sanitizer.Object,
                Mock.Of<IDataRedactor>());

            await competitionRepository.DeleteCompetition(_databaseFixture.CompetitionWithFullDetailsForDelete, memberKey, memberName).ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<Guid?>($"SELECT CompetitionId FROM {Tables.Competition} WHERE CompetitionId = @CompetitionId", _databaseFixture.CompetitionWithFullDetailsForDelete).ConfigureAwait(false);
                Assert.Null(result);
            }
        }
    }
}
