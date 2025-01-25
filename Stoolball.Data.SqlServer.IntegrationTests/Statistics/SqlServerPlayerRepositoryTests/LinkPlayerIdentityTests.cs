using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Moq;
using Newtonsoft.Json;
using Stoolball.Data.Abstractions;
using Stoolball.Data.Abstractions.Models;
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
    public class LinkPlayerIdentityTests(SqlServerTestDataFixture _fixture) : IDisposable
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
        public async Task LinkPlayerIdentity_throws_InvalidOperationException_if_identity_is_already_linked_to_target_player()
        {
            var player = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity();
            SetupMocksForLinkPlayerIdentity(player, player);

            var repo = CreateRepository();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await repo.LinkPlayerIdentity(player.PlayerId!.Value, player.PlayerIdentities[0].PlayerIdentityId!.Value, PlayerIdentityLinkedBy.Team, Guid.NewGuid(), "Member name"));
        }

        [Fact]
        public async Task LinkPlayerIdentity_throws_InvalidOperationException_if_both_players_are_linked_to_a_member()
        {
            var player1 = _testData.AnyPlayerLinkedToMemberWithOnlyOneIdentity(p => p.PlayerIdentities[0].LinkedBy == PlayerIdentityLinkedBy.Member &&
                                                                                    _testData.Players.Any(p2 => p2.PlayerIdentities.Count == 1 &&
                                                                                                                p2.MemberKey.HasValue &&
                                                                                                                p2.PlayerIdentities[0].LinkedBy == PlayerIdentityLinkedBy.Member &&
                                                                                                                p2.PlayerId != p.PlayerId &&
                                                                                                                p2.IsOnTheSameTeamAs(p)));
            var player2 = _testData.AnyPlayerLinkedToMemberWithOnlyOneIdentity(player2 => player2.PlayerIdentities[0].LinkedBy == PlayerIdentityLinkedBy.Member &&
                                                                                          player2.PlayerId != player1.PlayerId &&
                                                                                          player1.IsOnTheSameTeamAs(player2));
            SetupMocksForLinkPlayerIdentity(player1, player2);

            var repo = CreateRepository();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await repo.LinkPlayerIdentity(player1.PlayerId!.Value, player2.PlayerIdentities[0].PlayerIdentityId!.Value, PlayerIdentityLinkedBy.Team, Guid.NewGuid(), "Member name"));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task LinkPlayerIdentity_throws_InvalidOperationException_if_target_player_is_linked_to_a_member_unless_it_is_the_current_member(bool isCurrentMember)
        {
            var repo = CreateRepository();
            if (isCurrentMember)
            {
                var targetPlayer = _testData.AnyPlayerLinkedToMemberWithOnlyOneIdentity(p => _testData.Players.Any(p2 => p2.MemberKey is null && p2.IsOnTheSameTeamAs(p)));
                var playerWithIdentityToLink = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity(p => p.IsOnTheSameTeamAs(targetPlayer));
                var currentMember = targetPlayer.MemberKey!.Value;
                SetupMocksForLinkPlayerIdentity(targetPlayer, playerWithIdentityToLink);

                var exception = await Record.ExceptionAsync(async () => await repo.LinkPlayerIdentity(targetPlayer.PlayerId!.Value, playerWithIdentityToLink.PlayerIdentities[0].PlayerIdentityId!.Value, PlayerIdentityLinkedBy.Team, currentMember, "Member name"));
                Assert.Null(exception);
            }
            else
            {
                var currentMember = _testData.AnyMemberLinkedToPlayer();
                var targetPlayer = _testData.AnyPlayerLinkedToMemberWithOnlyOneIdentity(p => p.MemberKey != currentMember.memberKey && _testData.Players.Any(p2 => p2.MemberKey is null && p2.IsOnTheSameTeamAs(p)));
                var playerWithIdentityToLink = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity(p => p.IsOnTheSameTeamAs(targetPlayer));
                SetupMocksForLinkPlayerIdentity(targetPlayer, playerWithIdentityToLink);

                await Assert.ThrowsAsync<InvalidOperationException>(async () => await repo.LinkPlayerIdentity(targetPlayer.PlayerId!.Value, playerWithIdentityToLink.PlayerIdentities[0].PlayerIdentityId!.Value, PlayerIdentityLinkedBy.Team, currentMember.memberKey, currentMember.memberName));
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
                var targetPlayer = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity(p => p.PlayerIdentities[0].LinkedBy == PlayerIdentityLinkedBy.DefaultIdentity &&
                                                                                                _testData.Players.Any(p2 => p2.MemberKey is not null &&
                                                                                                                            p2.PlayerIdentities.Count == 1 &&
                                                                                                                            p2.PlayerIdentities[0].LinkedBy == PlayerIdentityLinkedBy.Member &&
                                                                                                                            p2.IsOnTheSameTeamAs(p)));
                var playerWithIdentityToLink = _testData.AnyPlayerLinkedToMemberWithOnlyOneIdentity(p => p.MemberKey is not null &&
                                                                                                         p.IsOnTheSameTeamAs(targetPlayer) &&
                                                                                                         p.PlayerIdentities[0].LinkedBy == PlayerIdentityLinkedBy.Member
                                                                                                         );
                var currentMember = playerWithIdentityToLink.MemberKey!.Value;
                SetupMocksForLinkPlayerIdentity(targetPlayer, playerWithIdentityToLink);

                var exception = await Record.ExceptionAsync(async () => await repo.LinkPlayerIdentity(targetPlayer.PlayerId!.Value, playerWithIdentityToLink.PlayerIdentities[0].PlayerIdentityId!.Value, PlayerIdentityLinkedBy.Team, currentMember, "Member name"));
                Assert.Null(exception);
            }
            else
            {
                var currentMember = _testData.AnyMemberLinkedToPlayer();
                var targetPlayer = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity(p => _testData.Players.Any(p2 => p2.MemberKey.HasValue && p2.MemberKey != currentMember.memberKey && p2.PlayerIdentities.Count == 1 && p2.IsOnTheSameTeamAs(p))); ;
                var playerWithIdentityToLink = _testData.AnyPlayerLinkedToMemberWithOnlyOneIdentity(p => p.MemberKey != currentMember.memberKey && p.IsOnTheSameTeamAs(targetPlayer));
                SetupMocksForLinkPlayerIdentity(targetPlayer, playerWithIdentityToLink);

                await Assert.ThrowsAsync<InvalidOperationException>(async () => await repo.LinkPlayerIdentity(targetPlayer.PlayerId!.Value, playerWithIdentityToLink.PlayerIdentities[0].PlayerIdentityId!.Value, PlayerIdentityLinkedBy.Team, currentMember.memberKey, currentMember.memberName));
            }
        }

        [Fact]
        public async Task LinkPlayerIdentity_throws_InvalidOperationException_if_identity_to_link_is_linked_to_other_identities()
        {
            var targetPlayer = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity(p => _testData.Players.Any(p2 => p2.IsOnTheSameTeamAs(p) &&
                                                                                                                       !p2.MemberKey.HasValue &&
                                                                                                                        p2.PlayerIdentities.Count > 1 &&
                                                                                                                        p2.PlayerIdentities.Count == p2.PlayerIdentities.Where(x => x.LinkedBy == PlayerIdentityLinkedBy.Team).Count()));
            var playerWithIdentityToLink = _testData.AnyPlayerNotLinkedToMemberWithMultipleIdentities(p => p.IsOnTheSameTeamAs(targetPlayer) &&
                                                                                                           p.PlayerIdentities.Count == p.PlayerIdentities.Where(x => x.LinkedBy == PlayerIdentityLinkedBy.Team).Count());
            SetupMocksForLinkPlayerIdentity(targetPlayer, playerWithIdentityToLink);

            var repo = CreateRepository();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await repo.LinkPlayerIdentity(targetPlayer.PlayerId!.Value, playerWithIdentityToLink.PlayerIdentities[0].PlayerIdentityId!.Value, PlayerIdentityLinkedBy.Team, Guid.NewGuid(), "Member name"));
        }

        [Fact]
        public async Task LinkPlayerIdentity_throws_InvalidOperationException_if_target_player_does_not_have_an_identity_on_the_same_team_as_identity_to_link()
        {
            var player1 = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity();
            var player2 = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity(p => !p.IsOnTheSameTeamAs(player1));
            SetupMocksForLinkPlayerIdentity(player1, player2);

            var repo = CreateRepository();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await repo.LinkPlayerIdentity(player1.PlayerId!.Value, player2.PlayerIdentities[0].PlayerIdentityId!.Value, PlayerIdentityLinkedBy.Team, Guid.NewGuid(), "Member name"));
        }

        [Fact]
        public async Task LinkPlayerIdentity_merges_two_default_identities()
        {
            var targetPlayer = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity();
            var playerWithIdentityToLink = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity(p => p.PlayerId != targetPlayer.PlayerId && p.IsOnTheSameTeamAs(targetPlayer));

            SetupMocksForLinkPlayerIdentity(targetPlayer, playerWithIdentityToLink);

            var member = _testData.AnyMemberNotLinkedToPlayer();

            var repo = CreateRepository();
            var movedIdentityResult = await repo.LinkPlayerIdentity(targetPlayer.PlayerId!.Value, playerWithIdentityToLink.PlayerIdentities[0].PlayerIdentityId!.Value, PlayerIdentityLinkedBy.Team, member.memberKey, member.memberName);
            await repo.ProcessAsyncUpdatesForPlayers();

            await AssertMergedPlayers(targetPlayer, playerWithIdentityToLink, movedIdentityResult);
        }

        private async Task AssertMergedPlayers(Player targetPlayer, Player playerWithIdentityToLink, MovedPlayerIdentity movedIdentityResult)
        {
            Assert.Equal(targetPlayer.PlayerId, movedIdentityResult.PlayerIdForTargetPlayer);
            Assert.Equal(targetPlayer.PlayerRoute, movedIdentityResult.PreviousRouteForTargetPlayer);
            Assert.Equal(targetPlayer.MemberKey, movedIdentityResult.MemberKeyForTargetPlayer);

            Assert.Equal(playerWithIdentityToLink.PlayerId, movedIdentityResult.PlayerIdForSourcePlayer);
            Assert.Equal(playerWithIdentityToLink.PlayerRoute, movedIdentityResult.PreviousRouteForSourcePlayer);
            Assert.Equal(playerWithIdentityToLink.MemberKey, movedIdentityResult.MemberKeyForSourcePlayer);

            Assert.Equal(playerWithIdentityToLink.PlayerRoute, movedIdentityResult.NewRouteForTargetPlayer);

            using (var connectionForAssert = _connectionFactory.CreateDatabaseConnection())
            {
                connectionForAssert.Open();

                var targetPlayerUpdated = await connectionForAssert.QuerySingleAsync<(string PlayerRoute, Guid? MemberKey)>($"SELECT PlayerRoute, MemberKey FROM {Tables.Player} WHERE PlayerId = @PlayerId", targetPlayer);
                Assert.Equal(playerWithIdentityToLink.PlayerRoute, targetPlayerUpdated.PlayerRoute);
                Assert.Null(targetPlayerUpdated.MemberKey);

                var expectedIdentities = targetPlayer.PlayerIdentities.Select(pi => pi).ToList();
                expectedIdentities.Add(playerWithIdentityToLink.PlayerIdentities[0]);
                foreach (var identity in expectedIdentities)
                {
                    var playerIdentity = await connectionForAssert.QuerySingleAsync<(Guid PlayerId, string LinkedBy)>($"SELECT PlayerId, LinkedBy FROM {Tables.PlayerIdentity} WHERE PlayerIdentityId = @PlayerIdentityId", identity);
                    Assert.Equal(targetPlayer.PlayerId, playerIdentity.PlayerId);
                    Assert.Equal(PlayerIdentityLinkedBy.Team.ToString(), playerIdentity.LinkedBy);

                    var playerIdsInStatistics = await connectionForAssert.QueryAsync<Guid>($"SELECT PlayerId FROM {Tables.PlayerInMatchStatistics} WHERE PlayerIdentityId = @PlayerIdentityId", identity);
                    foreach (var playerIdInStatistics in playerIdsInStatistics)
                    {
                        Assert.Equal(targetPlayer.PlayerId, playerIdInStatistics);
                    }

                    var playerRoutesInStatistics = await connectionForAssert.QueryAsync<string>($"SELECT PlayerRoute FROM {Tables.PlayerInMatchStatistics} WHERE PlayerId = @PlayerId", targetPlayer);
                    foreach (var route in playerRoutesInStatistics)
                    {
                        Assert.Equal(playerWithIdentityToLink.PlayerRoute, route);
                    }
                }

                var obsoletePlayerShouldBeRemoved = await connectionForAssert.QuerySingleOrDefaultAsync<int>($"SELECT COUNT(PlayerId) FROM {Tables.Player} WHERE PlayerId = @PlayerId", playerWithIdentityToLink);
                Assert.Equal(0, obsoletePlayerShouldBeRemoved);
            }
        }

        [Fact]
        public async Task LinkPlayerIdentity_merges_players_if_target_is_linked_to_other_identities_on_the_same_team()
        {
            var playerWithIdentityToLink = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity(p => _testData.Players.Any(p2 => p2.IsOnTheSameTeamAs(p) &&
                                                                                                                                    !p2.MemberKey.HasValue &&
                                                                                                                                    p2.PlayerIdentities.Count > 1 &&
                                                                                                                                    p2.PlayerIdentities.Count == p2.PlayerIdentities.Where(x => x.LinkedBy == PlayerIdentityLinkedBy.Team).Count()
                                                                                                                                ));
            var targetPlayer = _testData.AnyPlayerNotLinkedToMemberWithMultipleIdentities(p => p.IsOnTheSameTeamAs(playerWithIdentityToLink) &&
                                                                                          p.PlayerIdentities.Count == p.PlayerIdentities.Where(x => x.LinkedBy == PlayerIdentityLinkedBy.Team).Count());
            SetupMocksForLinkPlayerIdentity(targetPlayer, playerWithIdentityToLink);

            var repo = CreateRepository();
            var movedIdentityResult = await repo.LinkPlayerIdentity(targetPlayer.PlayerId!.Value, playerWithIdentityToLink.PlayerIdentities[0].PlayerIdentityId!.Value, PlayerIdentityLinkedBy.Team, Guid.NewGuid(), "Member name");
            await repo.ProcessAsyncUpdatesForPlayers();

            await AssertMergedPlayers(targetPlayer, playerWithIdentityToLink, movedIdentityResult);
        }


        [Fact]
        public async Task LinkPlayerIdentity_redirects_deleted_player_to_linked_player()
        {
            var player1 = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity();
            var player2 = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity(p => p.PlayerId != player1.PlayerId);
            SetupMocksForLinkPlayerIdentity(player1, player2);

            var member = _testData.AnyMemberNotLinkedToPlayer();

            var repo = CreateRepository();
            await repo.LinkPlayerIdentity(player1.PlayerId!.Value, player2.PlayerIdentities[0].PlayerIdentityId!.Value, PlayerIdentityLinkedBy.Team, member.memberKey, member.memberName).ConfigureAwait(false);

            _redirectsRepository.VerifyPlayerIsRedirected(player1.PlayerRoute!, player2.PlayerRoute!);
        }

        [Fact]
        public async Task LinkPlayerIdentity_audits_and_logs()
        {
            var player1 = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity();
            var player2 = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity(p => p.PlayerId != player1.PlayerId);

            SetupMocksForLinkPlayerIdentity(player1, player2);

            var member = _testData.AnyMemberNotLinkedToPlayer();

            var repo = CreateRepository();
            await repo.LinkPlayerIdentity(player1.PlayerId!.Value, player2.PlayerIdentities[0].PlayerIdentityId!.Value, PlayerIdentityLinkedBy.Team, member.memberKey, member.memberName).ConfigureAwait(false);

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
    }
}
