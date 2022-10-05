using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using AngleSharp.Css.Dom;
using Ganss.XSS;
using Moq;
using Stoolball.Logging;
using Stoolball.Matches;
using Stoolball.Routing;
using Stoolball.Teams;
using Xunit;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer.UnitTests
{
    public class SqlServerTournamentRepositoryUnitTests
    {
        private readonly Mock<IDatabaseConnectionFactory> _connectionFactory = new();
        private readonly Mock<IDbConnection> _databaseConnection = new();
        private readonly Mock<IDbTransaction> _databaseTransaction = new();
        private readonly Mock<IDapperWrapper> _dapperWrapper = new();
        private readonly Mock<IAuditRepository> _auditRepository = new();
        private readonly Mock<ILogger<SqlServerTournamentRepository>> _logger = new();
        private readonly Mock<IRouteGenerator> _routeGenerator = new();
        private readonly Mock<IRedirectsRepository> _redirectsRepository = new();
        private readonly Mock<ITeamRepository> _teamRepository = new();
        private readonly Mock<IMatchRepository> _matchRepository = new();
        private readonly Mock<IHtmlSanitizer> _htmlSanitizer = new();
        private readonly Mock<IStoolballEntityCopier> _copier = new();

        public SqlServerTournamentRepositoryUnitTests()
        {
            _connectionFactory.Setup(x => x.CreateDatabaseConnection()).Returns(_databaseConnection.Object);
            _databaseConnection.Setup(x => x.BeginTransaction()).Returns(_databaseTransaction.Object);

            _htmlSanitizer.Setup(x => x.AllowedTags).Returns(new HashSet<string>());
            _htmlSanitizer.Setup(x => x.AllowedAttributes).Returns(new HashSet<string>());
            _htmlSanitizer.Setup(x => x.AllowedCssProperties).Returns(new HashSet<string>());
            _htmlSanitizer.Setup(x => x.AllowedAtRules).Returns(new HashSet<CssRuleType>());
        }

        private SqlServerTournamentRepository CreateRepository()
        {
            return new SqlServerTournamentRepository(
                _connectionFactory.Object,
                _dapperWrapper.Object,
                _auditRepository.Object,
                _logger.Object,
                _routeGenerator.Object,
                _redirectsRepository.Object,
                _teamRepository.Object,
                _matchRepository.Object,
                _htmlSanitizer.Object,
                _copier.Object);
        }

#nullable disable
        [Fact]
        public async Task Create_tournament_throws_ArgumentNullException_if_tournament_is_null()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(() => repo.CreateTournament(null, Guid.NewGuid(), "Member name")).ConfigureAwait(false);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task Create_tournament_throws_ArgumentException_if_member_name_is_missing(string memberName)
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(() => repo.CreateTournament(new Tournament(), Guid.NewGuid(), memberName)).ConfigureAwait(false);
        }
#nullable enable

        [Fact]
        public async Task Create_tournament_audits_and_logs()
        {
            var memberKey = Guid.NewGuid();
            var memberName = "Member name";

            var original = new Tournament
            {
                TournamentId = Guid.NewGuid(),
                TournamentName = "Example tournament",
                StartTime = DateTime.Now.AddDays(1),
                TournamentRoute = "/tournaments/example-tournament",
                MemberKey = memberKey
            };

            var auditable = new Tournament
            {
                TournamentId = Guid.NewGuid(),
                TournamentName = original.TournamentName,
                StartTime = original.StartTime,
                TournamentRoute = original.TournamentRoute,
                MemberKey = memberKey
            };

            var redacted = new Tournament
            {
                TournamentId = Guid.NewGuid(),
                TournamentName = original.TournamentName,
                StartTime = original.StartTime,
                TournamentRoute = original.TournamentRoute,
                MemberKey = memberKey
            };

            _copier.Setup(x => x.CreateAuditableCopy(original)).Returns(auditable);
            _copier.Setup(x => x.CreateRedactedCopy(auditable)).Returns(redacted);

            var repo = CreateRepository();

            var createdTournament = await repo.CreateTournament(original, memberKey, memberName);

            _auditRepository.Verify(x => x.CreateAudit(It.Is<AuditRecord>(audit =>
                                                                            audit.Action == AuditAction.Create &&
                                                                            audit.MemberKey == memberKey &&
                                                                            audit.ActorName == memberName &&
                                                                            audit.EntityUri == auditable.EntityUri &&
                                                                            audit.State.Contains(auditable.TournamentId.Value.ToString()) &&
                                                                            audit.RedactedState.Contains(redacted.TournamentId.Value.ToString()) &&
                                                                            audit.AuditDate.Date == DateTime.UtcNow.Date
                                                                         ), _databaseTransaction.Object), Times.Once);

            _logger.Verify(x => x.Info(LoggingTemplates.Created, redacted, memberName, memberKey, typeof(SqlServerTournamentRepository), nameof(SqlServerTournamentRepository.CreateTournament)), Times.Once);

        }
    }
}
