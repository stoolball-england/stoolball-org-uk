using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Humanizer;
using Moq;
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

namespace Stoolball.Data.SqlServer.IntegrationTests.Statistics.SqlServerPlayerRepositoryTests
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class UpdatePlayerIdentityTests(SqlServerTestDataFixture _fixture) : IDisposable
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
    }
}
