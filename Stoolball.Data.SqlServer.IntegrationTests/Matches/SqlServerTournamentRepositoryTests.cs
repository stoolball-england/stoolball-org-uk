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
using Stoolball.Teams;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Matches
{
    [Collection(IntegrationTestConstants.RepositoryIntegrationTestCollection)]
    public class SqlServerTournamentRepositoryTests
    {
        private readonly SqlServerRepositoryFixture _databaseFixture;

        public SqlServerTournamentRepositoryTests(SqlServerRepositoryFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Delete_tournament_succeeds()
        {
            var sanitizer = new Mock<IHtmlSanitizer>();
            sanitizer.Setup(x => x.AllowedTags).Returns(new HashSet<string>());
            sanitizer.Setup(x => x.AllowedAttributes).Returns(new HashSet<string>());
            sanitizer.Setup(x => x.AllowedCssProperties).Returns(new HashSet<string>());
            sanitizer.Setup(x => x.AllowedAtRules).Returns(new HashSet<CssRuleType>());

            var memberKey = Guid.NewGuid();
            var memberName = "Dee Leeter";

            var repo = new SqlServerTournamentRepository(
                _databaseFixture.ConnectionFactory,
                Mock.Of<IAuditRepository>(),
                Mock.Of<ILogger>(),
                Mock.Of<IRouteGenerator>(),
                Mock.Of<IRedirectsRepository>(),
                Mock.Of<ITeamRepository>(),
                sanitizer.Object,
                Mock.Of<IDataRedactor>());

            await repo.DeleteTournament(_databaseFixture.TournamentWithFullDetailsForDelete, memberKey, memberName).ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<Guid?>($"SELECT TournamentId FROM {Tables.Tournament} WHERE TournamentId = @TournamentId", _databaseFixture.TournamentWithFullDetailsForDelete).ConfigureAwait(false);
                Assert.Null(result);
            }
        }
    }
}
