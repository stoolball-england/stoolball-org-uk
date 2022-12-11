using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Humanizer;
using Moq;
using Newtonsoft.Json;
using Stoolball.Data.Abstractions;
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
        private readonly Mock<IPlayerCacheInvalidator> _playerCacheClearer = new();

        public SqlServerPlayerRepositoryTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
            _scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        }

        private SqlServerPlayerRepository CreateRepository()
        {
            return new SqlServerPlayerRepository(_databaseFixture.ConnectionFactory,
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
#nullable disable
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
#nullable enable

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
            public Guid? PlayerId { get; set; }
            public string? PlayerIdentityName { get; set; }
            public string? ComparableName { get; set; }
            public string? RouteSegment { get; set; }
            public Guid? TeamId { get; set; }
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
                        Team = new Team { TeamId = _databaseFixture.TestData.TeamWithFullDetails!.TeamId }
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
                        $"SELECT PlayerId, PlayerIdentityName, ComparableName, RouteSegment, TeamId FROM {Tables.PlayerIdentity} WHERE PlayerIdentityName = @PlayerIdentityName",
                        new { playerIdentity.PlayerIdentityName },
                        transaction);

                    Assert.NotNull(identityResult);
                    Assert.Equal(playerIdentity.PlayerIdentityName, identityResult.PlayerIdentityName);
                    Assert.Equal(playerIdentity.ComparableName(), identityResult.ComparableName);
                    Assert.Equal(playerIdentity.PlayerIdentityName.Kebaberize(), identityResult.RouteSegment);
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
                Team = new Team { TeamId = _databaseFixture.TestData.TeamWithFullDetails!.TeamId }
            };
            _copier.Setup(x => x.CreateAuditableCopy(playerIdentity)).Returns(playerIdentity);
            _playerNameFormatter.Setup(x => x.CapitaliseName(playerIdentity.PlayerIdentityName)).Returns(playerIdentity.PlayerIdentityName);
            _routeGenerator.Setup(x => x.GenerateUniqueRoute("/players", playerIdentity.PlayerIdentityName, NoiseWords.PlayerRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult($"/players/{Guid.NewGuid()}"));
            _routeGenerator.Setup(x => x.GenerateUniqueRoute(string.Empty, playerIdentity.PlayerIdentityName.Kebaberize(), NoiseWords.PlayerRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(playerIdentity.PlayerIdentityName.Kebaberize()));
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

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.LinkPlayerToMemberAccount(new Player { PlayerRoute = "/example" }, Guid.NewGuid(), "Member name"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task LinkPlayerToMemberAccount_throws_ArgumentException_if_PlayerRoute_is_missing(string playerRoute)
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.LinkPlayerToMemberAccount(new Player { PlayerId = Guid.NewGuid(), PlayerRoute = playerRoute }, Guid.NewGuid(), "Member name"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task LinkPlayerToMemberAccount_throws_ArgumentNullException_if_memberName_is_missing(string memberName)
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.LinkPlayerToMemberAccount(new Player { PlayerId = Guid.NewGuid(), PlayerRoute = "/example" }, Guid.NewGuid(), memberName));
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
            await repo.ProcessAsyncUpdatesForLinkingAndUnlinkingPlayersToMemberAccounts();

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
                    var alreadyLinkedPlayerIdNowLinkedToMovedPlayerIdentity = await connectionForAssert.QuerySingleAsync<Guid>($"SELECT PlayerId FROM {Tables.PlayerIdentity} WHERE PlayerIdentityId = @PlayerIdentityId", identity);
                    Assert.Equal(playerAlreadyLinked.PlayerId, alreadyLinkedPlayerIdNowLinkedToMovedPlayerIdentity);

                    var playerIdsForMovedIdentityShouldBeTheAlreadyLinkedPlayer = await connectionForAssert.QueryAsync<Guid>($"SELECT PlayerId FROM {Tables.PlayerInMatchStatistics} WHERE PlayerIdentityId = @PlayerIdentityId", identity);
                    foreach (var playerIdNowLinkedToSecondPlayerIdentityInStatistics in playerIdsForMovedIdentityShouldBeTheAlreadyLinkedPlayer)
                    {
                        Assert.Equal(playerAlreadyLinked.PlayerId, playerIdNowLinkedToSecondPlayerIdentityInStatistics);
                    }

                    var playerRouteForAllIdentitiesOfAlreadyLinkedPlayerShouldBeUpdated = await connectionForAssert.QueryAsync<string>($"SELECT PlayerRoute FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = @PlayerId", playerAlreadyLinked);
                    foreach (var route in playerRouteForAllIdentitiesOfAlreadyLinkedPlayerShouldBeUpdated)
                    {
                        Assert.Equal(playerNotLinkedToMember.PlayerRoute, route);
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
            var playerLinkedToMember = AnyPlayerLinkedToMemberWithOnlyOneIdentity();
            var playerCopy = new Player { PlayerId = playerLinkedToMember.PlayerId };
            _copier.Setup(x => x.CreateAuditableCopy(playerLinkedToMember)).Returns(playerCopy);

            var repo = CreateRepository();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await repo.LinkPlayerToMemberAccount(playerLinkedToMember, Guid.NewGuid(), "Member name"));
        }

        [Fact]
        public async Task LinkPlayerToMemberAccount_where_member_has_no_previous_player_audits_and_logs()
        {
            var player = AnyPlayerNotLinkedToMember();
            var playerCopy = new Player { PlayerId = player.PlayerId };
            _copier.Setup(x => x.CreateAuditableCopy(player)).Returns(playerCopy);
            var memberNotLinkedToPlayer = AnyMemberNotLinkedToPlayer();

            var repo = CreateRepository();
            await repo.LinkPlayerToMemberAccount(player, memberNotLinkedToPlayer.memberKey, memberNotLinkedToPlayer.memberName);

            _auditRepository.Verify(x => x.CreateAudit(It.Is<AuditRecord>(x => x.Action == AuditAction.Update && x.EntityUri.ToString().EndsWith(player.PlayerId.ToString()!)), It.IsAny<IDbTransaction>()), Times.Once);
            _logger.Verify(x => x.Info(LoggingTemplates.Updated, JsonConvert.SerializeObject(playerCopy), memberNotLinkedToPlayer.memberName, memberNotLinkedToPlayer.memberKey, typeof(SqlServerPlayerRepository), nameof(SqlServerPlayerRepository.LinkPlayerToMemberAccount)));
        }

        [Fact]
        public async Task LinkPlayerToMemberAccount_where_member_has_linked_player_audits_and_logs()
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

            _auditRepository.Verify(x => x.CreateAudit(It.Is<AuditRecord>(x => x.Action == AuditAction.Delete && x.EntityUri.ToString().EndsWith(playerNotLinkedToMember.PlayerId.ToString()!)), It.IsAny<IDbTransaction>()), Times.Once);
            _logger.Verify(x => x.Info(LoggingTemplates.Deleted, It.Is<string>(x => x.Contains(playerNotLinkedToMember.PlayerId.ToString()!)), memberWithExistingPlayer.memberName, memberWithExistingPlayer.memberKey, typeof(SqlServerPlayerRepository), nameof(SqlServerPlayerRepository.LinkPlayerToMemberAccount)));

            _auditRepository.Verify(x => x.CreateAudit(It.Is<AuditRecord>(x => x.Action == AuditAction.Update && x.EntityUri.ToString().EndsWith(playerAlreadyLinked.PlayerId.ToString()!)), It.IsAny<IDbTransaction>()), Times.Once);
            _logger.Verify(x => x.Info(LoggingTemplates.Updated, It.Is<string>(x => x.Contains(playerAlreadyLinked.PlayerId.ToString()!)), memberWithExistingPlayer.memberName, memberWithExistingPlayer.memberKey, typeof(SqlServerPlayerRepository), nameof(SqlServerPlayerRepository.LinkPlayerToMemberAccount)));
        }

        [Fact]
        public async Task UnlinkPlayerIdentityFromMemberAccount_throws_ArgumentException_if_PlayerIdentityId_is_null()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.UnlinkPlayerIdentityFromMemberAccount(new PlayerIdentity { PlayerIdentityName = "player" }, Guid.NewGuid(), "Member name"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task UnlinkPlayerIdentityFromMemberAccount_throws_ArgumentException_if_PlayerIdentityName_is_missing(string playerIdentityName)
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.UnlinkPlayerIdentityFromMemberAccount(new PlayerIdentity { PlayerIdentityId = Guid.NewGuid(), PlayerIdentityName = playerIdentityName }, Guid.NewGuid(), "Member name"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task UnlinkPlayerIdentityFromMemberAccount_throws_ArgumentNullException_if_memberName_is_missing(string memberName)
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.UnlinkPlayerIdentityFromMemberAccount(new PlayerIdentity { PlayerIdentityId = Guid.NewGuid(), PlayerIdentityName = "player" }, Guid.NewGuid(), memberName));
        }

        [Fact]
        public async Task UnlinkPlayerIdentityFromMemberAccount_for_penultimate_identity_moves_identity_to_new_player_including_statistics()
        {
            var player = AnyPlayerLinkedToMemberWithMultipleIdentities();
            var playerCopy = new Player { PlayerId = player.PlayerId };
            _copier.Setup(x => x.CreateAuditableCopy(player)).Returns(playerCopy);
            var playerIdentity = player.PlayerIdentities.First();
            var member = _databaseFixture.TestData.Members.Single(x => x.memberKey == player.MemberKey);

            var generatedPlayerRoute = "/players/" + Guid.NewGuid().ToString();
            _routeGenerator.Setup(x => x.GenerateUniqueRoute("/players", playerIdentity.PlayerIdentityName, NoiseWords.PlayerRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(generatedPlayerRoute));

            var repo = CreateRepository();

            await repo.UnlinkPlayerIdentityFromMemberAccount(playerIdentity, member.memberKey, member.memberName);
            await repo.ProcessAsyncUpdatesForLinkingAndUnlinkingPlayersToMemberAccounts();

            using (var connectionForAssert = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                connectionForAssert.Open();

                var originalPlayerStillLinkedToMember = await connectionForAssert.QuerySingleAsync<Guid?>($"SELECT MemberKey FROM {Tables.Player} WHERE PlayerId = @PlayerId", player);
                Assert.Equal(member.memberKey, originalPlayerStillLinkedToMember);

                var newPlayerLinkedToIdentity = await connectionForAssert.QuerySingleAsync<(Guid? playerId, string route, Guid? memberKey)>(
                    $@"SELECT p.PlayerId, p.PlayerRoute, p.MemberKey 
                       FROM {Tables.Player} p INNER JOIN {Tables.PlayerIdentity} pi ON p.PlayerId = pi.PlayerId 
                       WHERE PlayerIdentityId = @PlayerIdentityId", playerIdentity);
                Assert.NotEqual(player.PlayerId, newPlayerLinkedToIdentity.playerId);
                Assert.Equal(generatedPlayerRoute, newPlayerLinkedToIdentity.route);
                Assert.Null(newPlayerLinkedToIdentity.memberKey);

                var statisticsForIdentity = await connectionForAssert.QueryAsync<(Guid? playerId, string route)>($"SELECT PlayerId, PlayerRoute FROM {Tables.PlayerInMatchStatistics} WHERE PlayerIdentityId = @PlayerIdentityId", playerIdentity);
                foreach (var row in statisticsForIdentity)
                {
                    Assert.Equal(newPlayerLinkedToIdentity.playerId, row.playerId);
                    Assert.Equal(generatedPlayerRoute, row.route);
                }
            }
        }

        [Fact]
        public async Task UnlinkPlayerIdentityFromMemberAccount_for_penultimate_identity_audits_and_logs()
        {
            var player = AnyPlayerLinkedToMemberWithMultipleIdentities();
            var playerCopy = new Player { PlayerId = player.PlayerId };
            _copier.Setup(x => x.CreateAuditableCopy(player)).Returns(playerCopy);
            var playerIdentity = player.PlayerIdentities.First();
            var member = _databaseFixture.TestData.Members.Single(x => x.memberKey == player.MemberKey);

            var generatedPlayerRoute = "/players/" + Guid.NewGuid().ToString();
            _routeGenerator.Setup(x => x.GenerateUniqueRoute("/players", playerIdentity.PlayerIdentityName, NoiseWords.PlayerRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(generatedPlayerRoute));

            var repo = CreateRepository();

            await repo.UnlinkPlayerIdentityFromMemberAccount(playerIdentity, member.memberKey, member.memberName);

            _auditRepository.Verify(x => x.CreateAudit(It.IsAny<AuditRecord>(), It.IsAny<IDbTransaction>()), Times.Once);
            _logger.Verify(x => x.Info(LoggingTemplates.Created, It.IsAny<string>(), member.memberName, member.memberKey, typeof(SqlServerPlayerRepository), nameof(SqlServerPlayerRepository.UnlinkPlayerIdentityFromMemberAccount)));
        }

        [Fact]
        public async Task UnlinkPlayerIdentityFromMemberAccount_for_last_identity_unlinks_player_from_member()
        {
            var player = AnyPlayerLinkedToMemberWithOnlyOneIdentity();
            var member = _databaseFixture.TestData.Members.Single(x => x.memberKey == player.MemberKey);

            var repo = CreateRepository();

            await repo.UnlinkPlayerIdentityFromMemberAccount(player.PlayerIdentities.First(), member.memberKey, member.memberName);

            using (var connectionForAssert = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                connectionForAssert.Open();
                var result = await connectionForAssert.QuerySingleAsync<Guid?>($"SELECT MemberKey FROM {Tables.Player} WHERE PlayerId = @PlayerId", player);

                Assert.Null(result);
            }
        }

        [Fact]
        public async Task UnlinkPlayerIdentityFromMemberAccount_for_last_identity_audits_and_logs()
        {
            var player = AnyPlayerLinkedToMemberWithOnlyOneIdentity();
            var member = _databaseFixture.TestData.Members.Single(x => x.memberKey == player.MemberKey);

            var repo = CreateRepository();

            await repo.UnlinkPlayerIdentityFromMemberAccount(player.PlayerIdentities.First(), member.memberKey, member.memberName);

            _auditRepository.Verify(x => x.CreateAudit(It.IsAny<AuditRecord>(), It.IsAny<IDbTransaction>()), Times.Once);
            _logger.Verify(x => x.Info(LoggingTemplates.Updated, It.IsAny<string>(), member.memberName, member.memberKey, typeof(SqlServerPlayerRepository), nameof(SqlServerPlayerRepository.UnlinkPlayerIdentityFromMemberAccount)));
        }

        private Player AnyPlayerNotLinkedToMember()
        {
            return _databaseFixture.TestData.Players.First(x => !x.MemberKey.HasValue);
        }

        private Player AnyPlayerLinkedToMemberWithMultipleIdentities()
        {
            return _databaseFixture.TestData.Players.First(x => x.MemberKey.HasValue && x.PlayerIdentities.Count() > 1);
        }

        private Player AnyPlayerLinkedToMemberWithOnlyOneIdentity()
        {
            return _databaseFixture.TestData.Players.First(x => x.MemberKey.HasValue && x.PlayerIdentities.Count() == 1);
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
