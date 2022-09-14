using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Humanizer;
using Moq;
using Newtonsoft.Json;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Logging;
using Stoolball.Routing;
using Stoolball.Statistics;
using Stoolball.Teams;
using Xunit;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer.IntegrationTests.Statistics
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class SqlServerPlayerRepositoryTests : IDisposable
    {
        private readonly SqlServerTestDataFixture _databaseFixture;
        private readonly TransactionScope _scope;
        private readonly Mock<IAuditRepository> _auditRepository = new();
        private readonly Mock<ILogger<SqlServerPlayerRepository>> _logger = new();
        private readonly Mock<IStoolballEntityCopier> _copier = new();
        private readonly Mock<IPlayerNameFormatter> _playerNameFormatter = new();
        private readonly Mock<IRouteGenerator> _routeGenerator = new();
        private readonly Mock<IBestRouteSelector> _routeSelector = new();
        private readonly Mock<IRedirectsRepository> _redirectsRepository = new();

        public SqlServerPlayerRepositoryTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
            _scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        }

        private SqlServerPlayerRepository CreateRepository()
        {
            return new SqlServerPlayerRepository(_databaseFixture.ConnectionFactory,
                _auditRepository.Object,
                _logger.Object,
                _redirectsRepository.Object,
                _routeGenerator.Object,
                _copier.Object,
                _playerNameFormatter.Object,
                _routeSelector.Object);
        }

        [Fact]
        public async Task CreateOrMatchPlayerIdentity_throws_ArgumentNullException_if_playerIdentity_is_null()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.CreateOrMatchPlayerIdentity(null, Guid.NewGuid(), "Member name", Mock.Of<IDbTransaction>()));
        }

        [Fact]
        public async Task CreateOrMatchPlayerIdentity_throws_ArgumentException_if_PlayerIdentityName_is_null()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.CreateOrMatchPlayerIdentity(new PlayerIdentity { PlayerIdentityName = null, Team = new Team { TeamId = Guid.NewGuid() } }, Guid.NewGuid(), "Member name", Mock.Of<IDbTransaction>()));
        }

        [Fact]
        public async Task CreateOrMatchPlayerIdentity_throws_ArgumentException_if_PlayerIdentityName_is_empty_string()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.CreateOrMatchPlayerIdentity(new PlayerIdentity { PlayerIdentityName = string.Empty, Team = new Team { TeamId = Guid.NewGuid() } }, Guid.NewGuid(), "Member name", Mock.Of<IDbTransaction>()));
        }

        [Fact]
        public async Task CreateOrMatchPlayerIdentity_throws_ArgumentNullException_if_memberName_is_null()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.CreateOrMatchPlayerIdentity(new PlayerIdentity { PlayerIdentityName = "Player 1", Team = new Team { TeamId = Guid.NewGuid() } }, Guid.NewGuid(), null, Mock.Of<IDbTransaction>()));
        }

        [Fact]
        public async Task CreateOrMatchPlayerIdentity_throws_ArgumentNullException_if_memberName_is_empty_string()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.CreateOrMatchPlayerIdentity(new PlayerIdentity { PlayerIdentityName = "Player 1", Team = new Team { TeamId = Guid.NewGuid() } }, Guid.NewGuid(), string.Empty, Mock.Of<IDbTransaction>()));
        }

        [Fact]
        public async Task CreateOrMatchPlayerIdentity_throws_ArgumentException_if_TeamId_is_null()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.CreateOrMatchPlayerIdentity(new PlayerIdentity { PlayerIdentityName = "Player 1" }, Guid.NewGuid(), "Member name", Mock.Of<IDbTransaction>()));
        }

        [Fact]
        public async Task CreateOrMatchPlayerIdentity_throws_ArgumentNullException_if_transaction_is_null()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.CreateOrMatchPlayerIdentity(new PlayerIdentity { PlayerIdentityName = "Player 1", Team = new Team { TeamId = Guid.NewGuid() } }, Guid.NewGuid(), "Member name", null));
        }

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

            var result = await repo.CreateOrMatchPlayerIdentity(playerIdentity, Guid.NewGuid(), null, transaction.Object);

            Assert.Equal(playerIdentity, result);
            transaction.Verify(x => x.Connection, Times.Never);
        }

        [Fact]
        public async Task Matched_PlayerIdentity_is_returned()
        {
            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
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
                    Assert.Equal(dataForAnyPlayerIdentity.playerId, result.Player.PlayerId);
                    Assert.Equal(dataForAnyPlayerIdentity.teamId, result.Team.TeamId);
                    Assert.Equal(dataForAnyPlayerIdentity.comparableName, result.ComparableName());

                    transaction.Rollback();
                }
            }
        }

        private class PlayerIdentityResult
        {
            public Guid PlayerId { get; set; }
            public string PlayerIdentityName { get; set; }
            public string ComparableName { get; set; }
            public Guid TeamId { get; set; }
        }

        [Fact]
        public async Task Unmatched_PlayerIdentity_returns_new_player_and_identity()
        {
            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var playerIdentity = new PlayerIdentity
                    {
                        PlayerIdentityName = $"New player {Guid.NewGuid()}",
                        Team = new Team { TeamId = _databaseFixture.TestData.TeamWithFullDetails.TeamId }
                    };
                    var playerRoute = $"/players/{playerIdentity.PlayerIdentityName.Kebaberize()}";
                    _copier.Setup(x => x.CreateAuditableCopy(playerIdentity)).Returns(playerIdentity);
                    _playerNameFormatter.Setup(x => x.CapitaliseName(playerIdentity.PlayerIdentityName)).Returns(playerIdentity.PlayerIdentityName);
                    _routeGenerator.Setup(x => x.GenerateUniqueRoute("/players", playerIdentity.PlayerIdentityName, NoiseWords.PlayerRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(playerRoute));

                    var repo = CreateRepository();

                    var result = await repo.CreateOrMatchPlayerIdentity(playerIdentity, Guid.NewGuid(), "Member name", transaction);

                    Assert.NotNull(result);
                    _copier.Verify(x => x.CreateAuditableCopy(playerIdentity), Times.Once);
                    _playerNameFormatter.Verify(x => x.CapitaliseName(playerIdentity.PlayerIdentityName));
                    _routeGenerator.Verify(x => x.GenerateUniqueRoute("/players", playerIdentity.PlayerIdentityName, NoiseWords.PlayerRoute, It.IsAny<Func<string, Task<int>>>()));

                    var identityResult = await transaction.Connection.QuerySingleAsync<PlayerIdentityResult>(
                        $"SELECT PlayerId, PlayerIdentityName, ComparableName, TeamId FROM {Tables.PlayerIdentity} WHERE PlayerIdentityName = @PlayerIdentityName",
                        new { playerIdentity.PlayerIdentityName },
                        transaction);

                    Assert.NotNull(identityResult);
                    Assert.Equal(playerIdentity.PlayerIdentityName, identityResult.PlayerIdentityName);
                    Assert.Equal(playerIdentity.ComparableName(), identityResult.ComparableName);
                    Assert.Equal(playerIdentity.Team.TeamId, identityResult.TeamId);

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
                Team = new Team { TeamId = _databaseFixture.TestData.TeamWithFullDetails.TeamId }
            };
            _copier.Setup(x => x.CreateAuditableCopy(playerIdentity)).Returns(playerIdentity);
            _playerNameFormatter.Setup(x => x.CapitaliseName(playerIdentity.PlayerIdentityName)).Returns(playerIdentity.PlayerIdentityName);
            _routeGenerator.Setup(x => x.GenerateUniqueRoute("/players", playerIdentity.PlayerIdentityName, NoiseWords.PlayerRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult($"/players/{Guid.NewGuid()}"));
            var memberName = "Member name";
            var memberKey = Guid.NewGuid();

            var repo = CreateRepository();

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
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

        [Fact]
        public async Task LinkPlayerToMemberAccount_throws_ArgumentException_if_PlayerId_is_null()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.LinkPlayerToMemberAccount(new Player { PlayerRoute = "/example" }, Guid.NewGuid(), null));
        }

        [Fact]
        public async Task LinkPlayerToMemberAccount_throws_ArgumentException_if_PlayerRoute_is_null()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.LinkPlayerToMemberAccount(new Player { PlayerId = Guid.NewGuid() }, Guid.NewGuid(), null));
        }

        [Fact]
        public async Task LinkPlayerToMemberAccount_throws_ArgumentNullException_if_memberName_is_null()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.LinkPlayerToMemberAccount(new Player { PlayerId = Guid.NewGuid(), PlayerRoute = "/example" }, Guid.NewGuid(), null));
        }

        [Fact]
        public async Task LinkPlayerToMemberAccount_throws_ArgumentNullException_if_memberName_is_empty_string()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.LinkPlayerToMemberAccount(new Player { PlayerId = Guid.NewGuid(), PlayerRoute = "/example" }, Guid.NewGuid(), string.Empty));
        }

        [Fact]
        public async Task LinkPlayerToMemberAccount_where_member_has_no_previous_player_updates_player()
        {
            var memberWithNoExistingPlayer = AnyMemberNotLinkedToPlayer();
            var playerNotLinkedToMember = AnyPlayerNotLinkedToMember();
            var playerCopy = new Player { PlayerId = playerNotLinkedToMember.PlayerId, PlayerRoute = playerNotLinkedToMember.PlayerRoute };
            _copier.Setup(x => x.CreateAuditableCopy(playerNotLinkedToMember)).Returns(playerCopy);

            var repo = CreateRepository();
            var returnedPlayer = await repo.LinkPlayerToMemberAccount(playerNotLinkedToMember, memberWithNoExistingPlayer.memberKey, memberWithNoExistingPlayer.memberName);

            Assert.Equal(memberWithNoExistingPlayer.memberKey, returnedPlayer.MemberKey);

            using (var connectionForAssert = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                connectionForAssert.Open();
                var result = await connectionForAssert.QuerySingleAsync<Guid>($"SELECT MemberKey FROM {Tables.Player} WHERE PlayerId = @PlayerId", playerCopy);

                Assert.Equal(memberWithNoExistingPlayer.memberKey, result);
            }
        }


        [Fact]
        public async Task LinkPlayerToMemberAccount_where_member_has_linked_player_merges_players()
        {
            var memberWithExistingPlayer = AnyMemberLinkedToPlayer();
            var playerAlreadyLinked = _databaseFixture.TestData.Players.First(x => x.MemberKey == memberWithExistingPlayer.memberKey);
            var playerAlreadyLinkedCopy = new Player { PlayerId = playerAlreadyLinked.PlayerId, PlayerRoute = playerAlreadyLinked.PlayerRoute };
            _copier.Setup(x => x.CreateAuditableCopy(playerAlreadyLinked)).Returns(playerAlreadyLinkedCopy);

            var playerNotLinkedToMember = AnyPlayerNotLinkedToMember();
            var playerNotLinkedToMemberCopy = new Player { PlayerId = playerNotLinkedToMember.PlayerId, PlayerRoute = playerNotLinkedToMember.PlayerRoute };
            _copier.Setup(x => x.CreateAuditableCopy(playerNotLinkedToMember)).Returns(playerNotLinkedToMemberCopy);

            _routeSelector.Setup(x => x.SelectBestRoute(playerAlreadyLinked.PlayerRoute, playerNotLinkedToMember.PlayerRoute)).Returns(playerNotLinkedToMember.PlayerRoute);

            var repo = CreateRepository();
            var returnedPlayer = await repo.LinkPlayerToMemberAccount(playerNotLinkedToMember, memberWithExistingPlayer.memberKey, memberWithExistingPlayer.memberName);

            // The returned player should have the result of merging both players
            Assert.Equal(playerAlreadyLinked.PlayerId, returnedPlayer.PlayerId);
            Assert.Equal(playerNotLinkedToMember.PlayerRoute, returnedPlayer.PlayerRoute);
            Assert.Equal(memberWithExistingPlayer.memberKey, returnedPlayer.MemberKey);

            using (var connectionForAssert = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                connectionForAssert.Open();

                var originalPlayerStillLinkedToMemberWithUpdatedRoute = await connectionForAssert.QuerySingleAsync<Player>($"SELECT PlayerId, PlayerRoute FROM {Tables.Player} WHERE MemberKey = @memberKey", new { memberWithExistingPlayer.memberKey });
                Assert.Equal(playerAlreadyLinked.PlayerId, originalPlayerStillLinkedToMemberWithUpdatedRoute.PlayerId);
                Assert.Equal(playerNotLinkedToMember.PlayerRoute, originalPlayerStillLinkedToMemberWithUpdatedRoute.PlayerRoute);

                foreach (var identity in playerNotLinkedToMember.PlayerIdentities)
                {
                    var alreadyLinkedPlayerIdNowLinkedToPlayerIdentityFromSecondPlayer = await connectionForAssert.QuerySingleAsync<Guid>($"SELECT PlayerId FROM {Tables.PlayerIdentity} WHERE PlayerIdentityId = @PlayerIdentityId", identity);
                    Assert.Equal(playerAlreadyLinked.PlayerId, alreadyLinkedPlayerIdNowLinkedToPlayerIdentityFromSecondPlayer);

                    var playerIdsForIdentitiesBelongingToSecondPlayer = await connectionForAssert.QueryAsync<Guid>($"SELECT PlayerId FROM {Tables.PlayerInMatchStatistics} WHERE PlayerIdentityId = @PlayerIdentityId", identity);
                    foreach (var playerIdNowLinkedToSecondPlayerIdentityInStatistics in playerIdsForIdentitiesBelongingToSecondPlayer)
                    {
                        Assert.Equal(playerAlreadyLinked.PlayerId, playerIdNowLinkedToSecondPlayerIdentityInStatistics);
                    }
                }

                var obsoletePlayerShouldBeRemoved = await connectionForAssert.QuerySingleOrDefaultAsync<int>($"SELECT COUNT(PlayerId) FROM {Tables.Player} WHERE PlayerId = @PlayerId", playerNotLinkedToMember);
                Assert.Equal(0, obsoletePlayerShouldBeRemoved);
            }
        }

        [Fact]
        public async Task LinkPlayerToMemberAccount_where_member_has_linked_player_inserts_redirects()
        {
            var memberWithExistingPlayer = AnyMemberLinkedToPlayer();
            var playerAlreadyLinked = _databaseFixture.TestData.Players.First(x => x.MemberKey == memberWithExistingPlayer.memberKey);
            var playerAlreadyLinkedCopy = new Player { PlayerId = playerAlreadyLinked.PlayerId, PlayerRoute = playerAlreadyLinked.PlayerRoute };
            _copier.Setup(x => x.CreateAuditableCopy(playerAlreadyLinked)).Returns(playerAlreadyLinkedCopy);

            var playerNotLinkedToMember = AnyPlayerNotLinkedToMember();
            var playerNotLinkedToMemberCopy = new Player { PlayerId = playerNotLinkedToMember.PlayerId, PlayerRoute = playerNotLinkedToMember.PlayerRoute };
            _copier.Setup(x => x.CreateAuditableCopy(playerNotLinkedToMember)).Returns(playerNotLinkedToMemberCopy);

            _routeSelector.Setup(x => x.SelectBestRoute(playerAlreadyLinked.PlayerRoute, playerNotLinkedToMember.PlayerRoute)).Returns(playerNotLinkedToMember.PlayerRoute);

            var repo = CreateRepository();
            await repo.LinkPlayerToMemberAccount(playerNotLinkedToMember, memberWithExistingPlayer.memberKey, memberWithExistingPlayer.memberName);

            _redirectsRepository.Verify(x => x.InsertRedirect(playerAlreadyLinked.PlayerRoute, playerNotLinkedToMember.PlayerRoute, null, It.IsAny<IDbTransaction>()), Times.Once);
            _redirectsRepository.Verify(x => x.InsertRedirect(playerAlreadyLinked.PlayerRoute, playerNotLinkedToMember.PlayerRoute, "/batting", It.IsAny<IDbTransaction>()), Times.Once);
            _redirectsRepository.Verify(x => x.InsertRedirect(playerAlreadyLinked.PlayerRoute, playerNotLinkedToMember.PlayerRoute, "/bowling", It.IsAny<IDbTransaction>()), Times.Once);
            _redirectsRepository.Verify(x => x.InsertRedirect(playerAlreadyLinked.PlayerRoute, playerNotLinkedToMember.PlayerRoute, "/fielding", It.IsAny<IDbTransaction>()), Times.Once);
            _redirectsRepository.Verify(x => x.InsertRedirect(playerAlreadyLinked.PlayerRoute, playerNotLinkedToMember.PlayerRoute, "/individual-scores", It.IsAny<IDbTransaction>()), Times.Once);
            _redirectsRepository.Verify(x => x.InsertRedirect(playerAlreadyLinked.PlayerRoute, playerNotLinkedToMember.PlayerRoute, "/bowling-figures", It.IsAny<IDbTransaction>()), Times.Once);
            _redirectsRepository.Verify(x => x.InsertRedirect(playerAlreadyLinked.PlayerRoute, playerNotLinkedToMember.PlayerRoute, "/catches", It.IsAny<IDbTransaction>()), Times.Once);
            _redirectsRepository.Verify(x => x.InsertRedirect(playerAlreadyLinked.PlayerRoute, playerNotLinkedToMember.PlayerRoute, "/run-outs", It.IsAny<IDbTransaction>()), Times.Once);
        }

        [Fact]
        public async Task LinkPlayerToMemberAccount_where_player_already_linked_throws_InvalidOperationException()
        {
            var playerLinkedToMember = AnyPlayerLinkedToMember();
            var playerCopy = new Player { PlayerId = playerLinkedToMember.PlayerId };
            _copier.Setup(x => x.CreateAuditableCopy(playerLinkedToMember)).Returns(playerCopy);

            var repo = CreateRepository();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await repo.LinkPlayerToMemberAccount(playerLinkedToMember, Guid.NewGuid(), "Member name"));
        }

        [Fact]
        public async Task LinkPlayerToMemberAccount_audits_and_logs()
        {
            var player = AnyPlayerNotLinkedToMember();
            var playerCopy = new Player { PlayerId = player.PlayerId };
            _copier.Setup(x => x.CreateAuditableCopy(player)).Returns(playerCopy);
            var memberNotLinkedToPlayer = AnyMemberNotLinkedToPlayer();

            var repo = CreateRepository();
            await repo.LinkPlayerToMemberAccount(player, memberNotLinkedToPlayer.memberKey, memberNotLinkedToPlayer.memberName);

            _auditRepository.Verify(x => x.CreateAudit(It.IsAny<AuditRecord>(), It.IsAny<IDbTransaction>()), Times.Once);
            _logger.Verify(x => x.Info(LoggingTemplates.Updated, JsonConvert.SerializeObject(playerCopy), memberNotLinkedToPlayer.memberName, memberNotLinkedToPlayer.memberKey, typeof(SqlServerPlayerRepository), nameof(SqlServerPlayerRepository.LinkPlayerToMemberAccount)));
        }

        private Player AnyPlayerNotLinkedToMember()
        {
            return _databaseFixture.TestData.Players.First(x => !x.MemberKey.HasValue);
        }

        private Player AnyPlayerLinkedToMember()
        {
            return _databaseFixture.TestData.Players.First(x => x.MemberKey.HasValue);
        }

        private (Guid memberKey, string memberName) AnyMemberLinkedToPlayer()
        {
            return _databaseFixture.TestData.Members.First(x => _databaseFixture.TestData.Players.Any(p => p.MemberKey == x.memberKey));
        }

        private (Guid memberKey, string memberName) AnyMemberNotLinkedToPlayer()
        {
            return _databaseFixture.TestData.Members.First(x => !_databaseFixture.TestData.Players.Any(p => p.MemberKey == x.memberKey));
        }

        public void Dispose() => _scope.Dispose();
    }
}
