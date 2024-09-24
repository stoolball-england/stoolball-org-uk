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
using Stoolball.Matches;
using Stoolball.Routing;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Testing;
using Xunit;
using static Dapper.SqlMapper;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer.IntegrationTests.Statistics
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class SqlServerPlayerRepositoryTests : IDisposable
    {
        #region Setup and dispose
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly TestData _testData;
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
            _connectionFactory = databaseFixture?.ConnectionFactory ?? throw new ArgumentException($"{nameof(databaseFixture)}.{nameof(databaseFixture.ConnectionFactory)} cannot be null", nameof(databaseFixture));
            _testData = databaseFixture?.TestData ?? throw new ArgumentException($"{nameof(databaseFixture)}.{nameof(databaseFixture.TestData)} cannot be null", nameof(databaseFixture));
            _scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        }
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

        private PlayerIdentity SetupCopyOfPlayerIdentity(PlayerIdentity playerIdentityToUpdate, string updatedPlayerIdentityName)
        {
            var updatedPlayerIdentity = new PlayerIdentity
            {
                PlayerIdentityId = playerIdentityToUpdate.PlayerIdentityId,
                PlayerIdentityName = updatedPlayerIdentityName,
                Team = new Team { TeamId = playerIdentityToUpdate.Team?.TeamId },
                Player = new Player
                {
                    PlayerId = playerIdentityToUpdate.Player!.PlayerId,
                    PlayerIdentities = playerIdentityToUpdate.Player.PlayerIdentities.Select(x => new PlayerIdentity
                    {
                        PlayerIdentityId = x.PlayerIdentityId,
                        PlayerIdentityName = x.PlayerIdentityName
                    }).ToList()
                }
            };

            var updatedPlayerIdentityCopy = new PlayerIdentity
            {
                PlayerIdentityId = updatedPlayerIdentity.PlayerIdentityId,
                PlayerIdentityName = updatedPlayerIdentity.PlayerIdentityName,
                Team = new Team { TeamId = updatedPlayerIdentity.Team?.TeamId },
                Player = new Player { PlayerId = updatedPlayerIdentity.Player?.PlayerId }
            };

            _copier.Setup(x => x.CreateAuditableCopy(updatedPlayerIdentity.Player)).Returns(new Player { PlayerId = updatedPlayerIdentityCopy.Player.PlayerId });
            _copier.Setup(x => x.CreateAuditableCopy(updatedPlayerIdentity)).Returns(updatedPlayerIdentityCopy);
            _playerNameFormatter.Setup(x => x.CapitaliseName(updatedPlayerIdentityName)).Returns(updatedPlayerIdentityName);

            return updatedPlayerIdentity;
        }

        private void SetupRouteGenerator(PlayerIdentity originalPlayerIdentity, PlayerIdentity updatedPlayerIdentity, string updatedPlayerIdentityRouteSegment, string updatedPlayerRoute)
        {
            _routeGenerator.Setup(x => x.GenerateUniqueRoute(string.Empty, updatedPlayerIdentityRouteSegment, NoiseWords.PlayerRoute, It.IsAny<Func<string, Task<int>>>())).ReturnsAsync(updatedPlayerIdentityRouteSegment);

            var allPlayerIdentityNamesForPlayer = originalPlayerIdentity.Player!.PlayerIdentities.Select(pi => pi.PlayerIdentityName).Where(x => x != originalPlayerIdentity.PlayerIdentityName).ToList();
            allPlayerIdentityNamesForPlayer.Add(updatedPlayerIdentity.PlayerIdentityName);
            _routeGenerator.Setup(x => x.GenerateUniqueRoute("/players", It.Is<string>(x => allPlayerIdentityNamesForPlayer.Contains(x)), NoiseWords.PlayerRoute, It.IsAny<Func<string, Task<int>>>())).ReturnsAsync(updatedPlayerRoute);
        }

        #endregion

        #region CreateOrMatchPlayerIdentity
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
        #endregion

        #region LinkPlayerToMemberAccount
        [Fact]
        public async Task LinkPlayerToMemberAccount_where_member_has_no_previous_player_updates_player_and_identity()
        {
            var memberWithNoExistingPlayer = _testData.AnyMemberNotLinkedToPlayer();
            var playerNotLinkedToMember = _testData.AnyPlayerNotLinkedToMember();
            var playerCopy = new Player { PlayerId = playerNotLinkedToMember.PlayerId, PlayerRoute = playerNotLinkedToMember.PlayerRoute };
            _copier.Setup(x => x.CreateAuditableCopy(playerNotLinkedToMember)).Returns(playerCopy);

            var repo = CreateRepository();
            var returnedPlayer = await repo.LinkPlayerToMemberAccount(playerNotLinkedToMember, memberWithNoExistingPlayer.memberKey, memberWithNoExistingPlayer.memberName);

            Assert.Equal(memberWithNoExistingPlayer.memberKey, returnedPlayer.MemberKey);

            using (var connectionForAssert = _connectionFactory.CreateDatabaseConnection())
            {
                connectionForAssert.Open();

                var assignedMemberKey = await connectionForAssert.QuerySingleAsync<Guid>($"SELECT MemberKey FROM {Tables.Player} WHERE PlayerId = @PlayerId", playerCopy);
                Assert.Equal(memberWithNoExistingPlayer.memberKey, assignedMemberKey);

                var identitiesLinkedBy = await connectionForAssert.QueryAsync<string>($"SELECT LinkedBy FROM {Tables.PlayerIdentity} WHERE PlayerId = @PlayerId", playerCopy);
                foreach (var linkedBy in identitiesLinkedBy)
                {
                    Assert.Equal(PlayerIdentityLinkedBy.Member.ToString(), linkedBy);
                }
            }
        }

        [Fact]
        public async Task LinkPlayerToMemberAccount_where_member_has_linked_player_merges_players()
        {
            var memberWithExistingPlayer = _testData.AnyMemberLinkedToPlayer();
            var playerAlreadyLinked = _testData.Players.First(x => x.MemberKey == memberWithExistingPlayer.memberKey);
            var playerAlreadyLinkedCopy = new Player { PlayerId = playerAlreadyLinked.PlayerId, PlayerRoute = playerAlreadyLinked.PlayerRoute };
            _copier.Setup(x => x.CreateAuditableCopy(playerAlreadyLinked)).Returns(playerAlreadyLinkedCopy);

            var playerNotLinkedToMember = _testData.AnyPlayerNotLinkedToMember();
            var playerNotLinkedToMemberCopy = new Player { PlayerId = playerNotLinkedToMember.PlayerId, PlayerRoute = playerNotLinkedToMember.PlayerRoute };
            _copier.Setup(x => x.CreateAuditableCopy(playerNotLinkedToMember)).Returns(playerNotLinkedToMemberCopy);

            _routeSelector.Setup(x => x.SelectBestRoute(playerAlreadyLinked.PlayerRoute!, playerNotLinkedToMember.PlayerRoute!)).Returns(playerNotLinkedToMember.PlayerRoute!);

            var repo = CreateRepository();
            var returnedPlayer = await repo.LinkPlayerToMemberAccount(playerNotLinkedToMember, memberWithExistingPlayer.memberKey, memberWithExistingPlayer.memberName);
            await repo.ProcessAsyncUpdatesForPlayers();

            // The returned player should have the result of merging both players
            Assert.Equal(playerAlreadyLinked.PlayerId, returnedPlayer.PlayerId);
            Assert.Equal(playerNotLinkedToMember.PlayerRoute, returnedPlayer.PlayerRoute);
            Assert.Equal(memberWithExistingPlayer.memberKey, returnedPlayer.MemberKey);

            using (var connectionForAssert = _connectionFactory.CreateDatabaseConnection())
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

                var identitiesLinkedBy = await connectionForAssert.QueryAsync<string>($"SELECT LinkedBy FROM {Tables.PlayerIdentity} WHERE PlayerId = @PlayerId", playerAlreadyLinked);
                foreach (var linkedBy in identitiesLinkedBy)
                {
                    Assert.Equal(PlayerIdentityLinkedBy.Member.ToString(), linkedBy);
                }

                var obsoletePlayerShouldBeRemoved = await connectionForAssert.QuerySingleOrDefaultAsync<int>($"SELECT COUNT(PlayerId) FROM {Tables.Player} WHERE PlayerId = @PlayerId", playerNotLinkedToMember);
                Assert.Equal(0, obsoletePlayerShouldBeRemoved);
            }
        }

        [Fact]
        public async Task LinkPlayerToMemberAccount_where_member_has_linked_player_inserts_redirects()
        {
            var memberWithExistingPlayer = _testData.AnyMemberLinkedToPlayer();
            var playerAlreadyLinked = _testData.Players.First(x => x.MemberKey == memberWithExistingPlayer.memberKey);
            var playerAlreadyLinkedCopy = new Player { PlayerId = playerAlreadyLinked.PlayerId, PlayerRoute = playerAlreadyLinked.PlayerRoute };
            _copier.Setup(x => x.CreateAuditableCopy(playerAlreadyLinked)).Returns(playerAlreadyLinkedCopy);

            var playerNotLinkedToMember = _testData.AnyPlayerNotLinkedToMember();
            var playerNotLinkedToMemberCopy = new Player { PlayerId = playerNotLinkedToMember.PlayerId, PlayerRoute = playerNotLinkedToMember.PlayerRoute };
            _copier.Setup(x => x.CreateAuditableCopy(playerNotLinkedToMember)).Returns(playerNotLinkedToMemberCopy);

            _routeSelector.Setup(x => x.SelectBestRoute(playerAlreadyLinked.PlayerRoute!, playerNotLinkedToMember.PlayerRoute!)).Returns(playerNotLinkedToMember.PlayerRoute!);

            var repo = CreateRepository();
            await repo.LinkPlayerToMemberAccount(playerNotLinkedToMember, memberWithExistingPlayer.memberKey, memberWithExistingPlayer.memberName);

            VerifyPlayerIsRedirected(playerAlreadyLinked.PlayerRoute!, playerNotLinkedToMember.PlayerRoute!);
        }

        private void VerifyPlayerIsRedirected(string originalRoute, string revisedRoute)
        {
            _redirectsRepository.Verify(x => x.InsertRedirect(originalRoute, revisedRoute, null, It.IsAny<IDbTransaction>()), Times.Once);
            _redirectsRepository.Verify(x => x.InsertRedirect(originalRoute, revisedRoute, "/batting", It.IsAny<IDbTransaction>()), Times.Once);
            _redirectsRepository.Verify(x => x.InsertRedirect(originalRoute, revisedRoute, "/bowling", It.IsAny<IDbTransaction>()), Times.Once);
            _redirectsRepository.Verify(x => x.InsertRedirect(originalRoute, revisedRoute, "/fielding", It.IsAny<IDbTransaction>()), Times.Once);
            _redirectsRepository.Verify(x => x.InsertRedirect(originalRoute, revisedRoute, "/individual-scores", It.IsAny<IDbTransaction>()), Times.Once);
            _redirectsRepository.Verify(x => x.InsertRedirect(originalRoute, revisedRoute, "/bowling-figures", It.IsAny<IDbTransaction>()), Times.Once);
            _redirectsRepository.Verify(x => x.InsertRedirect(originalRoute, revisedRoute, "/catches", It.IsAny<IDbTransaction>()), Times.Once);
            _redirectsRepository.Verify(x => x.InsertRedirect(originalRoute, revisedRoute, "/run-outs", It.IsAny<IDbTransaction>()), Times.Once);
        }

        [Fact]
        public async Task LinkPlayerToMemberAccount_where_player_already_linked_throws_InvalidOperationException()
        {
            var playerLinkedToMember = _testData.AnyPlayerLinkedToMemberWithOnlyOneIdentity();
            var playerCopy = new Player { PlayerId = playerLinkedToMember.PlayerId };
            _copier.Setup(x => x.CreateAuditableCopy(playerLinkedToMember)).Returns(playerCopy);

            var repo = CreateRepository();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await repo.LinkPlayerToMemberAccount(playerLinkedToMember, Guid.NewGuid(), "Member name"));
        }

        [Fact]
        public async Task LinkPlayerToMemberAccount_where_member_has_no_previous_player_audits_and_logs()
        {
            var player = _testData.AnyPlayerNotLinkedToMember();
            var playerCopy = new Player { PlayerId = player.PlayerId };
            _copier.Setup(x => x.CreateAuditableCopy(player)).Returns(playerCopy);
            var memberNotLinkedToPlayer = _testData.AnyMemberNotLinkedToPlayer();

            var repo = CreateRepository();
            await repo.LinkPlayerToMemberAccount(player, memberNotLinkedToPlayer.memberKey, memberNotLinkedToPlayer.memberName);

            _auditRepository.Verify(x => x.CreateAudit(It.Is<AuditRecord>(x => x.Action == AuditAction.Update && x.EntityUri!.ToString().EndsWith(player.PlayerId.ToString()!)), It.IsAny<IDbTransaction>()), Times.Once);
            _logger.Verify(x => x.Info(LoggingTemplates.Updated, JsonConvert.SerializeObject(playerCopy), memberNotLinkedToPlayer.memberName, memberNotLinkedToPlayer.memberKey, typeof(SqlServerPlayerRepository), nameof(SqlServerPlayerRepository.LinkPlayerToMemberAccount)));
        }

        [Fact]
        public async Task LinkPlayerToMemberAccount_where_member_has_linked_player_audits_and_logs()
        {
            var memberWithExistingPlayer = _testData.AnyMemberLinkedToPlayer();
            var playerAlreadyLinked = _testData.Players.First(x => x.MemberKey == memberWithExistingPlayer.memberKey);
            var playerAlreadyLinkedCopy = new Player { PlayerId = playerAlreadyLinked.PlayerId, PlayerRoute = playerAlreadyLinked.PlayerRoute };
            _copier.Setup(x => x.CreateAuditableCopy(playerAlreadyLinked)).Returns(playerAlreadyLinkedCopy);

            var playerNotLinkedToMember = _testData.AnyPlayerNotLinkedToMember();
            var playerNotLinkedToMemberCopy = new Player { PlayerId = playerNotLinkedToMember.PlayerId, PlayerRoute = playerNotLinkedToMember.PlayerRoute };
            _copier.Setup(x => x.CreateAuditableCopy(playerNotLinkedToMember)).Returns(playerNotLinkedToMemberCopy);

            _routeSelector.Setup(x => x.SelectBestRoute(playerAlreadyLinked.PlayerRoute!, playerNotLinkedToMember.PlayerRoute!)).Returns(playerNotLinkedToMember.PlayerRoute!);

            var repo = CreateRepository();
            await repo.LinkPlayerToMemberAccount(playerNotLinkedToMember, memberWithExistingPlayer.memberKey, memberWithExistingPlayer.memberName);

            _auditRepository.Verify(x => x.CreateAudit(It.Is<AuditRecord>(x => x.Action == AuditAction.Delete && x.EntityUri!.ToString().EndsWith(playerNotLinkedToMember.PlayerId.ToString()!)), It.IsAny<IDbTransaction>()), Times.Once);
            _logger.Verify(x => x.Info(LoggingTemplates.Deleted, It.Is<string>(x => x.Contains(playerNotLinkedToMember.PlayerId.ToString()!)), memberWithExistingPlayer.memberName, memberWithExistingPlayer.memberKey, typeof(SqlServerPlayerRepository), nameof(SqlServerPlayerRepository.LinkPlayerToMemberAccount)));

            _auditRepository.Verify(x => x.CreateAudit(It.Is<AuditRecord>(x => x.Action == AuditAction.Update && x.EntityUri!.ToString().EndsWith(playerAlreadyLinked.PlayerId.ToString()!)), It.IsAny<IDbTransaction>()), Times.Once);
            _logger.Verify(x => x.Info(LoggingTemplates.Updated, It.Is<string>(x => x.Contains(playerAlreadyLinked.PlayerId.ToString()!)), memberWithExistingPlayer.memberName, memberWithExistingPlayer.memberKey, typeof(SqlServerPlayerRepository), nameof(SqlServerPlayerRepository.LinkPlayerToMemberAccount)));
        }
        #endregion

        #region LinkPlayerIdentity
        [Fact]
        public async Task LinkPlayerIdentity_throws_InvalidOperationException_if_identity_is_already_linked_to_target_player()
        {
            var player = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity();
            SetupMocksForLinkPlayerIdentity(player, player);

            var repo = CreateRepository();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await repo.LinkPlayerIdentity(player.PlayerId!.Value, player.PlayerIdentities[0].PlayerIdentityId!.Value, PlayerIdentityLinkedBy.ClubOrTeam, Guid.NewGuid(), "Member name"));
        }

        [Fact]
        public async Task LinkPlayerIdentity_throws_InvalidOperationException_if_both_players_are_linked_to_a_member()
        {
            var player1 = _testData.AnyPlayerLinkedToMemberWithOnlyOneIdentity(p => _testData.Players.Any(p2 => p2.MemberKey.HasValue && p2.PlayerIdentities.Count == 1 && p2.PlayerId != p.PlayerId && p2.IsOnTheSameTeamAs(p)));
            var player2 = _testData.AnyPlayerLinkedToMemberWithOnlyOneIdentity(player2 => player2.PlayerId != player1.PlayerId && player1.IsOnTheSameTeamAs(player2));
            SetupMocksForLinkPlayerIdentity(player1, player2);

            var repo = CreateRepository();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await repo.LinkPlayerIdentity(player1.PlayerId!.Value, player2.PlayerIdentities[0].PlayerIdentityId!.Value, PlayerIdentityLinkedBy.ClubOrTeam, Guid.NewGuid(), "Member name"));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task LinkPlayerIdentity_throws_InvalidOperationException_if_target_player_is_linked_to_a_member_unless_it_is_the_current_member(bool isCurrentMember)
        {
            var repo = CreateRepository();
            if (isCurrentMember)
            {
                var player1 = _testData.AnyPlayerLinkedToMemberWithOnlyOneIdentity(p => _testData.Players.Any(p2 => p2.MemberKey is null && p2.IsOnTheSameTeamAs(p)));
                var player2 = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity(p => p.IsOnTheSameTeamAs(player1));
                var currentMember = player1.MemberKey!.Value;
                SetupMocksForLinkPlayerIdentity(player1, player2);

                var exception = await Record.ExceptionAsync(async () => await repo.LinkPlayerIdentity(player1.PlayerId!.Value, player2.PlayerIdentities[0].PlayerIdentityId!.Value, PlayerIdentityLinkedBy.ClubOrTeam, currentMember, "Member name"));
                Assert.Null(exception);
            }
            else
            {
                var currentMember = _testData.AnyMemberLinkedToPlayer();
                var player1 = _testData.AnyPlayerLinkedToMemberWithOnlyOneIdentity(p => p.MemberKey != currentMember.memberKey && _testData.Players.Any(p2 => p2.MemberKey is null && p2.IsOnTheSameTeamAs(p)));
                var player2 = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity(p => p.IsOnTheSameTeamAs(player1));
                SetupMocksForLinkPlayerIdentity(player1, player2);

                await Assert.ThrowsAsync<InvalidOperationException>(async () => await repo.LinkPlayerIdentity(player1.PlayerId!.Value, player2.PlayerIdentities[0].PlayerIdentityId!.Value, PlayerIdentityLinkedBy.ClubOrTeam, currentMember.memberKey, currentMember.memberName));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task LinkPlayerIdentity_throws_InvalidOperationException_if_identity_to_link_is_linked_to_a_member_unless_it_is_the_current_member(bool isCurrentMember)
        {

            var repo = CreateRepository();
            if (isCurrentMember)
            {
                var player1 = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity(p => _testData.Players.Any(p2 => p2.MemberKey is not null && p2.PlayerIdentities.Count == 1 && p2.IsOnTheSameTeamAs(p)));
                var player2 = _testData.AnyPlayerLinkedToMemberWithOnlyOneIdentity(p => p.MemberKey is not null && p.IsOnTheSameTeamAs(player1));
                var currentMember = player2.MemberKey!.Value;
                SetupMocksForLinkPlayerIdentity(player1, player2);

                var exception = await Record.ExceptionAsync(async () => await repo.LinkPlayerIdentity(player1.PlayerId!.Value, player2.PlayerIdentities[0].PlayerIdentityId!.Value, PlayerIdentityLinkedBy.ClubOrTeam, currentMember, "Member name"));
                Assert.Null(exception);
            }
            else
            {
                var currentMember = _testData.AnyMemberLinkedToPlayer();
                var player1 = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity(p => _testData.Players.Any(p2 => p2.MemberKey.HasValue && p2.MemberKey != currentMember.memberKey && p2.PlayerIdentities.Count == 1 && p2.IsOnTheSameTeamAs(p))); ;
                var player2 = _testData.AnyPlayerLinkedToMemberWithOnlyOneIdentity(p => p.MemberKey != currentMember.memberKey && p.IsOnTheSameTeamAs(player1));
                SetupMocksForLinkPlayerIdentity(player1, player2);

                await Assert.ThrowsAsync<InvalidOperationException>(async () => await repo.LinkPlayerIdentity(player1.PlayerId!.Value, player2.PlayerIdentities[0].PlayerIdentityId!.Value, PlayerIdentityLinkedBy.ClubOrTeam, currentMember.memberKey, currentMember.memberName));
            }
        }

        [Fact]
        public async Task LinkPlayerIdentity_throws_InvalidOperationException_if_identity_to_link_is_linked_to_other_identities()
        {
            var player1 = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity();
            var player2 = _testData.AnyPlayerNotLinkedToMemberWithMultipleIdentities(p => p.IsOnTheSameTeamAs(player1));
            SetupMocksForLinkPlayerIdentity(player1, player2);

            var repo = CreateRepository();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await repo.LinkPlayerIdentity(player1.PlayerId!.Value, player2.PlayerIdentities[0].PlayerIdentityId!.Value, PlayerIdentityLinkedBy.ClubOrTeam, Guid.NewGuid(), "Member name"));
        }

        [Fact]
        public async Task LinkPlayerIdentity_throws_InvalidOperationException_if_target_player_does_not_have_an_identity_on_the_same_team_as_identity_to_link()
        {
            var player1 = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity();
            var player2 = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity(p => !p.IsOnTheSameTeamAs(player1));
            SetupMocksForLinkPlayerIdentity(player1, player2);

            var repo = CreateRepository();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await repo.LinkPlayerIdentity(player1.PlayerId!.Value, player2.PlayerIdentities[0].PlayerIdentityId!.Value, PlayerIdentityLinkedBy.ClubOrTeam, Guid.NewGuid(), "Member name"));
        }

        [Fact]
        public async Task LinkPlayerIdentity_merges_players()
        {
            var player1 = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity();
            var player2 = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity(p => p.PlayerId != player1.PlayerId);

            SetupMocksForLinkPlayerIdentity(player1, player2);

            var member = _testData.AnyMemberNotLinkedToPlayer();

            var repo = CreateRepository();
            var movedIdentityResult = await repo.LinkPlayerIdentity(player1.PlayerId!.Value, player2.PlayerIdentities[0].PlayerIdentityId!.Value, PlayerIdentityLinkedBy.ClubOrTeam, member.memberKey, member.memberName);
            await repo.ProcessAsyncUpdatesForPlayers();

            Assert.Equal(player1.PlayerId, movedIdentityResult.PlayerIdForTargetPlayer);
            Assert.Equal(player1.PlayerRoute, movedIdentityResult.PreviousRouteForTargetPlayer);
            Assert.Equal(player1.MemberKey, movedIdentityResult.MemberKeyForTargetPlayer);

            Assert.Equal(player2.PlayerId, movedIdentityResult.PlayerIdForSourcePlayer);
            Assert.Equal(player2.PlayerRoute, movedIdentityResult.PreviousRouteForSourcePlayer);
            Assert.Equal(player2.MemberKey, movedIdentityResult.MemberKeyForSourcePlayer);

            Assert.Equal(player2.PlayerRoute, movedIdentityResult.NewRouteForTargetPlayer);

            using (var connectionForAssert = _connectionFactory.CreateDatabaseConnection())
            {
                connectionForAssert.Open();

                var player1UpdatedRoute = await connectionForAssert.QuerySingleAsync<string>($"SELECT PlayerRoute FROM {Tables.Player} WHERE PlayerId = @PlayerId", player1);
                Assert.Equal(player2.PlayerRoute, player1UpdatedRoute);

                var expectedIdentities = player1.PlayerIdentities.Select(pi => pi).ToList();
                expectedIdentities.Add(player2.PlayerIdentities[0]);
                foreach (var identity in expectedIdentities)
                {
                    var playerIdForIdentity = await connectionForAssert.QuerySingleAsync<Guid>($"SELECT PlayerId FROM {Tables.PlayerIdentity} WHERE PlayerIdentityId = @PlayerIdentityId", identity);
                    Assert.Equal(player1.PlayerId, playerIdForIdentity);

                    var playerIdsInStatistics = await connectionForAssert.QueryAsync<Guid>($"SELECT PlayerId FROM {Tables.PlayerInMatchStatistics} WHERE PlayerIdentityId = @PlayerIdentityId", identity);
                    foreach (var playerIdInStatistics in playerIdsInStatistics)
                    {
                        Assert.Equal(player1.PlayerId, playerIdInStatistics);
                    }

                    var playerRoutesInStatistics = await connectionForAssert.QueryAsync<string>($"SELECT PlayerRoute FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = @PlayerId", player1);
                    foreach (var route in playerRoutesInStatistics)
                    {
                        Assert.Equal(player2.PlayerRoute, route);
                    }
                }

                var movedIdentityLinkedBy = await connectionForAssert.QuerySingleAsync<string>($"SELECT LinkedBy FROM {Tables.PlayerIdentity} WHERE PlayerIdentityId = @PlayerIdentityId", player2.PlayerIdentities[0]);
                Assert.Equal(PlayerIdentityLinkedBy.ClubOrTeam.ToString(), movedIdentityLinkedBy);

                var obsoletePlayerShouldBeRemoved = await connectionForAssert.QuerySingleOrDefaultAsync<int>($"SELECT COUNT(PlayerId) FROM {Tables.Player} WHERE PlayerId = @PlayerId", player2);
                Assert.Equal(0, obsoletePlayerShouldBeRemoved);
            }
        }

        [Fact]
        public async Task LinkPlayerIdentity_redirects_deleted_player_to_linked_player()
        {
            var player1 = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity();
            var player2 = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity(p => p.PlayerId != player1.PlayerId);
            SetupMocksForLinkPlayerIdentity(player1, player2);

            var member = _testData.AnyMemberNotLinkedToPlayer();

            var repo = CreateRepository();
            await repo.LinkPlayerIdentity(player1.PlayerId!.Value, player2.PlayerIdentities[0].PlayerIdentityId!.Value, PlayerIdentityLinkedBy.ClubOrTeam, member.memberKey, member.memberName).ConfigureAwait(false);

            VerifyPlayerIsRedirected(player1.PlayerRoute!, player2.PlayerRoute!);
        }

        [Fact]
        public async Task LinkPlayerIdentity_audits_and_logs()
        {
            var player1 = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity();
            var player2 = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity(p => p.PlayerId != player1.PlayerId);

            SetupMocksForLinkPlayerIdentity(player1, player2);

            var member = _testData.AnyMemberNotLinkedToPlayer();

            var repo = CreateRepository();
            await repo.LinkPlayerIdentity(player1.PlayerId!.Value, player2.PlayerIdentities[0].PlayerIdentityId!.Value, PlayerIdentityLinkedBy.ClubOrTeam, member.memberKey, member.memberName).ConfigureAwait(false);

            // audits and logs delete of original player and update of target player
            _auditRepository.Verify(x => x.CreateAudit(It.Is<AuditRecord>(x => x.Action == AuditAction.Update && x.EntityUri!.ToString().EndsWith(player1.PlayerId.ToString()!)), It.IsAny<IDbTransaction>()), Times.Once);
            _logger.Verify(x => x.Info(LoggingTemplates.Updated, It.IsAny<string>(), member.memberName, member.memberKey, typeof(SqlServerPlayerRepository), nameof(SqlServerPlayerRepository.LinkPlayerIdentity)));

            var deletedPlayer = new Player { PlayerId = player2.PlayerId, PlayerRoute = player2.PlayerRoute };
            deletedPlayer.PlayerIdentities.Add(new PlayerIdentity { PlayerIdentityId = player2.PlayerIdentities[0].PlayerIdentityId });
            _auditRepository.Verify(x => x.CreateAudit(It.Is<AuditRecord>(x => x.Action == AuditAction.Delete && x.EntityUri!.ToString().EndsWith(player2.PlayerId.ToString()!)), It.IsAny<IDbTransaction>()), Times.Once);
            _logger.Verify(x => x.Info(LoggingTemplates.Deleted, JsonConvert.SerializeObject(deletedPlayer), member.memberName, member.memberKey, typeof(SqlServerPlayerRepository), nameof(SqlServerPlayerRepository.LinkPlayerIdentity)));
        }

        private void SetupMocksForLinkPlayerIdentity(Player player1, Player player2)
        {
            _routeSelector.Setup(x => x.SelectBestRoute(player2.PlayerRoute!, player1.PlayerRoute!)).Returns(player2.PlayerRoute!);
        }
        #endregion

        #region UnlinkPlayerIdentityFromMemberAccount
        [Fact]
        public async Task UnlinkPlayerIdentityFromMemberAccount_for_penultimate_identity_moves_identity_to_new_player_including_statistics()
        {
            var player = _testData.AnyPlayerLinkedToMemberWithMultipleIdentities();
            var playerCopy = new Player { PlayerId = player.PlayerId };
            _copier.Setup(x => x.CreateAuditableCopy(player)).Returns(playerCopy);
            var playerIdentityToUnlink = player.PlayerIdentities.First();
            var member = _testData.Members.Single(x => x.memberKey == player.MemberKey);

            var generatedPlayerRoute = "/players/" + Guid.NewGuid().ToString();
            _routeGenerator.Setup(x => x.GenerateUniqueRoute("/players", playerIdentityToUnlink.PlayerIdentityName!, NoiseWords.PlayerRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(generatedPlayerRoute));

            var repo = CreateRepository();

            await repo.UnlinkPlayerIdentityFromMemberAccount(playerIdentityToUnlink, member.memberKey, member.memberName);
            await repo.ProcessAsyncUpdatesForPlayers();

            using (var connectionForAssert = _connectionFactory.CreateDatabaseConnection())
            {
                connectionForAssert.Open();

                var originalPlayerStillLinkedToMember = await connectionForAssert.QuerySingleAsync<Guid?>($"SELECT MemberKey FROM {Tables.Player} WHERE PlayerId = @PlayerId", player);
                Assert.Equal(member.memberKey, originalPlayerStillLinkedToMember);

                var newPlayerLinkedToIdentity = await connectionForAssert.QuerySingleAsync<(Guid? playerId, string route, Guid? memberKey, PlayerIdentityLinkedBy linkedBy)>(
                    $@"SELECT p.PlayerId, p.PlayerRoute, p.MemberKey, p.LinkedBy
                       FROM {Views.PlayerIdentity} p 
                       WHERE PlayerIdentityId = @PlayerIdentityId", playerIdentityToUnlink);
                Assert.NotEqual(player.PlayerId, newPlayerLinkedToIdentity.playerId);
                Assert.Equal(generatedPlayerRoute, newPlayerLinkedToIdentity.route);
                Assert.Null(newPlayerLinkedToIdentity.memberKey);
                Assert.Equal(PlayerIdentityLinkedBy.DefaultIdentity, newPlayerLinkedToIdentity.linkedBy);

                var statisticsForIdentity = await connectionForAssert.QueryAsync<(Guid? playerId, string route)>($"SELECT PlayerId, PlayerRoute FROM {Tables.PlayerInMatchStatistics} WHERE PlayerIdentityId = @PlayerIdentityId", playerIdentityToUnlink);
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
            var player = _testData.AnyPlayerLinkedToMemberWithMultipleIdentities();
            var playerCopy = new Player { PlayerId = player.PlayerId };
            _copier.Setup(x => x.CreateAuditableCopy(player)).Returns(playerCopy);
            var playerIdentity = player.PlayerIdentities.First();
            var member = _testData.Members.Single(x => x.memberKey == player.MemberKey);

            var generatedPlayerRoute = "/players/" + Guid.NewGuid().ToString();
            _routeGenerator.Setup(x => x.GenerateUniqueRoute("/players", playerIdentity.PlayerIdentityName!, NoiseWords.PlayerRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(generatedPlayerRoute));

            var repo = CreateRepository();

            await repo.UnlinkPlayerIdentityFromMemberAccount(playerIdentity, member.memberKey, member.memberName);

            _auditRepository.Verify(x => x.CreateAudit(It.IsAny<AuditRecord>(), It.IsAny<IDbTransaction>()), Times.Once);
            _logger.Verify(x => x.Info(LoggingTemplates.Created, It.IsAny<string>(), member.memberName, member.memberKey, typeof(SqlServerPlayerRepository), nameof(SqlServerPlayerRepository.UnlinkPlayerIdentityFromMemberAccount)));
        }

        [Fact]
        public async Task UnlinkPlayerIdentityFromMemberAccount_for_last_identity_unlinks_player_from_member()
        {
            var player = _testData.AnyPlayerLinkedToMemberWithOnlyOneIdentity();
            var member = _testData.Members.Single(x => x.memberKey == player.MemberKey);

            var repo = CreateRepository();

            await repo.UnlinkPlayerIdentityFromMemberAccount(player.PlayerIdentities.First(), member.memberKey, member.memberName);

            using (var connectionForAssert = _connectionFactory.CreateDatabaseConnection())
            {
                connectionForAssert.Open();
                var result = await connectionForAssert.QuerySingleAsync<(Guid? MemberKey, PlayerIdentityLinkedBy LinkedBy)>($"SELECT MemberKey, LinkedBy FROM {Views.PlayerIdentity} WHERE PlayerId = @PlayerId", player);

                Assert.Null(result.MemberKey);
                Assert.Equal(PlayerIdentityLinkedBy.DefaultIdentity, result.LinkedBy);
            }
        }

        [Fact]
        public async Task UnlinkPlayerIdentityFromMemberAccount_for_last_identity_audits_and_logs()
        {
            var player = _testData.AnyPlayerLinkedToMemberWithOnlyOneIdentity();
            var member = _testData.Members.Single(x => x.memberKey == player.MemberKey);

            var repo = CreateRepository();

            await repo.UnlinkPlayerIdentityFromMemberAccount(player.PlayerIdentities.First(), member.memberKey, member.memberName);

            _auditRepository.Verify(x => x.CreateAudit(It.IsAny<AuditRecord>(), It.IsAny<IDbTransaction>()), Times.Once);
            _logger.Verify(x => x.Info(LoggingTemplates.Updated, It.IsAny<string>(), member.memberName, member.memberKey, typeof(SqlServerPlayerRepository), nameof(SqlServerPlayerRepository.UnlinkPlayerIdentityFromMemberAccount)));
        }
        #endregion

        #region UnlinkPlayerIdentity

        [Fact]
        public async Task UnlinkPlayerIdentity_throws_InvalidOperationException_for_last_identity()
        {
            var player = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity();

            var repo = CreateRepository();

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await repo.UnlinkPlayerIdentity(player.PlayerIdentities.First().PlayerIdentityId!.Value, Guid.NewGuid(), "Member name"));
        }

        [Fact]
        public async Task UnlinkPlayerIdentity_for_penultimate_identity_moves_identity_to_new_player_including_statistics()
        {
            var player = _testData.AnyPlayerNotLinkedToMemberWithMultipleIdentities(p => p.PlayerIdentities.Count == 2);
            var identityToKeep = player.PlayerIdentities[0];
            var identityToUnlink = player.PlayerIdentities[1];

            var generatedPlayerRoute = "/players/" + Guid.NewGuid().ToString();
            _routeGenerator.Setup(x => x.GenerateUniqueRoute("/players", identityToUnlink.PlayerIdentityName!, NoiseWords.PlayerRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(generatedPlayerRoute));

            var repo = CreateRepository();

            await repo.UnlinkPlayerIdentity(identityToUnlink.PlayerIdentityId!.Value, Guid.NewGuid(), "Member name");
            await repo.ProcessAsyncUpdatesForPlayers();

            using (var connectionForAssert = _connectionFactory.CreateDatabaseConnection())
            {
                connectionForAssert.Open();

                var originalPlayerStillLinked = await connectionForAssert.ExecuteScalarAsync<Guid>($"SELECT PlayerId FROM {Tables.PlayerIdentity} WHERE PlayerIdentityId = @PlayerIdentityId", identityToKeep).ConfigureAwait(false);
                Assert.Equal(player.PlayerId, originalPlayerStillLinked);

                var newPlayerLinkedToIdentity = await connectionForAssert.QuerySingleAsync<(Guid? playerId, string route, PlayerIdentityLinkedBy linkedBy)>(
                    $@"SELECT p.PlayerId, p.PlayerRoute, p.LinkedBy
                       FROM {Views.PlayerIdentity} p 
                       WHERE PlayerIdentityId = @PlayerIdentityId", identityToUnlink).ConfigureAwait(false);
                Assert.NotEqual(player.PlayerId, newPlayerLinkedToIdentity.playerId);
                Assert.Equal(generatedPlayerRoute, newPlayerLinkedToIdentity.route);
                Assert.Equal(PlayerIdentityLinkedBy.DefaultIdentity, newPlayerLinkedToIdentity.linkedBy);

                var statisticsForIdentity = await connectionForAssert.QueryAsync<(Guid? playerId, string route)>($"SELECT PlayerId, PlayerRoute FROM {Tables.PlayerInMatchStatistics} WHERE PlayerIdentityId = @PlayerIdentityId", identityToUnlink);
                foreach (var row in statisticsForIdentity)
                {
                    Assert.Equal(newPlayerLinkedToIdentity.playerId, row.playerId);
                    Assert.Equal(generatedPlayerRoute, row.route);
                }
            }
        }

        [Fact]
        public async Task UnlinkPlayerIdentity_for_penultimate_identity_audits_and_logs()
        {
            var player = _testData.AnyPlayerNotLinkedToMemberWithMultipleIdentities(p => p.PlayerIdentities.Count == 2);
            var playerIdentity = player.PlayerIdentities[0];
            var member = _testData.AnyMemberNotLinkedToPlayer();

            var generatedPlayerRoute = "/players/" + Guid.NewGuid().ToString();
            _routeGenerator.Setup(x => x.GenerateUniqueRoute("/players", playerIdentity.PlayerIdentityName!, NoiseWords.PlayerRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(generatedPlayerRoute));

            var repo = CreateRepository();

            await repo.UnlinkPlayerIdentity(playerIdentity.PlayerIdentityId!.Value, member.memberKey, member.memberName);

            _auditRepository.Verify(x => x.CreateAudit(It.IsAny<AuditRecord>(), It.IsAny<IDbTransaction>()), Times.Once);
            _logger.Verify(x => x.Info(LoggingTemplates.Created, It.IsAny<string>(), member.memberName, member.memberKey, typeof(SqlServerPlayerRepository), nameof(SqlServerPlayerRepository.UnlinkPlayerIdentity)));
        }

        #endregion

        #region UpdatePlayerIdentity

        [Fact]
        public async Task UpdatePlayerIdentity_where_name_matches_another_player_identity_not_updated_and_returns_NotUnique()
        {
            var repo = CreateRepository();
            var identities = _testData.AnyTwoIdentitiesFromTheSameTeam();

            var updatedPlayerIdentity = new PlayerIdentity
            {
                PlayerIdentityId = identities.firstIdentity.PlayerIdentityId,
                PlayerIdentityName = identities.secondIdentity.PlayerIdentityName,
                Team = identities.firstIdentity.Team,
                Player = identities.firstIdentity.Player
            };

            var result = await repo.UpdatePlayerIdentity(updatedPlayerIdentity, Guid.NewGuid(), "Member name");

            Assert.Equal(PlayerIdentityUpdateResult.NotUnique, result.Status);

            using (var connectionForAssert = _connectionFactory.CreateDatabaseConnection())
            {
                connectionForAssert.Open();
                var playerToUpdateNameAfter = await connectionForAssert.ExecuteScalarAsync<string>($"SELECT PlayerIdentityName FROM {Tables.PlayerIdentity} WHERE PlayerIdentityId = @PlayerIdentityId", identities.firstIdentity).ConfigureAwait(false);
                Assert.Equal(identities.firstIdentity.PlayerIdentityName, playerToUpdateNameAfter);
            }
        }

        [Fact]
        public async Task UpdatePlayerIdentity_where_name_matches_same_player_identity_returns_Success()
        {
            var repo = CreateRepository();

            var playerIdentityToUpdate = _testData.PlayerIdentities[0];
            var playerIdentityToUpdateCopy = SetupCopyOfPlayerIdentity(playerIdentityToUpdate, playerIdentityToUpdate.PlayerIdentityName!);

            _copier.Setup(x => x.CreateAuditableCopy(playerIdentityToUpdate.Player)).Returns(new Player { PlayerId = playerIdentityToUpdate.Player?.PlayerId });
            _copier.Setup(x => x.CreateAuditableCopy(playerIdentityToUpdate)).Returns(playerIdentityToUpdateCopy);

            SetupRouteGenerator(playerIdentityToUpdate, playerIdentityToUpdateCopy, playerIdentityToUpdate.RouteSegment!, "/players/new-route");

            var result = await repo.UpdatePlayerIdentity(playerIdentityToUpdate, Guid.NewGuid(), "Member name");

            Assert.Equal(PlayerIdentityUpdateResult.Success, result.Status);
        }


        [Fact]
        public async Task UpdatePlayerIdentity_where_name_does_not_match_existing_player_identity_updates_name_including_statistics_and_returns_Success()
        {
            var repo = CreateRepository();

            var playerIdentityToUpdate = _testData.PlayerIdentities.First(x =>
                                                _testData.PlayerInnings.Any(pi => pi.Batter?.PlayerIdentityId == x.PlayerIdentityId) &&
                                                _testData.PlayerInnings.Any(pi => pi.Bowler?.PlayerIdentityId == x.PlayerIdentityId) &&
                                                _testData.PlayerInnings.Any(pi => (pi.DismissedBy?.PlayerIdentityId == x.PlayerIdentityId && pi.DismissalType == DismissalType.Caught) ||
                                                                                                  (pi.Bowler?.PlayerIdentityId == x.PlayerIdentityId && pi.DismissalType == DismissalType.CaughtAndBowled)) &&
                                                _testData.PlayerInnings.Any(pi => pi.DismissedBy?.PlayerIdentityId == x.PlayerIdentityId && pi.DismissalType == DismissalType.RunOut));

            var updatedPlayerIdentity = SetupCopyOfPlayerIdentity(playerIdentityToUpdate, Guid.NewGuid().ToString());
            SetupRouteGenerator(playerIdentityToUpdate, updatedPlayerIdentity, updatedPlayerIdentity.PlayerIdentityName.Kebaberize(), "/players/new-route");

            var result = await repo.UpdatePlayerIdentity(updatedPlayerIdentity, Guid.NewGuid(), "Member name");
            await repo.ProcessAsyncUpdatesForPlayers();

            Assert.Equal(PlayerIdentityUpdateResult.Success, result.Status);

            using (var connectionForAssert = _connectionFactory.CreateDatabaseConnection())
            {
                connectionForAssert.Open();
                var playerToUpdateNameAfter = await connectionForAssert.QuerySingleAsync<(string playerIdentityName, string comparableName, string routeSegment)>(
                    $"SELECT PlayerIdentityName, ComparableName, RouteSegment FROM {Tables.PlayerIdentity} WHERE PlayerIdentityId = @PlayerIdentityId", playerIdentityToUpdate).ConfigureAwait(false);
                Assert.Equal(updatedPlayerIdentity.PlayerIdentityName, playerToUpdateNameAfter.playerIdentityName);
                Assert.Equal(updatedPlayerIdentity.ComparableName(), playerToUpdateNameAfter.comparableName);
                Assert.Equal(updatedPlayerIdentity.PlayerIdentityName.Kebaberize(), playerToUpdateNameAfter.routeSegment);

                var playerToUpdateNameInStatistics = await connectionForAssert.QueryAsync<string>($"SELECT PlayerIdentityName FROM {Tables.PlayerInMatchStatistics} WHERE PlayerIdentityId = @PlayerIdentityId", playerIdentityToUpdate).ConfigureAwait(false);
                foreach (var nameInStatistics in playerToUpdateNameInStatistics)
                {
                    Assert.Equal(updatedPlayerIdentity.PlayerIdentityName, nameInStatistics);
                }

                playerToUpdateNameInStatistics = await connectionForAssert.QueryAsync<string>($"SELECT BowledByPlayerIdentityName FROM {Tables.PlayerInMatchStatistics} WHERE BowledByPlayerIdentityId = @PlayerIdentityId", playerIdentityToUpdate).ConfigureAwait(false);
                foreach (var nameInStatistics in playerToUpdateNameInStatistics)
                {
                    Assert.Equal(updatedPlayerIdentity.PlayerIdentityName, nameInStatistics);
                }

                playerToUpdateNameInStatistics = await connectionForAssert.QueryAsync<string>($"SELECT CaughtByPlayerIdentityName FROM {Tables.PlayerInMatchStatistics} WHERE CaughtByPlayerIdentityId = @PlayerIdentityId", playerIdentityToUpdate).ConfigureAwait(false);
                foreach (var nameInStatistics in playerToUpdateNameInStatistics)
                {
                    Assert.Equal(updatedPlayerIdentity.PlayerIdentityName, nameInStatistics);
                }

                playerToUpdateNameInStatistics = await connectionForAssert.QueryAsync<string>($"SELECT RunOutByPlayerIdentityName FROM {Tables.PlayerInMatchStatistics} WHERE RunOutByPlayerIdentityId = @PlayerIdentityId", playerIdentityToUpdate).ConfigureAwait(false);
                foreach (var nameInStatistics in playerToUpdateNameInStatistics)
                {
                    Assert.Equal(updatedPlayerIdentity.PlayerIdentityName, nameInStatistics);
                }
            }

            _playerNameFormatter.Verify(x => x.CapitaliseName(updatedPlayerIdentity.PlayerIdentityName!), Times.Once);
        }

        [Fact]
        public async Task UpdatePlayerIdentity_where_name_does_not_match_existing_player_identity_leaves_route_unchanged_if_still_appropriate()
        {
            var repo = CreateRepository();

            var playerIdentityToUpdate = _testData.PlayerIdentities.First();
            var updatedPlayerIdentity = SetupCopyOfPlayerIdentity(playerIdentityToUpdate, Guid.NewGuid().ToString());
            var updatedPlayerRoute = "/players/new-route";
            SetupRouteGenerator(playerIdentityToUpdate, updatedPlayerIdentity, updatedPlayerIdentity.PlayerIdentityName.Kebaberize(), updatedPlayerRoute);

            var nameOfAnyPlayerIdentityFollowingTheUpdate = updatedPlayerIdentity.PlayerIdentityName!;
            _routeGenerator.Setup(x => x.GenerateRoute("/players", nameOfAnyPlayerIdentityFollowingTheUpdate, NoiseWords.PlayerRoute)).Returns(playerIdentityToUpdate.Player!.PlayerRoute!);
            _routeGenerator.Setup(x => x.IsMatchingRoute(playerIdentityToUpdate.Player!.PlayerRoute!, playerIdentityToUpdate.Player!.PlayerRoute!)).Returns(true);

            _ = await repo.UpdatePlayerIdentity(updatedPlayerIdentity, Guid.NewGuid(), "Member name");

            using (var connectionForAssert = _connectionFactory.CreateDatabaseConnection())
            {
                connectionForAssert.Open();
                var playerRouteAfter = await connectionForAssert.QuerySingleAsync<string>($"SELECT PlayerRoute FROM {Tables.Player} WHERE PlayerId = @PlayerId", new { playerIdentityToUpdate.Player!.PlayerId }).ConfigureAwait(false);
                Assert.Equal(playerIdentityToUpdate.Player.PlayerRoute, playerRouteAfter);

                _redirectsRepository.Verify(x => x.InsertRedirect(playerIdentityToUpdate.Player.PlayerRoute!, updatedPlayerRoute, null, It.IsAny<IDbTransaction>()), Times.Never);
            }
        }

        [Fact]
        public async Task UpdatePlayerIdentity_where_name_does_not_match_existing_player_identity_updates_route_and_redirects()
        {
            var repo = CreateRepository();

            var playerIdentityToUpdate = _testData.PlayerIdentities.First();
            var updatedPlayerIdentity = SetupCopyOfPlayerIdentity(playerIdentityToUpdate, Guid.NewGuid().ToString());
            var updatedPlayerRoute = "/players/new-route";
            SetupRouteGenerator(playerIdentityToUpdate, updatedPlayerIdentity, updatedPlayerIdentity.PlayerIdentityName.Kebaberize(), updatedPlayerRoute);

            _ = await repo.UpdatePlayerIdentity(updatedPlayerIdentity, Guid.NewGuid(), "Member name");

            using (var connectionForAssert = _connectionFactory.CreateDatabaseConnection())
            {
                connectionForAssert.Open();
                var playerRouteAfter = await connectionForAssert.QuerySingleAsync<string>($"SELECT PlayerRoute FROM {Tables.Player} WHERE PlayerId = @PlayerId", new { playerIdentityToUpdate.Player!.PlayerId }).ConfigureAwait(false);
                Assert.Equal(updatedPlayerRoute, playerRouteAfter);

                _redirectsRepository.Verify(x => x.InsertRedirect(playerIdentityToUpdate.Player.PlayerRoute!, updatedPlayerRoute, null, It.IsAny<IDbTransaction>()), Times.Once);
            }
        }
        #endregion
    }
}
