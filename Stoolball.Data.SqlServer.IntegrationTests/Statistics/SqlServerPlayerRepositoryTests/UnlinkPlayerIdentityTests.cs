using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
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
    public class UnlinkPlayerIdentityTests(SqlServerTestDataFixture _fixture) : IDisposable
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
        public async Task UnlinkPlayerIdentity_throws_InvalidOperationException_for_last_identity()
        {
            var player = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity();

            var repo = CreateRepository();

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await repo.UnlinkPlayerIdentity(player.PlayerIdentities.First().PlayerIdentityId!.Value, Guid.NewGuid(), "Member name").ConfigureAwait(false));
        }

        [Fact]
        public async Task UnlinkPlayerIdentity_throws_InvalidOperationException_unlinking_identity_where_all_linked_by_member_and_called_by_different_member()
        {
            var player = _testData.AnyPlayerLinkedToMemberWithMultipleIdentities(p => p.PlayerIdentities.Count(p2 => p2.LinkedBy == PlayerIdentityLinkedBy.Member) == p.PlayerIdentities.Count);

            var repo = CreateRepository();

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await repo.UnlinkPlayerIdentity(player.PlayerIdentities.First().PlayerIdentityId!.Value, Guid.NewGuid(), "Member name").ConfigureAwait(false));
        }

        [Fact]
        public async Task UnlinkPlayerIdentity_throws_InvalidOperationException_unlinking_identity_linked_by_member_where_others_not_linked_by_member_and_called_by_different_member()
        {
            var player = _testData.AnyPlayerLinkedToMemberWithMultipleIdentities(p => p.PlayerIdentities.Any(p2 => p2.LinkedBy == PlayerIdentityLinkedBy.Member) &&
                                                                                      p.PlayerIdentities.Any(p2 => p2.LinkedBy == PlayerIdentityLinkedBy.Team));

            var repo = CreateRepository();

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await repo.UnlinkPlayerIdentity(player.PlayerIdentities.First(p => p.LinkedBy == PlayerIdentityLinkedBy.Member).PlayerIdentityId!.Value, Guid.NewGuid(), "Member name").ConfigureAwait(false));
        }

        [Fact]
        public async Task UnlinkPlayerIdentity_unlinks_identity_linked_by_team_where_others_linked_by_member_and_called_by_different_member()
        {
            var player = _testData.AnyPlayerLinkedToMemberWithMultipleIdentities(p => p.PlayerIdentities.Any(p2 => p2.LinkedBy == PlayerIdentityLinkedBy.Member) &&
                                                                                      p.PlayerIdentities.Any(p2 => p2.LinkedBy == PlayerIdentityLinkedBy.Team));

            var identityToUnlink = player.PlayerIdentities.First(p => p.LinkedBy == PlayerIdentityLinkedBy.Team);

            var generatedPlayerRoute = "/players/" + Guid.NewGuid().ToString();
            _routeGenerator.Setup(x => x.GenerateUniqueRoute("/players", identityToUnlink.PlayerIdentityName!, NoiseWords.PlayerRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(generatedPlayerRoute));

            var repo = CreateRepository();

            await repo.UnlinkPlayerIdentity(identityToUnlink.PlayerIdentityId!.Value, Guid.NewGuid(), "Member name").ConfigureAwait(false);

            using (var connectionForAssert = _connectionFactory.CreateDatabaseConnection())
            {
                connectionForAssert.Open();

                var remainingIdentitiesForPlayer = await connectionForAssert.QueryAsync<(Guid PlayerId, Guid MemberKey, Guid PlayerIdentityId, PlayerIdentityLinkedBy LinkedBy)>(
                                                                                        $"SELECT PlayerId, MemberKey, PlayerIdentityId, LinkedBy FROM {Views.PlayerIdentity} WHERE PlayerId = @PlayerId",
                                                                                        new { player.PlayerId }).ConfigureAwait(false);
                foreach (var originalIdentity in player.PlayerIdentities)
                {
                    if (originalIdentity.PlayerIdentityId == identityToUnlink.PlayerIdentityId)
                    {
                        Assert.DoesNotContain(remainingIdentitiesForPlayer, pi => pi.PlayerIdentityId == originalIdentity.PlayerIdentityId);
                        continue;
                    }

                    var remainingIdentity = remainingIdentitiesForPlayer.SingleOrDefault(pi => pi.PlayerIdentityId == originalIdentity.PlayerIdentityId);
                    Assert.Equal(player.MemberKey, remainingIdentity.MemberKey);
                    Assert.Equal(originalIdentity.LinkedBy, remainingIdentity.LinkedBy);
                }

                var newPlayerLinkedToIdentity = await AssertNewPlayerForUnlinkedIdentity(player.PlayerId!.Value, identityToUnlink.PlayerIdentityId!.Value, generatedPlayerRoute, connectionForAssert).ConfigureAwait(false);

                await AssertStatisticsForUnlinkedIdentity(identityToUnlink.PlayerIdentityId!.Value, generatedPlayerRoute, connectionForAssert, newPlayerLinkedToIdentity);
            }
        }

        [Fact]
        public async Task UnlinkPlayerIdentity_with_multiple_identities_remaining_moves_identity_to_new_player_including_statistics()
        {
            var player = _testData.AnyPlayerNotLinkedToMemberWithMultipleIdentities(p => p.PlayerIdentities.Count > 2 &&
                                                                                         p.PlayerIdentities.Count(pi => pi.LinkedBy == PlayerIdentityLinkedBy.Team) == p.PlayerIdentities.Count);

            var identityToUnlink = player.PlayerIdentities[1];
            var identitiesToKeep = player.PlayerIdentities.Where(pi => pi.PlayerIdentityId != identityToUnlink.PlayerIdentityId).Select(pi => pi.PlayerIdentityId);

            var generatedPlayerRoute = "/players/" + Guid.NewGuid().ToString();
            _routeGenerator.Setup(x => x.GenerateUniqueRoute("/players", identityToUnlink.PlayerIdentityName!, NoiseWords.PlayerRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(generatedPlayerRoute));

            var repo = CreateRepository();

            await repo.UnlinkPlayerIdentity(identityToUnlink.PlayerIdentityId!.Value, Guid.NewGuid(), "Member name").ConfigureAwait(false);
            await repo.ProcessAsyncUpdatesForPlayers().ConfigureAwait(false);

            using (var connectionForAssert = _connectionFactory.CreateDatabaseConnection())
            {
                connectionForAssert.Open();

                var originalIdentitiesStillLinked = await connectionForAssert.QueryAsync<(Guid PlayerId, PlayerIdentityLinkedBy LinkedBy)>(
                                                                                        $"SELECT PlayerId, LinkedBy FROM {Tables.PlayerIdentity} WHERE PlayerIdentityId IN @PlayerIdentityIds",
                                                                                        new { PlayerIdentityIds = identitiesToKeep }).ConfigureAwait(false);
                foreach (var identity in originalIdentitiesStillLinked)
                {
                    Assert.Equal(player.PlayerId, identity.PlayerId);
                    Assert.Equal(PlayerIdentityLinkedBy.Team, identity.LinkedBy);
                }

                var newPlayerLinkedToIdentity = await AssertNewPlayerForUnlinkedIdentity(player.PlayerId!.Value, identityToUnlink.PlayerIdentityId.Value, generatedPlayerRoute, connectionForAssert).ConfigureAwait(false);

                await AssertStatisticsForUnlinkedIdentity(identityToUnlink.PlayerIdentityId.Value, generatedPlayerRoute, connectionForAssert, newPlayerLinkedToIdentity);
            }
        }

        [Fact]
        public async Task UnlinkPlayerIdentity_for_penultimate_identity_moves_identity_to_new_player_including_statistics()
        {
            var player = _testData.AnyPlayerNotLinkedToMemberWithMultipleIdentities(p => p.PlayerIdentities.Count == 2 &&
                                                                                         p.PlayerIdentities.Count(pi => pi.LinkedBy == PlayerIdentityLinkedBy.Team) == 2);
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

                var originalPlayerStillLinked = await connectionForAssert.QuerySingleAsync<(Guid PlayerId, PlayerIdentityLinkedBy LinkedBy)>($"SELECT PlayerId, LinkedBy FROM {Tables.PlayerIdentity} WHERE PlayerIdentityId = @PlayerIdentityId", identityToKeep).ConfigureAwait(false);
                Assert.Equal(player.PlayerId, originalPlayerStillLinked.PlayerId);
                Assert.Equal(PlayerIdentityLinkedBy.DefaultIdentity, originalPlayerStillLinked.LinkedBy);

                var newPlayerLinkedToIdentity = await AssertNewPlayerForUnlinkedIdentity(player.PlayerId!.Value, identityToUnlink.PlayerIdentityId.Value, generatedPlayerRoute, connectionForAssert).ConfigureAwait(false);

                await AssertStatisticsForUnlinkedIdentity(identityToUnlink.PlayerIdentityId.Value, generatedPlayerRoute, connectionForAssert, newPlayerLinkedToIdentity);
            }
        }

        private static async Task<Guid?> AssertNewPlayerForUnlinkedIdentity(Guid previousPlayerId, Guid identityToUnlink, string expectedRoute, IDbConnection connectionForAssert)
        {
            var (playerId, route, memberKey, linkedBy) = await connectionForAssert.QuerySingleAsync<(Guid, string, Guid?, PlayerIdentityLinkedBy)>(
                $@"SELECT p.PlayerId, p.PlayerRoute, p.MemberKey, p.LinkedBy
                       FROM {Views.PlayerIdentity} p 
                       WHERE PlayerIdentityId = @PlayerIdentityId", new { PlayerIdentityId = identityToUnlink }).ConfigureAwait(false);
            Assert.NotEqual(previousPlayerId, playerId);
            Assert.Equal(expectedRoute, route);
            Assert.Null(memberKey);
            Assert.Equal(PlayerIdentityLinkedBy.DefaultIdentity, linkedBy);
            return playerId;
        }

        private static async Task AssertStatisticsForUnlinkedIdentity(Guid identityToUnlink, string generatedPlayerRoute, IDbConnection connectionForAssert, Guid? newPlayerLinkedToIdentity)
        {
            var statisticsForIdentity = await connectionForAssert.QueryAsync<(Guid? playerId, string route)>($"SELECT PlayerId, PlayerRoute FROM {Tables.PlayerInMatchStatistics} WHERE PlayerIdentityId = @PlayerIdentityId",
                                                                                                                new { PlayerIdentityId = identityToUnlink });
            foreach (var row in statisticsForIdentity)
            {
                Assert.Equal(newPlayerLinkedToIdentity, row.playerId);
                Assert.Equal(generatedPlayerRoute, row.route);
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
    }
}
