using System.Transactions;
using AngleSharp.Css.Dom;
using Ganss.Xss;
using Stoolball.Data.Abstractions;
using Stoolball.Security;

namespace Stoolball.Data.SqlServer.IntegrationTests.Competitions.SeasonRepository
{
    [Collection("SqlServerTestData")]
    public class SqlServerSeasonRepositoryTestsBase : IDisposable
    {
        protected SqlServerTestDataFixture DatabaseFixture { get; private init; }
        protected SqlServerSeasonRepository Repository => CreateRepository();
        protected Mock<IAuditRepository> AuditRepository { get; init; } = new();
        protected Mock<ILogger<SqlServerSeasonRepository>> Logger { get; init; } = new();
        protected Mock<IHtmlSanitizer> HtmlSanitizer { get; init; } = new();

        protected IStoolballEntityCopier EntityCopier { get; init; } = new StoolballEntityCopier(new DataRedactor());

        private readonly TransactionScope _scope;
        protected Guid MemberKey { get; init; }
        protected string MemberName { get; init; }

        public SqlServerSeasonRepositoryTestsBase(SqlServerTestDataFixture databaseFixture)
        {
            DatabaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
            MemberKey = DatabaseFixture.TestData.Members[0].memberKey;
            MemberName = DatabaseFixture.TestData.Members[0].memberName;

            _scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        }

        public void Dispose() => _scope.Dispose();

        protected SqlServerSeasonRepository CreateRepository()
        {
            HtmlSanitizer.Setup(x => x.AllowedTags).Returns(new HashSet<string>());
            HtmlSanitizer.Setup(x => x.AllowedAttributes).Returns(new HashSet<string>());
            HtmlSanitizer.Setup(x => x.AllowedCssProperties).Returns(new HashSet<string>());
            HtmlSanitizer.Setup(x => x.AllowedAtRules).Returns(new HashSet<CssRuleType>());

            return new SqlServerSeasonRepository(
                DatabaseFixture.ConnectionFactory,
                AuditRepository.Object,
                Logger.Object,
                HtmlSanitizer.Object,
                Mock.Of<IRedirectsRepository>(),
                EntityCopier
                );
        }
    }
}