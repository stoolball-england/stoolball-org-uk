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
using Stoolball.Data.Abstractions.Models;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Data.SqlServer.IntegrationTests.Redirects;
using Stoolball.Logging;
using Stoolball.Matches;
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
    public class SqlServerPlayerRepositoryTests(SqlServerTestDataFixture _fixture) : IDisposable
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
                    PlayerIdentities = new PlayerIdentityList(playerIdentityToUpdate.Player.PlayerIdentities.Select(x => new PlayerIdentity
                    {
                        PlayerIdentityId = x.PlayerIdentityId,
                        PlayerIdentityName = x.PlayerIdentityName
                    }))
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

        #region LinkPlayerIdentity
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
                                                _testData.PlayerInnings.Any(pi => pi.DismissedBy?.PlayerIdentityId == x.PlayerIdentityId && pi.DismissalType == DismissalType.Caught ||
                                                                                                  pi.Bowler?.PlayerIdentityId == x.PlayerIdentityId && pi.DismissalType == DismissalType.CaughtAndBowled) &&
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
