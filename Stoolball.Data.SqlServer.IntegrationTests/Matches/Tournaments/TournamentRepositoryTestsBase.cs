

using System.Transactions;
using AngleSharp.Css.Dom;
using Ganss.Xss;
using Stoolball.Data.Abstractions;
using Stoolball.Routing;
using Stoolball.Security;

namespace Stoolball.Data.SqlServer.IntegrationTests.Matches.Tournaments
{
    [Collection("SqlServerTestData")]
    public class TournamentRepositoryTestsBase : IDisposable
    {
        protected SqlServerTestDataFixture DatabaseFixture { get; init; }
        protected TransactionScope Scope { get; init; }
        protected SqlServerTournamentRepository Repository => CreateRepository();

        protected Mock<IAuditRepository> AuditRepository { get; init; } = new();
        protected Mock<ILogger<SqlServerTournamentRepository>> Logger { get; init; } = new();
        protected Mock<IRouteGenerator> RouteGenerator { get; init; } = new();
        protected Mock<IRedirectsRepository> RedirectsRepository { get; init; } = new();
        protected Mock<ITeamRepository> TeamRepository { get; init; } = new();
        protected Mock<IMatchRepository> MatchRepository { get; init; } = new();
        protected Mock<IHtmlSanitizer> HtmlSanitizer { get; init; } = new();
        protected StoolballEntityCopier Copier { get; init; } = new(new DataRedactor());

        protected Guid MemberKey { get; init; }
        protected string MemberUsername { get; init; }
        protected string MemberName { get; init; }

        public TournamentRepositoryTestsBase(SqlServerTestDataFixture databaseFixture)
        {
            DatabaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
            Scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            MemberKey = DatabaseFixture.TestData.Members[0].Key;
            MemberName = DatabaseFixture.TestData.Members[0].Name;
            MemberUsername = DatabaseFixture.TestData.Members[0].Username();

            HtmlSanitizer.Setup(x => x.AllowedTags).Returns(new HashSet<string>());
            HtmlSanitizer.Setup(x => x.AllowedAttributes).Returns(new HashSet<string>());
            HtmlSanitizer.Setup(x => x.AllowedCssProperties).Returns(new HashSet<string>());
            HtmlSanitizer.Setup(x => x.AllowedAtRules).Returns(new HashSet<CssRuleType>());
        }

        private SqlServerTournamentRepository CreateRepository()
        {
            return new SqlServerTournamentRepository(
                DatabaseFixture.ConnectionFactory,
                new DapperWrapper(),
                AuditRepository.Object,
                Logger.Object,
                RouteGenerator.Object,
                RedirectsRepository.Object,
                TeamRepository.Object,
                MatchRepository.Object,
                HtmlSanitizer.Object,
                Copier);
        }
        public void Dispose() => Scope.Dispose();
    }
}