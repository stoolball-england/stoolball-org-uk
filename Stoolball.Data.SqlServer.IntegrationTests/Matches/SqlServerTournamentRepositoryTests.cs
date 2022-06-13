using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;
using AngleSharp.Css.Dom;
using Dapper;
using Ganss.XSS;
using Moq;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Logging;
using Stoolball.Matches;
using Stoolball.Routing;
using Stoolball.Teams;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Matches
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class SqlServerTournamentRepositoryTests : IDisposable
    {
        private readonly SqlServerTestDataFixture _databaseFixture;
        private readonly TransactionScope _scope;

        public SqlServerTournamentRepositoryTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
            _scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        }

        [Fact]
        public async Task Delete_tournament_succeeds()
        {
            var sanitizer = new Mock<IHtmlSanitizer>();
            sanitizer.Setup(x => x.AllowedTags).Returns(new HashSet<string>());
            sanitizer.Setup(x => x.AllowedAttributes).Returns(new HashSet<string>());
            sanitizer.Setup(x => x.AllowedCssProperties).Returns(new HashSet<string>());
            sanitizer.Setup(x => x.AllowedAtRules).Returns(new HashSet<CssRuleType>());

            var auditable = new Tournament { TournamentId = _databaseFixture.TestData.TournamentWithFullDetails.TournamentId };
            var redacted = new Tournament { TournamentId = _databaseFixture.TestData.TournamentWithFullDetails.TournamentId };

            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(_databaseFixture.TestData.TournamentWithFullDetails)).Returns(auditable);
            copier.Setup(x => x.CreateAuditableCopy(auditable)).Returns(redacted);

            var memberKey = Guid.NewGuid();
            var memberName = "Dee Leeter";

            var repo = new SqlServerTournamentRepository(
                _databaseFixture.ConnectionFactory,
                Mock.Of<IAuditRepository>(),
                Mock.Of<ILogger<SqlServerTournamentRepository>>(),
                Mock.Of<IRouteGenerator>(),
                Mock.Of<IRedirectsRepository>(),
                Mock.Of<ITeamRepository>(),
                Mock.Of<IMatchRepository>(),
                sanitizer.Object,
                copier.Object);

            await repo.DeleteTournament(_databaseFixture.TestData.TournamentWithFullDetails, memberKey, memberName).ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<Guid?>($"SELECT TournamentId FROM {Tables.Tournament} WHERE TournamentId = @TournamentId", new { _databaseFixture.TestData.TournamentWithFullDetails.TournamentId }).ConfigureAwait(false);
                Assert.Null(result);
            }
        }
        public void Dispose() => _scope.Dispose();
    }
}
