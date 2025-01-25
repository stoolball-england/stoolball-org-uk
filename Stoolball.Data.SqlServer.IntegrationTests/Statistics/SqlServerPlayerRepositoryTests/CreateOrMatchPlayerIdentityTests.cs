using System;
using System.Data;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Humanizer;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Logging;
using Stoolball.Routing;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Testing;
using Xunit;
using static Dapper.SqlMapper;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer.IntegrationTests.Statistics.SqlServerPlayerRepositoryTests
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class CreateOrMatchPlayerIdentityTests(SqlServerTestDataFixture _fixture) : IDisposable
    {
        #region Setup and dispose
        private readonly IDatabaseConnectionFactory _connectionFactory = _fixture.ConnectionFactory;
        private readonly TestData _testData = _fixture.TestData;
        private readonly TransactionScope _scope = new(TransactionScopeAsyncFlowOption.Enabled);
        private readonly Mock<IAuditRepository> _auditRepository = new();
        private readonly Mock<ILogger<SqlServerPlayerRepository>> _logger = new();
        private readonly Mock<IStoolballEntityCopier> _copier = new();
        private readonly Mock<IPlayerNameFormatter> _playerNameFormatter = new();
        private readonly Mock<IRouteGenerator> _routeGenerator = new();
        private readonly Mock<IBestRouteSelector> _routeSelector = new();
        private readonly Mock<IRedirectsRepository> _redirectsRepository = new();
        private readonly Mock<IPlayerCacheInvalidator> _playerCacheClearer = new();

        public void Dispose() => _scope.Dispose();

        private SqlServerPlayerRepository CreateRepository()
        {
            return new SqlServerPlayerRepository(_connectionFactory,
                new DapperWrapper(),
                _auditRepository.Object,
                _logger.Object,
                _redirectsRepository.Object,
                _routeGenerator.Object,
                _copier.Object,
                _playerNameFormatter.Object,
                _routeSelector.Object,
                _playerCacheClearer.Object);
        }

        #endregion

        [Fact]
        public async Task CreateOrMatchPlayerIdentity_returns_playerIdentity_if_playerIdentityId_and_playerId_are_present()
        {
            var playerIdentity = new PlayerIdentity
            {
                Player = new Player
                {
                    PlayerId = Guid.NewGuid()
                },
                PlayerIdentityId = Guid.NewGuid(),
                PlayerIdentityName = "Player 1",
                Team = new Team { TeamId = Guid.NewGuid() }
            };
            var repo = CreateRepository();
            var transaction = new Mock<IDbTransaction>();

            var result = await repo.CreateOrMatchPlayerIdentity(playerIdentity, Guid.NewGuid(), "Member name", transaction.Object);

            Assert.Equal(playerIdentity, result);
            transaction.Verify(x => x.Connection, Times.Never);
        }

        [Fact]
        public async Task CreateOrMatchPlayerIdentity_returns_matched_PlayerIdentity()
        {
            using (var connection = _connectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var dataForAnyPlayerIdentity = await connection.QuerySingleAsync<(Guid playerIdentityId, Guid playerId, string comparableName, Guid teamId)>(
                        $"SELECT TOP 1 PlayerIdentityId, PlayerId, ComparableName, TeamId FROM {Tables.PlayerIdentity}"
                    );
                    var playerIdentity = new PlayerIdentity { PlayerIdentityName = dataForAnyPlayerIdentity.comparableName, Team = new Team { TeamId = dataForAnyPlayerIdentity.teamId } };

                    var repo = CreateRepository();

                    var result = await repo.CreateOrMatchPlayerIdentity(playerIdentity, Guid.NewGuid(), "Member name", transaction);

                    Assert.NotNull(result);
                    Assert.Equal(dataForAnyPlayerIdentity.playerIdentityId, result.PlayerIdentityId);
                    Assert.Equal(dataForAnyPlayerIdentity.playerId, result.Player?.PlayerId);
                    Assert.Equal(dataForAnyPlayerIdentity.teamId, result.Team?.TeamId);
                    Assert.Equal(dataForAnyPlayerIdentity.comparableName, result.ComparableName());

                    transaction.Rollback();
                }
            }
        }

        private class PlayerIdentityResult
        {
            public Guid? PlayerId { get; set; }
            public string? PlayerIdentityName { get; set; }
            public string? ComparableName { get; set; }
            public string? RouteSegment { get; set; }
            public Guid? TeamId { get; set; }
            public PlayerIdentityLinkedBy? LinkedBy { get; set; }
        }

        [Fact]
        public async Task Unmatched_PlayerIdentity_returns_new_player_and_identity()
        {
            using (var connection = _connectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var playerIdentity = new PlayerIdentity
                    {
                        PlayerIdentityName = $"New player {Guid.NewGuid()}",
                        Team = new Team { TeamId = _testData.TeamWithFullDetails!.TeamId }
                    };
                    var playerRoute = $"/players/{playerIdentity.PlayerIdentityName.Kebaberize()}";
                    _copier.Setup(x => x.CreateAuditableCopy(playerIdentity)).Returns(playerIdentity);
                    _playerNameFormatter.Setup(x => x.CapitaliseName(playerIdentity.PlayerIdentityName)).Returns(playerIdentity.PlayerIdentityName);
                    _routeGenerator.Setup(x => x.GenerateUniqueRoute("/players", playerIdentity.PlayerIdentityName, NoiseWords.PlayerRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(playerRoute));
                    _routeGenerator.Setup(x => x.GenerateUniqueRoute(string.Empty, playerIdentity.PlayerIdentityName.Kebaberize(), NoiseWords.PlayerRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(playerIdentity.PlayerIdentityName.Kebaberize()));

                    var repo = CreateRepository();

                    var result = await repo.CreateOrMatchPlayerIdentity(playerIdentity, Guid.NewGuid(), "Member name", transaction);

                    Assert.NotNull(result);
                    _copier.Verify(x => x.CreateAuditableCopy(playerIdentity), Times.Once);
                    _playerNameFormatter.Verify(x => x.CapitaliseName(playerIdentity.PlayerIdentityName));
                    _routeGenerator.Verify(x => x.GenerateUniqueRoute("/players", playerIdentity.PlayerIdentityName, NoiseWords.PlayerRoute, It.IsAny<Func<string, Task<int>>>()));

                    var identityResult = await transaction.Connection.QuerySingleAsync<PlayerIdentityResult>(
                        $"SELECT PlayerId, PlayerIdentityName, ComparableName, RouteSegment, TeamId, LinkedBy FROM {Tables.PlayerIdentity} WHERE PlayerIdentityName = @PlayerIdentityName",
                        new { playerIdentity.PlayerIdentityName },
                        transaction);

                    Assert.NotNull(identityResult);
                    Assert.Equal(playerIdentity.PlayerIdentityName, identityResult.PlayerIdentityName);
                    Assert.Equal(playerIdentity.ComparableName(), identityResult.ComparableName);
                    Assert.Equal(playerIdentity.PlayerIdentityName.Kebaberize(), identityResult.RouteSegment);
                    Assert.Equal(playerIdentity.Team.TeamId, identityResult.TeamId);
                    Assert.Equal(PlayerIdentityLinkedBy.DefaultIdentity, identityResult.LinkedBy);

                    var playerResult = await transaction.Connection.QuerySingleAsync<Player>(
                        $"SELECT PlayerRoute FROM {Tables.Player} WHERE PlayerId = @PlayerId",
                        new { identityResult.PlayerId },
                        transaction);

                    Assert.NotNull(playerResult);
                    Assert.Equal(playerRoute, playerResult.PlayerRoute);

                    transaction.Rollback();
                }
            }
        }


        [Fact]
        public async Task CreateOrMatchPlayerIdentity_audits_and_logs()
        {
            var playerIdentity = new PlayerIdentity
            {
                PlayerIdentityName = $"New player {Guid.NewGuid()}",
                Team = new Team { TeamId = _testData.TeamWithFullDetails!.TeamId }
            };
            _copier.Setup(x => x.CreateAuditableCopy(playerIdentity)).Returns(playerIdentity);
            _playerNameFormatter.Setup(x => x.CapitaliseName(playerIdentity.PlayerIdentityName)).Returns(playerIdentity.PlayerIdentityName);
            _routeGenerator.Setup(x => x.GenerateUniqueRoute("/players", playerIdentity.PlayerIdentityName, NoiseWords.PlayerRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult($"/players/{Guid.NewGuid()}"));
            _routeGenerator.Setup(x => x.GenerateUniqueRoute(string.Empty, playerIdentity.PlayerIdentityName.Kebaberize(), NoiseWords.PlayerRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(playerIdentity.PlayerIdentityName.Kebaberize()));
            var memberName = "Member name";
            var memberKey = Guid.NewGuid();

            var repo = CreateRepository();

            using (var connection = _connectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var result = await repo.CreateOrMatchPlayerIdentity(playerIdentity, memberKey, memberName, transaction);
                    transaction.Rollback();
                }
            }

            _auditRepository.Verify(x => x.CreateAudit(It.IsAny<AuditRecord>(), It.IsAny<IDbTransaction>()), Times.Once);
            _logger.Verify(x => x.Info(LoggingTemplates.Created, It.IsAny<Player>(), memberName, memberKey, typeof(SqlServerPlayerRepository), nameof(SqlServerPlayerRepository.CreateOrMatchPlayerIdentity)));
        }
    }
}
