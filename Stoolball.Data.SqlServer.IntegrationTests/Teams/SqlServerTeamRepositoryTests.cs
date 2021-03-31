using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AngleSharp.Css.Dom;
using Dapper;
using Ganss.XSS;
using Moq;
using Stoolball.Logging;
using Stoolball.Routing;
using Stoolball.Security;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Teams
{
    [Collection(IntegrationTestConstants.RepositoryIntegrationTestCollection)]
    public class SqlServerTeamRepositoryTests
    {
        private readonly SqlServerRepositoryFixture _databaseFixture;

        public SqlServerTeamRepositoryTests(SqlServerRepositoryFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Delete_team_succeeds()
        {
            var sanitizer = new Mock<IHtmlSanitizer>();
            sanitizer.Setup(x => x.AllowedTags).Returns(new HashSet<string>());
            sanitizer.Setup(x => x.AllowedAttributes).Returns(new HashSet<string>());
            sanitizer.Setup(x => x.AllowedCssProperties).Returns(new HashSet<string>());
            sanitizer.Setup(x => x.AllowedAtRules).Returns(new HashSet<CssRuleType>());

            var memberKey = Guid.NewGuid();
            var memberName = "Dee Leeter";

            var repo = new SqlServerTeamRepository(
                _databaseFixture.ConnectionFactory,
                Mock.Of<IAuditRepository>(),
                Mock.Of<ILogger>(),
                Mock.Of<IRouteGenerator>(),
                Mock.Of<IRedirectsRepository>(),
                Mock.Of<IMemberGroupHelper>(),
                sanitizer.Object,
                Mock.Of<IDataRedactor>());

            await repo.DeleteTeam(_databaseFixture.TeamWithFullDetailsForDelete, memberKey, memberName).ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<Guid?>($"SELECT TeamId FROM {Tables.Team} WHERE TeamId = @TeamId", _databaseFixture.TeamWithFullDetailsForDelete).ConfigureAwait(false);
                Assert.Null(result);
            }
        }
    }
}
