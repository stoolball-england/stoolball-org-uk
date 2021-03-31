using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AngleSharp.Css.Dom;
using Dapper;
using Ganss.XSS;
using Moq;
using Stoolball.Logging;
using Stoolball.Matches;
using Stoolball.Routing;
using Stoolball.Security;
using Stoolball.Statistics;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Matches
{
    [Collection(IntegrationTestConstants.RepositoryIntegrationTestCollection)]
    public class SqlServerMatchRepositoryTests
    {
        private readonly SqlServerRepositoryFixture _databaseFixture;

        public SqlServerMatchRepositoryTests(SqlServerRepositoryFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Delete_match_succeeds()
        {
            var sanitizer = new Mock<IHtmlSanitizer>();
            sanitizer.Setup(x => x.AllowedTags).Returns(new HashSet<string>());
            sanitizer.Setup(x => x.AllowedAttributes).Returns(new HashSet<string>());
            sanitizer.Setup(x => x.AllowedCssProperties).Returns(new HashSet<string>());
            sanitizer.Setup(x => x.AllowedAtRules).Returns(new HashSet<CssRuleType>());

            var memberKey = Guid.NewGuid();
            var memberName = "Dee Leeter";

            var repo = new SqlServerMatchRepository(
                _databaseFixture.ConnectionFactory,
                Mock.Of<IAuditRepository>(),
                Mock.Of<ILogger>(),
                Mock.Of<IRouteGenerator>(),
                Mock.Of<IRedirectsRepository>(),
                sanitizer.Object,
                Mock.Of<IMatchNameBuilder>(),
                Mock.Of<IPlayerTypeSelector>(),
                Mock.Of<IBowlingScorecardComparer>(),
                Mock.Of<IBattingScorecardComparer>(),
                Mock.Of<IPlayerRepository>(),
                Mock.Of<IDataRedactor>(),
                Mock.Of<IStatisticsRepository>(),
                Mock.Of<IOversHelper>(),
                Mock.Of<IPlayerInMatchStatisticsBuilder>());

            await repo.DeleteMatch(_databaseFixture.MatchWithFullDetailsForDelete, memberKey, memberName).ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<Guid?>($"SELECT MatchId FROM {Tables.Match} WHERE MatchId = @MatchId", _databaseFixture.MatchWithFullDetailsForDelete).ConfigureAwait(false);
                Assert.Null(result);
            }
        }
    }
}
