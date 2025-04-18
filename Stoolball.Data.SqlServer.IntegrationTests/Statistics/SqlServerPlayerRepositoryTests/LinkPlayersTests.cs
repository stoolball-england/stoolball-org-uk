﻿using System;
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
    public class LinkPlayersTests(SqlServerTestDataFixture _fixture) : IDisposable
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

        #region Never allowed
        [Fact]
        public async Task LinkPlayers_throws_InvalidOperationException_if_player_to_link_is_same_as_target_player()
        {
            var player = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity();
            SetupMocksForLinkPlayerIdentity(player, player);

            var repo = CreateRepository();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await repo.LinkPlayers(player.PlayerId!.Value, player.PlayerId!.Value, PlayerIdentityLinkedBy.Team, Guid.NewGuid(), "Member name"));
        }

        [Fact]
        public async Task LinkPlayers_throws_InvalidOperationException_if_both_players_are_linked_to_a_member()
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
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await repo.LinkPlayers(player1.PlayerId!.Value, player2.PlayerId!.Value, PlayerIdentityLinkedBy.Team, Guid.NewGuid(), "Member name").ConfigureAwait(false));
        }

        [Theory]
        [InlineData(true, 1, false)]
        [InlineData(true, 2, false)]
        [InlineData(false, 1, true)]
        [InlineData(false, 1, false)]
        [InlineData(false, 2, true)]
        [InlineData(false, 2, false)]
        public async Task LinkPlayers_throws_InvalidOperationException_if_player_to_link_has_multiple_identities(
            bool targetPlayerIsLinkedToAnyMember,
            int totalIdentitiesForTargetPlayer,
            bool identityToLinkIsLinkedToAnyMember)
        {
            Func<Player, Player, bool> selectPlayerToLinkOnTheSameTeam =
              (playerToMatch, playerOnTheSameTeam) => playerToMatch.MemberKey.HasValue == identityToLinkIsLinkedToAnyMember &&
                                                      playerToMatch.IsOnTheSameTeamAs(playerOnTheSameTeam) &&
                                                      playerToMatch.PlayerIdentities.Count > 1 &&
                                                      playerToMatch.PlayerIdentities.All(pi => pi.LinkedBy == ExpectedLinkedBy(identityToLinkIsLinkedToAnyMember, true));

            var targetPlayer = _testData.Players.First(p => p.MemberKey.HasValue == targetPlayerIsLinkedToAnyMember &&
                                                            p.PlayerIdentities.Count == totalIdentitiesForTargetPlayer &&
                                                            p.PlayerIdentities.All(pi => pi.LinkedBy == ExpectedLinkedBy(targetPlayerIsLinkedToAnyMember, totalIdentitiesForTargetPlayer > 1)) &&
                                                            _testData.Players.Any(p2 => selectPlayerToLinkOnTheSameTeam(p2, p)));
            var playerToLink = _testData.Players.First(p => selectPlayerToLinkOnTheSameTeam(p, targetPlayer));

            SetupMocksForLinkPlayerIdentity(targetPlayer, playerToLink);

            var repo = CreateRepository();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await repo.LinkPlayers(targetPlayer.PlayerId!.Value, playerToLink.PlayerId!.Value, PlayerIdentityLinkedBy.Team, Guid.NewGuid(), "Member name"));
        }

        [Fact]
        public async Task LinkPlayers_throws_InvalidOperationException_if_member_for_player_to_link_has_linked_identities_on_multiple_teams()
        {
            Func<Player, Player, bool> selectPlayerToLinkOnTheSameTeam =
              (playerToMatch, playerOnTheSameTeam) => playerToMatch.MemberKey.HasValue &&
                                                      playerToMatch.IsOnTheSameTeamAs(playerOnTheSameTeam) &&
                                                      playerToMatch.PlayerIdentities.Count > 1 &&
                                                      playerToMatch.PlayerIdentities.All(pi => pi.LinkedBy == PlayerIdentityLinkedBy.Member) &&
                                                      playerToMatch.PlayerIdentities.Select(pi => pi.Team!.TeamId).Distinct().Count() > 1;

            var targetPlayer = _testData.AnyPlayerNotLinkedToMemberWithMultipleIdentities(p =>
                                                        p.PlayerIdentities.All(pi => pi.LinkedBy == PlayerIdentityLinkedBy.Team) &&
                                                        _testData.Players.Any(p2 => selectPlayerToLinkOnTheSameTeam(p2, p)));
            var playerToLink = _testData.Players.First(p => selectPlayerToLinkOnTheSameTeam(p, targetPlayer));

            SetupMocksForLinkPlayerIdentity(targetPlayer, playerToLink);

            var repo = CreateRepository();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await repo.LinkPlayers(targetPlayer.PlayerId!.Value, playerToLink.PlayerId!.Value, PlayerIdentityLinkedBy.Team, Guid.NewGuid(), "Member name"));
        }

        [Fact]
        public async Task LinkPlayers_throws_InvalidOperationException_if_target_player_does_not_have_an_identity_on_the_same_team_as_player_to_link()
        {
            var player1 = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity();
            var player2 = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity(p => !p.IsOnTheSameTeamAs(player1));
            SetupMocksForLinkPlayerIdentity(player1, player2);

            var repo = CreateRepository();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await repo.LinkPlayers(player1.PlayerId!.Value, player2.PlayerId!.Value, PlayerIdentityLinkedBy.Team, Guid.NewGuid(), "Member name"));
        }
        #endregion

        #region Not allowed, because either player is linked to a member who is not the current member

        [Fact]
        public async Task LinkPlayers_throws_InvalidOperationException_if_target_player_is_linked_to_a_member_who_is_not_the_current_member()
        {
            var repo = CreateRepository();

            var currentMember = _testData.AnyMemberLinkedToPlayer();
            var targetPlayer = _testData.AnyPlayerLinkedToMemberWithOnlyOneIdentity(p => p.MemberKey != currentMember.memberKey && _testData.Players.Any(p2 => p2.MemberKey is null && p2.IsOnTheSameTeamAs(p)));
            var playerToLink = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity(p => p.IsOnTheSameTeamAs(targetPlayer));
            SetupMocksForLinkPlayerIdentity(targetPlayer, playerToLink);

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await repo.LinkPlayers(targetPlayer.PlayerId!.Value, playerToLink.PlayerId!.Value, PlayerIdentityLinkedBy.Team, currentMember.memberKey, currentMember.memberName).ConfigureAwait(false));
        }

        [Fact]
        public async Task LinkPlayers_throws_InvalidOperationException_if_player_to_link_is_linked_to_a_member_who_is_not_the_current_member()
        {
            var repo = CreateRepository();

            var currentMember = _testData.AnyMemberLinkedToPlayer();
            var targetPlayer = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity(p => _testData.Players.Any(p2 => p2.MemberKey.HasValue && p2.MemberKey != currentMember.memberKey && p2.PlayerIdentities.Count == 1 && p2.IsOnTheSameTeamAs(p))); ;
            var playerToLink = _testData.AnyPlayerLinkedToMemberWithOnlyOneIdentity(p => p.MemberKey != currentMember.memberKey && p.IsOnTheSameTeamAs(targetPlayer));
            SetupMocksForLinkPlayerIdentity(targetPlayer, playerToLink);

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await repo.LinkPlayers(targetPlayer.PlayerId!.Value, playerToLink.PlayerId!.Value, PlayerIdentityLinkedBy.Team, currentMember.memberKey, currentMember.memberName));
        }
        #endregion

        #region Allowed, if either player is linked to the current member

        [Fact]
        public async Task LinkPlayers_merges_players_if_target_player_is_linked_to_current_member()
        {
            var repo = CreateRepository();

            var targetPlayer = _testData.AnyPlayerLinkedToMemberWithOnlyOneIdentity(p => _testData.Players.Any(p2 => p2.MemberKey is null && p2.IsOnTheSameTeamAs(p)));
            var playerToLink = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity(p => p.IsOnTheSameTeamAs(targetPlayer));
            var currentMember = targetPlayer.MemberKey!.Value;
            SetupMocksForLinkPlayerIdentity(targetPlayer, playerToLink);

            var movedIdentityResult = await repo.LinkPlayers(targetPlayer.PlayerId!.Value, playerToLink.PlayerId!.Value, PlayerIdentityLinkedBy.Team, currentMember, "Member name").ConfigureAwait(false);
            await repo.ProcessAsyncUpdatesForPlayers();

            await AssertMergedPlayers(targetPlayer, playerToLink, movedIdentityResult, true);
        }

        [Fact]
        public async Task LinkPlayers_merges_players_if_player_to_link_is_linked_to_a_member_if_it_is_the_current_member()
        {
            var repo = CreateRepository();

            Func<Player, Player, bool> selectPlayerToLinkOnTheSameTeam =
                (playerToMatch, playerOnTheSameTeam) => playerToMatch.MemberKey is not null &&
                                                        playerToMatch.IsOnTheSameTeamAs(playerOnTheSameTeam) &&
                                                        playerToMatch.PlayerIdentities.Count == 1 &&
                                                        playerToMatch.PlayerIdentities.All(pi => pi.LinkedBy == PlayerIdentityLinkedBy.Member);

            var targetPlayer = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity(p => p.PlayerIdentities[0].LinkedBy == PlayerIdentityLinkedBy.DefaultIdentity &&
                                                                                            _testData.Players.Any(p2 => selectPlayerToLinkOnTheSameTeam(p2, p)));
            var playerToLink = _testData.AnyPlayerLinkedToMember(p => selectPlayerToLinkOnTheSameTeam(p, targetPlayer));

            var currentMember = playerToLink.MemberKey!.Value;
            SetupMocksForLinkPlayerIdentity(targetPlayer, playerToLink);

            var movedIdentityResult = await repo.LinkPlayers(targetPlayer.PlayerId!.Value, playerToLink.PlayerId!.Value, PlayerIdentityLinkedBy.Team, currentMember, "Member name").ConfigureAwait(false);
            await repo.ProcessAsyncUpdatesForPlayers();

            await AssertMergedPlayers(targetPlayer, playerToLink, movedIdentityResult, true);
        }
        #endregion

        #region Always allowed
        [Fact]
        public async Task LinkPlayers_merges_two_default_identities()
        {
            var targetPlayer = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity();
            var playerToLink = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity(p => p.PlayerId != targetPlayer.PlayerId && p.IsOnTheSameTeamAs(targetPlayer));

            SetupMocksForLinkPlayerIdentity(targetPlayer, playerToLink);

            var member = _testData.AnyMemberNotLinkedToPlayer();

            var repo = CreateRepository();
            var movedIdentityResult = await repo.LinkPlayers(targetPlayer.PlayerId!.Value, playerToLink.PlayerId!.Value, PlayerIdentityLinkedBy.Team, member.memberKey, member.memberName);
            await repo.ProcessAsyncUpdatesForPlayers();

            await AssertMergedPlayers(targetPlayer, playerToLink, movedIdentityResult, false);
        }


        [Fact]
        public async Task LinkPlayers_merges_players_if_target_is_linked_to_other_identities_on_the_same_team()
        {
            var playerToLink = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity(p => p.PlayerIdentities[0].LinkedBy == PlayerIdentityLinkedBy.DefaultIdentity &&
                                                                                                        _testData.Players.Any(p2 => p2.IsOnTheSameTeamAs(p) &&
                                                                                                                                   !p2.MemberKey.HasValue &&
                                                                                                                                    p2.PlayerIdentities.Count > 1 &&
                                                                                                                                    p2.PlayerIdentities.All(x => x.LinkedBy == PlayerIdentityLinkedBy.Team)
                                                                                                                                ));
            var targetPlayer = _testData.AnyPlayerNotLinkedToMemberWithMultipleIdentities(p => p.IsOnTheSameTeamAs(playerToLink) &&
                                                                                               p.PlayerIdentities.All(x => x.LinkedBy == PlayerIdentityLinkedBy.Team));
            SetupMocksForLinkPlayerIdentity(targetPlayer, playerToLink);

            var repo = CreateRepository();
            var movedIdentityResult = await repo.LinkPlayers(targetPlayer.PlayerId!.Value, playerToLink.PlayerId!.Value, PlayerIdentityLinkedBy.Team, Guid.NewGuid(), "Member name");
            await repo.ProcessAsyncUpdatesForPlayers();

            await AssertMergedPlayers(targetPlayer, playerToLink, movedIdentityResult, false);
        }


        [Fact]
        public async Task LinkPlayers_redirects_deleted_player_to_linked_player()
        {
            var player1 = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity();
            var player2 = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity(p => p.PlayerId != player1.PlayerId);
            SetupMocksForLinkPlayerIdentity(player1, player2);

            var member = _testData.AnyMemberNotLinkedToPlayer();

            var repo = CreateRepository();
            await repo.LinkPlayers(player1.PlayerId!.Value, player2.PlayerId!.Value, PlayerIdentityLinkedBy.Team, member.memberKey, member.memberName).ConfigureAwait(false);

            _redirectsRepository.VerifyPlayerIsRedirected(player1.PlayerRoute!, player2.PlayerRoute!);
        }

        [Fact]
        public async Task LinkPlayers_audits_and_logs()
        {
            var player1 = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity();
            var player2 = _testData.AnyPlayerNotLinkedToMemberWithOnlyOneIdentity(p => p.PlayerId != player1.PlayerId);

            SetupMocksForLinkPlayerIdentity(player1, player2);

            var member = _testData.AnyMemberNotLinkedToPlayer();

            var repo = CreateRepository();
            await repo.LinkPlayers(player1.PlayerId!.Value, player2.PlayerId!.Value, PlayerIdentityLinkedBy.Team, member.memberKey, member.memberName).ConfigureAwait(false);

            // audits and logs delete of original player and update of target player
            _auditRepository.Verify(x => x.CreateAudit(It.Is<AuditRecord>(x => x.Action == AuditAction.Update && x.EntityUri!.ToString().EndsWith(player1.PlayerId.ToString()!)), It.IsAny<IDbTransaction>()), Times.Once);
            _logger.Verify(x => x.Info(LoggingTemplates.Updated, It.IsAny<string>(), member.memberName, member.memberKey, typeof(SqlServerPlayerRepository), nameof(SqlServerPlayerRepository.LinkPlayers)));

            var deletedPlayer = new Player { PlayerId = player2.PlayerId, PlayerRoute = player2.PlayerRoute };
            deletedPlayer.PlayerIdentities.Add(new PlayerIdentity { PlayerIdentityId = player2.PlayerIdentities[0].PlayerIdentityId });
            _auditRepository.Verify(x => x.CreateAudit(It.Is<AuditRecord>(x => x.Action == AuditAction.Delete && x.EntityUri!.ToString().EndsWith(player2.PlayerId.ToString()!)), It.IsAny<IDbTransaction>()), Times.Once);
            _logger.Verify(x => x.Info(LoggingTemplates.Deleted, JsonConvert.SerializeObject(deletedPlayer), member.memberName, member.memberKey, typeof(SqlServerPlayerRepository), nameof(SqlServerPlayerRepository.LinkPlayers)));
        }

        #endregion

        #region Helpers
        private async Task AssertMergedPlayers(Player targetPlayer, Player playerWithIdentityToLink, LinkPlayersResult movedIdentityResult, bool currentMemberWasLinkedToEitherPlayer)
        {
            Assert.Equal(targetPlayer.PlayerId, movedIdentityResult.PlayerIdForTargetPlayer);
            Assert.Equal(targetPlayer.PlayerRoute, movedIdentityResult.PreviousRouteForTargetPlayer);
            Assert.Equal(targetPlayer.MemberKey, movedIdentityResult.PreviousMemberKeyForTargetPlayer);

            Assert.Equal(playerWithIdentityToLink.PlayerId, movedIdentityResult.PlayerIdForSourcePlayer);
            Assert.Equal(playerWithIdentityToLink.PlayerRoute, movedIdentityResult.PreviousRouteForSourcePlayer);
            Assert.Equal(playerWithIdentityToLink.MemberKey, movedIdentityResult.PreviousMemberKeyForSourcePlayer);

            Assert.Equal(playerWithIdentityToLink.PlayerRoute, movedIdentityResult.NewRouteForTargetPlayer);
            Assert.Equal(targetPlayer.MemberKey ?? playerWithIdentityToLink.MemberKey, movedIdentityResult.NewMemberKeyForTargetPlayer);

            using (var connectionForAssert = _connectionFactory.CreateDatabaseConnection())
            {
                connectionForAssert.Open();

                var targetPlayerUpdated = await connectionForAssert.QuerySingleAsync<(string PlayerRoute, Guid? MemberKey)>($"SELECT PlayerRoute, MemberKey FROM {Tables.Player} WHERE PlayerId = @PlayerId", targetPlayer);
                Assert.Equal(playerWithIdentityToLink.PlayerRoute, targetPlayerUpdated.PlayerRoute);
                Assert.Equal(movedIdentityResult.NewMemberKeyForTargetPlayer, targetPlayerUpdated.MemberKey);

                var expectedIdentities = targetPlayer.PlayerIdentities.Select(pi => pi).ToList();
                expectedIdentities.AddRange(playerWithIdentityToLink.PlayerIdentities);
                foreach (var identity in expectedIdentities)
                {
                    var playerIdentity = await connectionForAssert.QuerySingleAsync<(Guid PlayerId, PlayerIdentityLinkedBy LinkedBy)>($"SELECT PlayerId, LinkedBy FROM {Tables.PlayerIdentity} WHERE PlayerIdentityId = @PlayerIdentityId", identity);
                    Assert.Equal(targetPlayer.PlayerId, playerIdentity.PlayerId);
                    if (currentMemberWasLinkedToEitherPlayer)
                    {
                        Assert.Equal(PlayerIdentityLinkedBy.Member, playerIdentity.LinkedBy);
                    }
                    else
                    {
                        Assert.Equal(PlayerIdentityLinkedBy.Team, playerIdentity.LinkedBy);
                    }

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

        private static PlayerIdentityLinkedBy ExpectedLinkedBy(bool isLinkedToMember, bool hasMultipleIdentities)
        {
            if (isLinkedToMember) { return PlayerIdentityLinkedBy.Member; }
            return hasMultipleIdentities ? PlayerIdentityLinkedBy.Team : PlayerIdentityLinkedBy.DefaultIdentity;
        }

        private void SetupMocksForLinkPlayerIdentity(Player player1, Player player2)
        {
            _routeSelector.Setup(x => x.SelectBestRoute(player2.PlayerRoute!, player1.PlayerRoute!)).Returns(player2.PlayerRoute!);
        }
        #endregion
    }
}
