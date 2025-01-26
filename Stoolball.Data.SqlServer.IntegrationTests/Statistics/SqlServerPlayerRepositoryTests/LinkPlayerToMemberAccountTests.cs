using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Moq;
using Newtonsoft.Json;
using Stoolball.Data.Abstractions;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Data.SqlServer.IntegrationTests.Redirects;
using Stoolball.Logging;
using Stoolball.Routing;
using Stoolball.Statistics;
using Stoolball.Testing;
using Xunit;
using static Dapper.SqlMapper;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer.IntegrationTests.Statistics.SqlServerPlayerRepositoryTests
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class LinkPlayerToMemberAccountTests : IDisposable
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

        public LinkPlayerToMemberAccountTests(SqlServerTestDataFixture databaseFixture)
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

        #endregion

        #region LinkPlayerToMemberAccount
        [Fact]
        public async Task LinkPlayerToMemberAccount_where_member_has_no_previous_player_updates_player_and_identity()
        {
            var memberWithNoExistingPlayer = _testData.AnyMemberNotLinkedToPlayer();
            var playerNotLinkedToMember = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity();
            var playerCopy = new Player { PlayerId = playerNotLinkedToMember.PlayerId, PlayerRoute = playerNotLinkedToMember.PlayerRoute };
            _copier.Setup(x => x.CreateAuditableCopy(playerNotLinkedToMember)).Returns(playerCopy);

            var repo = CreateRepository();
            var returnedPlayer = await repo.LinkPlayerToMemberAccount(playerNotLinkedToMember, memberWithNoExistingPlayer.memberKey, memberWithNoExistingPlayer.memberName).ConfigureAwait(false);

            Assert.Equal(memberWithNoExistingPlayer.memberKey, returnedPlayer.MemberKey);

            using (var connectionForAssert = _connectionFactory.CreateDatabaseConnection())
            {
                connectionForAssert.Open();

                var assignedMemberKey = await connectionForAssert.QuerySingleAsync<Guid>($"SELECT MemberKey FROM {Tables.Player} WHERE PlayerId = @PlayerId", playerCopy).ConfigureAwait(false);
                Assert.Equal(memberWithNoExistingPlayer.memberKey, assignedMemberKey);

                var linkedBy = await connectionForAssert.QuerySingleAsync<string>($"SELECT LinkedBy FROM {Tables.PlayerIdentity} WHERE PlayerId = @PlayerId", playerCopy).ConfigureAwait(false);
                Assert.Equal(PlayerIdentityLinkedBy.Member.ToString(), linkedBy);
            }
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(1, false)]
        [InlineData(2, true)]
        [InlineData(2, false)]
        public async Task LinkPlayerToMemberAccount_where_member_has_linked_player_merges_players_for_authenticated_member(int existingIdentitiesForPlayerToLink, bool playerToLinkIsOnSameTeamAsPlayerIdentityForMember)
        {
            var memberWithExistingPlayer = _testData.AnyMemberLinkedToPlayerWithOnlyOneIdentity();
            var playerAlreadyLinked = _testData.Players.First(x => x.MemberKey == memberWithExistingPlayer.memberKey);
            var playerAlreadyLinkedCopy = new Player { PlayerId = playerAlreadyLinked.PlayerId, PlayerRoute = playerAlreadyLinked.PlayerRoute };
            _copier.Setup(x => x.CreateAuditableCopy(playerAlreadyLinked)).Returns(playerAlreadyLinkedCopy);

            var playerNotLinkedToMember = existingIdentitiesForPlayerToLink == 1 ?
                _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity() :
                _testData.AnyPlayerNotLinkedToMemberWithMultipleIdentities(p => p.PlayerIdentities.Count == existingIdentitiesForPlayerToLink &&
                                                                                p.PlayerIdentities.Where(p2 => p2.LinkedBy == PlayerIdentityLinkedBy.Team).Count() == existingIdentitiesForPlayerToLink &&
                                                                                playerToLinkIsOnSameTeamAsPlayerIdentityForMember ? p.IsOnTheSameTeamAs(playerAlreadyLinked) : !p.IsOnTheSameTeamAs(playerAlreadyLinked));
            var playerNotLinkedToMemberCopy = new Player { PlayerId = playerNotLinkedToMember.PlayerId, PlayerRoute = playerNotLinkedToMember.PlayerRoute };
            _copier.Setup(x => x.CreateAuditableCopy(playerNotLinkedToMember)).Returns(playerNotLinkedToMemberCopy);
            var expectedIdentities = playerAlreadyLinked.PlayerIdentities.Count + playerNotLinkedToMember.PlayerIdentities.Count;

            _routeSelector.Setup(x => x.SelectBestRoute(playerAlreadyLinked.PlayerRoute!, playerNotLinkedToMember.PlayerRoute!)).Returns(playerNotLinkedToMember.PlayerRoute!);

            var repo = CreateRepository();
            var returnedPlayer = await repo.LinkPlayerToMemberAccount(playerNotLinkedToMember, memberWithExistingPlayer.memberKey, memberWithExistingPlayer.memberName).ConfigureAwait(false);
            await repo.ProcessAsyncUpdatesForPlayers();

            // The returned player should have the result of merging both players
            Assert.Equal(playerAlreadyLinked.PlayerId, returnedPlayer.PlayerId);
            Assert.Equal(playerNotLinkedToMember.PlayerRoute, returnedPlayer.PlayerRoute);
            Assert.Equal(memberWithExistingPlayer.memberKey, returnedPlayer.MemberKey);

            using (var connectionForAssert = _connectionFactory.CreateDatabaseConnection())
            {
                connectionForAssert.Open();

                var originalPlayerStillLinkedToMemberWithUpdatedRoute = await connectionForAssert.QuerySingleAsync<Player>($"SELECT PlayerId, PlayerRoute FROM {Tables.Player} WHERE MemberKey = @memberKey", new { memberWithExistingPlayer.memberKey }).ConfigureAwait(false);
                Assert.Equal(playerAlreadyLinked.PlayerId, originalPlayerStillLinkedToMemberWithUpdatedRoute.PlayerId);
                Assert.Equal(playerNotLinkedToMember.PlayerRoute, originalPlayerStillLinkedToMemberWithUpdatedRoute.PlayerRoute);

                foreach (var identity in playerNotLinkedToMember.PlayerIdentities)
                {
                    var alreadyLinkedPlayerIdNowLinkedToMovedPlayerIdentity = await connectionForAssert.QuerySingleAsync<Guid>($"SELECT PlayerId FROM {Tables.PlayerIdentity} WHERE PlayerIdentityId = @PlayerIdentityId", identity).ConfigureAwait(false);
                    Assert.Equal(playerAlreadyLinked.PlayerId, alreadyLinkedPlayerIdNowLinkedToMovedPlayerIdentity);

                    var playerIdsForMovedIdentityShouldBeTheAlreadyLinkedPlayer = await connectionForAssert.QueryAsync<Guid>($"SELECT PlayerId FROM {Tables.PlayerInMatchStatistics} WHERE PlayerIdentityId = @PlayerIdentityId", identity).ConfigureAwait(false);
                    foreach (var playerIdNowLinkedToSecondPlayerIdentityInStatistics in playerIdsForMovedIdentityShouldBeTheAlreadyLinkedPlayer)
                    {
                        Assert.Equal(playerAlreadyLinked.PlayerId, playerIdNowLinkedToSecondPlayerIdentityInStatistics);
                    }
                }

                var playerRouteForAllIdentitiesOfAlreadyLinkedPlayerShouldBeUpdated = await connectionForAssert.QueryAsync<string>($"SELECT PlayerRoute FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = @PlayerId", playerAlreadyLinked).ConfigureAwait(false);
                foreach (var route in playerRouteForAllIdentitiesOfAlreadyLinkedPlayerShouldBeUpdated)
                {
                    Assert.Equal(playerNotLinkedToMember.PlayerRoute, route);
                }

                var identitiesLinkedBy = (await connectionForAssert.QueryAsync<string>($"SELECT LinkedBy FROM {Tables.PlayerIdentity} WHERE PlayerId = @PlayerId", playerAlreadyLinked).ConfigureAwait(false)).ToList();
                Assert.Equal(expectedIdentities, identitiesLinkedBy.Count);
                foreach (var linkedBy in identitiesLinkedBy)
                {
                    Assert.Equal(PlayerIdentityLinkedBy.Member.ToString(), linkedBy);
                }

                var obsoletePlayerShouldBeRemoved = await connectionForAssert.QuerySingleOrDefaultAsync<int>($"SELECT COUNT(PlayerId) FROM {Tables.Player} WHERE PlayerId = @PlayerId", playerNotLinkedToMember).ConfigureAwait(false);
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
            await repo.LinkPlayerToMemberAccount(playerNotLinkedToMember, memberWithExistingPlayer.memberKey, memberWithExistingPlayer.memberName).ConfigureAwait(false);

            _redirectsRepository.VerifyPlayerIsRedirected(playerAlreadyLinked.PlayerRoute!, playerNotLinkedToMember.PlayerRoute!);
        }

        [Fact]
        public async Task LinkPlayerToMemberAccount_where_identity_to_link_already_linked_to_different_member_throws_InvalidOperationException()
        {
            var playerLinkedToMember = _testData.AnyPlayerLinkedToMemberWithOnlyOneIdentity();
            var playerCopy = new Player { PlayerId = playerLinkedToMember.PlayerId };
            _copier.Setup(x => x.CreateAuditableCopy(playerLinkedToMember)).Returns(playerCopy);

            var repo = CreateRepository();
            _ = await Assert.ThrowsAsync<InvalidOperationException>(async () => await repo.LinkPlayerToMemberAccount(playerLinkedToMember, Guid.NewGuid(), "Member name")).ConfigureAwait(false);
        }

        [Theory]
        [InlineData(PlayerIdentityLinkedBy.Team, true)]
        [InlineData(PlayerIdentityLinkedBy.Team, false)]
        [InlineData(PlayerIdentityLinkedBy.StoolballEngland, true)]
        [InlineData(PlayerIdentityLinkedBy.StoolballEngland, false)]
        public async Task LinkPlayerToMemberAccount_where_player_already_linked_by_team_or_admin_updates_all_identities_to_member(PlayerIdentityLinkedBy linkedBy, bool isFirstPlayerIdentityForMember)
        {
            var playerNotLinkedToMember = _testData.AnyPlayerNotLinkedToMemberWithMultipleIdentities(p => p.PlayerIdentities.Count == p.PlayerIdentities.Where(p2 => p2.LinkedBy == linkedBy).Count());
            var playerCopy = new Player { PlayerId = playerNotLinkedToMember.PlayerId, PlayerRoute = playerNotLinkedToMember.PlayerRoute };
            _copier.Setup(x => x.CreateAuditableCopy(playerNotLinkedToMember)).Returns(playerCopy);

            var expectedIdentities = playerNotLinkedToMember.PlayerIdentities.Count;
            var member = isFirstPlayerIdentityForMember ? _testData.AnyMemberNotLinkedToPlayer() : _testData.AnyMemberLinkedToPlayer();
            if (!isFirstPlayerIdentityForMember)
            {
                var existingPlayerForMember = _testData.Players.Single(p => p.MemberKey == member.memberKey);
                expectedIdentities += existingPlayerForMember.PlayerIdentities.Count;
                _routeSelector.Setup(x => x.SelectBestRoute(existingPlayerForMember.PlayerRoute!, playerNotLinkedToMember.PlayerRoute!)).Returns(playerNotLinkedToMember.PlayerRoute!);
            }

            var repo = CreateRepository();
            _ = await repo.LinkPlayerToMemberAccount(playerNotLinkedToMember, member.memberKey, member.memberName).ConfigureAwait(false);
            await repo.ProcessAsyncUpdatesForPlayers();


            using (var connectionForAssert = _connectionFactory.CreateDatabaseConnection())
            {
                connectionForAssert.Open();

                var updatedIdentities = (await connectionForAssert.QueryAsync<(Guid? MemberKey, Guid PlayerIdentityId, PlayerIdentityLinkedBy LinkedBy)>(
                                                                        $"SELECT MemberKey, PlayerIdentityId, LinkedBy FROM {Views.PlayerIdentity} WHERE PlayerId = @PlayerId", playerCopy).ConfigureAwait(false)).ToList();

                Assert.Equal(expectedIdentities, updatedIdentities.Count);
                foreach (var identity in playerNotLinkedToMember.PlayerIdentities.Select(p => p.PlayerIdentityId))
                {
                    Assert.Contains(updatedIdentities, pi => pi.PlayerIdentityId == identity);

                    var updatedIdentity = updatedIdentities.Single(pi => pi.PlayerIdentityId == identity);
                    Assert.Equal(member.memberKey, updatedIdentity.MemberKey);
                    Assert.Equal(PlayerIdentityLinkedBy.Member, updatedIdentity.LinkedBy);
                }

                if (!isFirstPlayerIdentityForMember)
                {
                    var obsoletePlayerShouldBeRemoved = await connectionForAssert.QuerySingleOrDefaultAsync<int>($"SELECT COUNT(PlayerId) FROM {Tables.Player} WHERE PlayerId = @PlayerId", playerNotLinkedToMember).ConfigureAwait(false);
                    Assert.Equal(0, obsoletePlayerShouldBeRemoved);
                }
            }
        }

        [Fact]
        public async Task LinkPlayerToMemberAccount_where_member_has_no_previous_player_audits_and_logs()
        {
            var player = _testData.AnyPlayerNotLinkedToMember();
            var playerCopy = new Player { PlayerId = player.PlayerId };
            _copier.Setup(x => x.CreateAuditableCopy(player)).Returns(playerCopy);
            var memberNotLinkedToPlayer = _testData.AnyMemberNotLinkedToPlayer();

            var repo = CreateRepository();
            await repo.LinkPlayerToMemberAccount(player, memberNotLinkedToPlayer.memberKey, memberNotLinkedToPlayer.memberName).ConfigureAwait(false);

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
            await repo.LinkPlayerToMemberAccount(playerNotLinkedToMember, memberWithExistingPlayer.memberKey, memberWithExistingPlayer.memberName).ConfigureAwait(false);

            _auditRepository.Verify(x => x.CreateAudit(It.Is<AuditRecord>(x => x.Action == AuditAction.Delete && x.EntityUri!.ToString().EndsWith(playerNotLinkedToMember.PlayerId.ToString()!)), It.IsAny<IDbTransaction>()), Times.Once);
            _logger.Verify(x => x.Info(LoggingTemplates.Deleted, It.Is<string>(x => x.Contains(playerNotLinkedToMember.PlayerId.ToString()!)), memberWithExistingPlayer.memberName, memberWithExistingPlayer.memberKey, typeof(SqlServerPlayerRepository), nameof(SqlServerPlayerRepository.LinkPlayerToMemberAccount)));

            _auditRepository.Verify(x => x.CreateAudit(It.Is<AuditRecord>(x => x.Action == AuditAction.Update && x.EntityUri!.ToString().EndsWith(playerAlreadyLinked.PlayerId.ToString()!)), It.IsAny<IDbTransaction>()), Times.Once);
            _logger.Verify(x => x.Info(LoggingTemplates.Updated, It.Is<string>(x => x.Contains(playerAlreadyLinked.PlayerId.ToString()!)), memberWithExistingPlayer.memberName, memberWithExistingPlayer.memberKey, typeof(SqlServerPlayerRepository), nameof(SqlServerPlayerRepository.LinkPlayerToMemberAccount)));
        }
        #endregion

        #region UnlinkPlayerIdentityFromMemberAccount
        [Fact]
        public async Task UnlinkPlayerIdentityFromMemberAccount_for_penultimate_identity_moves_identity_to_new_player_including_statistics()
        {
            var player = _testData.AnyPlayerLinkedToMemberWithMultipleIdentities(
                p => p.PlayerIdentities.Count == p.PlayerIdentities.Count(pi => pi.LinkedBy == PlayerIdentityLinkedBy.Member) &&
                     p.PlayerIdentities.Count == 2);
            var playerCopy = new Player { PlayerId = player.PlayerId };
            _copier.Setup(x => x.CreateAuditableCopy(player)).Returns(playerCopy);
            var playerIdentityToUnlink = player.PlayerIdentities.First();
            var member = _testData.Members.Single(x => x.memberKey == player.MemberKey);

            var generatedPlayerRoute = "/players/" + Guid.NewGuid().ToString();
            _routeGenerator.Setup(x => x.GenerateUniqueRoute("/players", playerIdentityToUnlink.PlayerIdentityName!, NoiseWords.PlayerRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(generatedPlayerRoute));

            var repo = CreateRepository();

            await repo.UnlinkPlayerIdentityFromMemberAccount(playerIdentityToUnlink, member.memberKey, member.memberName).ConfigureAwait(false);
            await repo.ProcessAsyncUpdatesForPlayers().ConfigureAwait(false);

            using (var connectionForAssert = _connectionFactory.CreateDatabaseConnection())
            {
                connectionForAssert.Open();

                var originalPlayerStillLinkedToMember = await connectionForAssert.QuerySingleAsync<Guid?>($"SELECT MemberKey FROM {Tables.Player} WHERE PlayerId = @PlayerId", player).ConfigureAwait(false);
                Assert.Equal(member.memberKey, originalPlayerStillLinkedToMember);

                var remainingIdentityLinkedBy = await connectionForAssert.QuerySingleAsync<string>($"SELECT LinkedBy FROM {Tables.PlayerIdentity} WHERE PlayerId = @PlayerId", player).ConfigureAwait(false);
                Assert.Equal(PlayerIdentityLinkedBy.Member.ToString(), remainingIdentityLinkedBy);

                var newPlayerLinkedToIdentity = await connectionForAssert.QuerySingleAsync<(Guid? playerId, string route, Guid? memberKey, PlayerIdentityLinkedBy linkedBy)>(
                    $@"SELECT p.PlayerId, p.PlayerRoute, p.MemberKey, p.LinkedBy
                       FROM {Views.PlayerIdentity} p 
                       WHERE PlayerIdentityId = @PlayerIdentityId", playerIdentityToUnlink).ConfigureAwait(false);
                Assert.NotEqual(player.PlayerId, newPlayerLinkedToIdentity.playerId);
                Assert.Equal(generatedPlayerRoute, newPlayerLinkedToIdentity.route);
                Assert.Null(newPlayerLinkedToIdentity.memberKey);
                Assert.Equal(PlayerIdentityLinkedBy.DefaultIdentity, newPlayerLinkedToIdentity.linkedBy);

                var statisticsForIdentity = await connectionForAssert.QueryAsync<(Guid? playerId, string route)>($"SELECT PlayerId, PlayerRoute FROM {Tables.PlayerInMatchStatistics} WHERE PlayerIdentityId = @PlayerIdentityId", playerIdentityToUnlink).ConfigureAwait(false);
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

            await repo.UnlinkPlayerIdentityFromMemberAccount(playerIdentity, member.memberKey, member.memberName).ConfigureAwait(false);

            _auditRepository.Verify(x => x.CreateAudit(It.IsAny<AuditRecord>(), It.IsAny<IDbTransaction>()), Times.Once);
            _logger.Verify(x => x.Info(LoggingTemplates.Created, It.IsAny<string>(), member.memberName, member.memberKey, typeof(SqlServerPlayerRepository), nameof(SqlServerPlayerRepository.UnlinkPlayerIdentityFromMemberAccount)));
        }

        [Fact]
        public async Task UnlinkPlayerIdentityFromMemberAccount_for_last_identity_unlinks_player_from_member()
        {
            var player = _testData.AnyPlayerLinkedToMemberWithOnlyOneIdentity();
            var member = _testData.Members.Single(x => x.memberKey == player.MemberKey);

            var repo = CreateRepository();

            await repo.UnlinkPlayerIdentityFromMemberAccount(player.PlayerIdentities.First(), member.memberKey, member.memberName).ConfigureAwait(false);

            using (var connectionForAssert = _connectionFactory.CreateDatabaseConnection())
            {
                connectionForAssert.Open();
                var result = await connectionForAssert.QuerySingleAsync<(Guid? MemberKey, PlayerIdentityLinkedBy LinkedBy)>($"SELECT MemberKey, LinkedBy FROM {Views.PlayerIdentity} WHERE PlayerId = @PlayerId", player).ConfigureAwait(false);

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

            await repo.UnlinkPlayerIdentityFromMemberAccount(player.PlayerIdentities.First(), member.memberKey, member.memberName).ConfigureAwait(false);

            _auditRepository.Verify(x => x.CreateAudit(It.IsAny<AuditRecord>(), It.IsAny<IDbTransaction>()), Times.Once);
            _logger.Verify(x => x.Info(LoggingTemplates.Updated, It.IsAny<string>(), member.memberName, member.memberKey, typeof(SqlServerPlayerRepository), nameof(SqlServerPlayerRepository.UnlinkPlayerIdentityFromMemberAccount)));
        }
        #endregion
    }
}
