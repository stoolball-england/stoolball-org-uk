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

namespace Stoolball.Data.SqlServer.IntegrationTests.Competitions
{
    [Collection(IntegrationTestConstants.RepositoryIntegrationTestCollection)]
    public class SqlServerSeasonRepositoryTests
    {
        private readonly SqlServerRepositoryFixture _databaseFixture;

        public SqlServerSeasonRepositoryTests(SqlServerRepositoryFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Delete_season_succeeds()
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

            await seasonRepository.DeleteSeason(_databaseFixture.SeasonWithFullDetailsForDelete, memberKey, memberName).ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<Guid?>($"SELECT SeasonId FROM {Tables.Season} WHERE SeasonId = @SeasonId", _databaseFixture.SeasonWithFullDetailsForDelete).ConfigureAwait(false);
                Assert.Null(result);
            }
        }
    }
}
