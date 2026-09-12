

using System.Transactions;
using Stoolball.Data.Abstractions;
using Stoolball.Html;
using Stoolball.Routing;
using Stoolball.Security;
using Stoolball.Statistics;

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
        protected Mock<IHtmlSanitizer> HtmlSanitizer { get; init; } = new();
        protected Mock<IMemberGroupHelper> MemberGroupHelper { get; init; } = new();
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
            MemberGroupHelper.Setup(x => x.CreateOrFindGroup("team", It.IsAny<string>(), NoiseWords.TeamRoute)).Returns(new SecurityGroup { Key = Guid.NewGuid(), Name = "Group name" });
        }

        private SqlServerTournamentRepository CreateRepository()
        {
            var dapperWrapper = new DapperWrapper();

            var matchNameBuilder = new Mock<IMatchNameBuilder>();
            matchNameBuilder.Setup(x => x.BuildMatchName(It.IsAny<Match>())).Returns($"Match {Guid.NewGuid()}");

            var playerTypeSelector = new Mock<IPlayerTypeSelector>();
            playerTypeSelector.Setup(x => x.SelectPlayerType(It.Is<Match>(m => m.Tournament != null))).Returns((Match m) => m.Tournament!.PlayerType);

            return new SqlServerTournamentRepository(
                DatabaseFixture.ConnectionFactory,
                dapperWrapper,
                AuditRepository.Object,
                Logger.Object,
                RouteGenerator.Object,
                RedirectsRepository.Object,
                new SqlServerTeamRepository(
                    DatabaseFixture.ConnectionFactory,
                    AuditRepository.Object,
                    Mock.Of<ILogger<SqlServerTeamRepository>>(),
                    RouteGenerator.Object,
                    RedirectsRepository.Object,
                    MemberGroupHelper.Object,
                    HtmlSanitizer.Object,
                    Copier,
                    Mock.Of<IUrlFormatter>(),
                    Mock.Of<ISocialMediaAccountFormatter>()),
                new SqlServerMatchRepository(
                    DatabaseFixture.ConnectionFactory,
                    dapperWrapper,
                    AuditRepository.Object,
                    Mock.Of<ILogger<SqlServerMatchRepository>>(),
                    RouteGenerator.Object,
                    RedirectsRepository.Object,
                    HtmlSanitizer.Object,
                    matchNameBuilder.Object,
                    playerTypeSelector.Object,
                    Mock.Of<IBowlingScorecardComparer>(),
                    Mock.Of<IBattingScorecardComparer>(),
                    Mock.Of<IPlayerRepository>(),
                    Mock.Of<IDataRedactor>(),
                    Mock.Of<IStatisticsRepository>(),
                    Mock.Of<IOversHelper>(),
                    Mock.Of<IPlayerInMatchStatisticsBuilder>(),
                    Mock.Of<IMatchInningsFactory>(),
                    Mock.Of<ISeasonDataSource>(),
                    Copier
                    ),
                HtmlSanitizer.Object,
                Copier);
        }
        public void Dispose() => Scope.Dispose();
    }
}