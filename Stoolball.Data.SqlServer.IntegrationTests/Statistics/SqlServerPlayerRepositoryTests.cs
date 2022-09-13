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

        public SqlServerPlayerRepositoryTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
            _scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        }

        [Fact]
        public async Task CreateOrMatchPlayerIdentity_throws_ArgumentNullException_if_playerIdentity_is_null()
        {
            var repo = new SqlServerPlayerRepository(_databaseFixture.ConnectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger<SqlServerPlayerRepository>>(), Mock.Of<IRouteGenerator>(), Mock.Of<IStoolballEntityCopier>(), Mock.Of<IPlayerNameFormatter>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.CreateOrMatchPlayerIdentity(null, Guid.NewGuid(), "Member name", Mock.Of<IDbTransaction>()));
        }

        [Fact]
        public async Task CreateOrMatchPlayerIdentity_throws_ArgumentException_if_PlayerIdentityName_is_null()
        {
            var repo = new SqlServerPlayerRepository(_databaseFixture.ConnectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger<SqlServerPlayerRepository>>(), Mock.Of<IRouteGenerator>(), Mock.Of<IStoolballEntityCopier>(), Mock.Of<IPlayerNameFormatter>());

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.CreateOrMatchPlayerIdentity(new PlayerIdentity { PlayerIdentityName = null, Team = new Team { TeamId = Guid.NewGuid() } }, Guid.NewGuid(), "Member name", Mock.Of<IDbTransaction>()));
        }

        [Fact]
        public async Task CreateOrMatchPlayerIdentity_throws_ArgumentException_if_PlayerIdentityName_is_empty_string()
        {
            var repo = new SqlServerPlayerRepository(_databaseFixture.ConnectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger<SqlServerPlayerRepository>>(), Mock.Of<IRouteGenerator>(), Mock.Of<IStoolballEntityCopier>(), Mock.Of<IPlayerNameFormatter>());

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.CreateOrMatchPlayerIdentity(new PlayerIdentity { PlayerIdentityName = string.Empty, Team = new Team { TeamId = Guid.NewGuid() } }, Guid.NewGuid(), "Member name", Mock.Of<IDbTransaction>()));
        }

        [Fact]
        public async Task CreateOrMatchPlayerIdentity_throws_ArgumentNullException_if_memberName_is_null()
        {
            var repo = new SqlServerPlayerRepository(_databaseFixture.ConnectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger<SqlServerPlayerRepository>>(), Mock.Of<IRouteGenerator>(), Mock.Of<IStoolballEntityCopier>(), Mock.Of<IPlayerNameFormatter>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.CreateOrMatchPlayerIdentity(new PlayerIdentity { PlayerIdentityName = "Player 1", Team = new Team { TeamId = Guid.NewGuid() } }, Guid.NewGuid(), null, Mock.Of<IDbTransaction>()));
        }

        [Fact]
        public async Task CreateOrMatchPlayerIdentity_throws_ArgumentNullException_if_memberName_is_empty_string()
        {
            var repo = new SqlServerPlayerRepository(_databaseFixture.ConnectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger<SqlServerPlayerRepository>>(), Mock.Of<IRouteGenerator>(), Mock.Of<IStoolballEntityCopier>(), Mock.Of<IPlayerNameFormatter>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.CreateOrMatchPlayerIdentity(new PlayerIdentity { PlayerIdentityName = "Player 1", Team = new Team { TeamId = Guid.NewGuid() } }, Guid.NewGuid(), string.Empty, Mock.Of<IDbTransaction>()));
        }

        [Fact]
        public async Task CreateOrMatchPlayerIdentity_throws_ArgumentException_if_TeamId_is_null()
        {
            var repo = new SqlServerPlayerRepository(_databaseFixture.ConnectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger<SqlServerPlayerRepository>>(), Mock.Of<IRouteGenerator>(), Mock.Of<IStoolballEntityCopier>(), Mock.Of<IPlayerNameFormatter>());

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.CreateOrMatchPlayerIdentity(new PlayerIdentity { PlayerIdentityName = "Player 1" }, Guid.NewGuid(), "Member name", Mock.Of<IDbTransaction>()));
        }

        [Fact]
        public async Task CreateOrMatchPlayerIdentity_throws_ArgumentNullException_if_transaction_is_null()
        {
            var repo = new SqlServerPlayerRepository(_databaseFixture.ConnectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger<SqlServerPlayerRepository>>(), Mock.Of<IRouteGenerator>(), Mock.Of<IStoolballEntityCopier>(), Mock.Of<IPlayerNameFormatter>());

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
            var repo = new SqlServerPlayerRepository(_databaseFixture.ConnectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger<SqlServerPlayerRepository>>(), Mock.Of<IRouteGenerator>(), Mock.Of<IStoolballEntityCopier>(), Mock.Of<IPlayerNameFormatter>());
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

                    var repo = new SqlServerPlayerRepository(_databaseFixture.ConnectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger<SqlServerPlayerRepository>>(), Mock.Of<IRouteGenerator>(), Mock.Of<IStoolballEntityCopier>(), Mock.Of<IPlayerNameFormatter>());

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
                    var copier = new Mock<IStoolballEntityCopier>();
                    copier.Setup(x => x.CreateAuditableCopy(playerIdentity)).Returns(playerIdentity);
                    var playerNameFormatter = new Mock<IPlayerNameFormatter>();
                    playerNameFormatter.Setup(x => x.CapitaliseName(playerIdentity.PlayerIdentityName)).Returns(playerIdentity.PlayerIdentityName);
                    var routeGenerator = new Mock<IRouteGenerator>();
                    routeGenerator.Setup(x => x.GenerateUniqueRoute("/players", playerIdentity.PlayerIdentityName, NoiseWords.PlayerRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(playerRoute));

                    var repo = new SqlServerPlayerRepository(_databaseFixture.ConnectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger<SqlServerPlayerRepository>>(), routeGenerator.Object, copier.Object, playerNameFormatter.Object);

                    var result = await repo.CreateOrMatchPlayerIdentity(playerIdentity, Guid.NewGuid(), "Member name", transaction);

                    Assert.NotNull(result);
                    copier.Verify(x => x.CreateAuditableCopy(playerIdentity), Times.Once);
                    playerNameFormatter.Verify(x => x.CapitaliseName(playerIdentity.PlayerIdentityName));
                    routeGenerator.Verify(x => x.GenerateUniqueRoute("/players", playerIdentity.PlayerIdentityName, NoiseWords.PlayerRoute, It.IsAny<Func<string, Task<int>>>()));

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
            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(playerIdentity)).Returns(playerIdentity);
            var playerNameFormatter = new Mock<IPlayerNameFormatter>();
            playerNameFormatter.Setup(x => x.CapitaliseName(playerIdentity.PlayerIdentityName)).Returns(playerIdentity.PlayerIdentityName);
            var auditRepository = new Mock<IAuditRepository>();
            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute("/players", playerIdentity.PlayerIdentityName, NoiseWords.PlayerRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult($"/players/{Guid.NewGuid()}"));
            var logger = new Mock<ILogger<SqlServerPlayerRepository>>();
            var memberName = "Member name";
            var memberKey = Guid.NewGuid();

            var repo = new SqlServerPlayerRepository(_databaseFixture.ConnectionFactory, auditRepository.Object, logger.Object, routeGenerator.Object, copier.Object, playerNameFormatter.Object);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var result = await repo.CreateOrMatchPlayerIdentity(playerIdentity, memberKey, memberName, transaction);
                    transaction.Rollback();
                }
            }

            auditRepository.Verify(x => x.CreateAudit(It.IsAny<AuditRecord>(), It.IsAny<IDbTransaction>()), Times.Once);
            logger.Verify(x => x.Info(LoggingTemplates.Created, It.IsAny<Player>(), memberName, memberKey, typeof(SqlServerPlayerRepository), nameof(SqlServerPlayerRepository.CreateOrMatchPlayerIdentity)));
        }

        [Fact]
        public async Task LinkPlayerToMemberAccount_throws_ArgumentException_if_PlayerId_is_null()
        {
            var repo = new SqlServerPlayerRepository(_databaseFixture.ConnectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger<SqlServerPlayerRepository>>(), Mock.Of<IRouteGenerator>(), Mock.Of<IStoolballEntityCopier>(), Mock.Of<IPlayerNameFormatter>());

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.LinkPlayerToMemberAccount(new Player(), Guid.NewGuid(), null));
        }

        [Fact]
        public async Task LinkPlayerToMemberAccount_throws_ArgumentNullException_if_memberName_is_null()
        {
            var repo = new SqlServerPlayerRepository(_databaseFixture.ConnectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger<SqlServerPlayerRepository>>(), Mock.Of<IRouteGenerator>(), Mock.Of<IStoolballEntityCopier>(), Mock.Of<IPlayerNameFormatter>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.LinkPlayerToMemberAccount(new Player { PlayerId = Guid.NewGuid() }, Guid.NewGuid(), null));
        }

        [Fact]
        public async Task LinkPlayerToMemberAccount_throws_ArgumentNullException_if_memberName_is_empty_string()
        {
            var repo = new SqlServerPlayerRepository(_databaseFixture.ConnectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger<SqlServerPlayerRepository>>(), Mock.Of<IRouteGenerator>(), Mock.Of<IStoolballEntityCopier>(), Mock.Of<IPlayerNameFormatter>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.LinkPlayerToMemberAccount(new Player { PlayerId = Guid.NewGuid() }, Guid.NewGuid(), string.Empty));
        }

        [Fact]
        public async Task LinkPlayerToMemberAccount_where_member_has_no_previous_player_updates_player()
        {
            Guid memberWithNoExistingPlayer;
            Guid playerNotLinkedToMember;
            using (var connectionForArrange = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                connectionForArrange.Open();
                memberWithNoExistingPlayer = await connectionForArrange.QuerySingleAsync<Guid>($"SELECT TOP 1 uniqueId FROM umbracoNode WHERE id IN (SELECT nodeId from cmsMember) AND uniqueId NOT IN (SELECT DISTINCT MemberKey FROM {Tables.Player} WHERE MemberKey IS NOT NULL)");
                playerNotLinkedToMember = await connectionForArrange.QuerySingleAsync<Guid>($"SELECT TOP 1 PlayerId FROM {Tables.Player} WHERE MemberKey IS NULL");
            }

            var player = new Player { PlayerId = playerNotLinkedToMember };
            var playerCopy = new Player { PlayerId = player.PlayerId };
            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(player)).Returns(playerCopy);

            var repo = new SqlServerPlayerRepository(_databaseFixture.ConnectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger<SqlServerPlayerRepository>>(), Mock.Of<IRouteGenerator>(), copier.Object, Mock.Of<IPlayerNameFormatter>());
            await repo.LinkPlayerToMemberAccount(player, memberWithNoExistingPlayer, "Member name");

            using (var connectionForAssert = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                connectionForAssert.Open();
                var result = await connectionForAssert.QuerySingleAsync<Guid>($"SELECT MemberKey FROM {Tables.Player} WHERE PlayerId = @PlayerId", playerCopy);

                Assert.Equal(memberWithNoExistingPlayer, result);
            }
        }


        [Fact]
        public async Task LinkPlayerToMemberAccount_where_member_has_previous_player_merges_players()
        {
            (Guid memberKey, Guid playerId) memberWithExistingPlayer;
            Guid playerNotLinkedToMember;
            using (var connectionForArrange = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                connectionForArrange.Open();
                memberWithExistingPlayer = await connectionForArrange.QuerySingleAsync<(Guid, Guid)>($"SELECT TOP 1 MemberKey, PlayerId FROM {Tables.Player} WHERE MemberKey IS NOT NULL");
                playerNotLinkedToMember = await connectionForArrange.QuerySingleAsync<Guid>($"SELECT TOP 1 p.* FROM {Tables.Player} p INNER JOIN {Tables.PlayerIdentity} pi ON p.PlayerId = pi.PlayerId WHERE p.MemberKey IS NULL");
            }

            var playerAlreadyLinked = _databaseFixture.TestData.Players.Single(x => x.PlayerId == memberWithExistingPlayer.playerId);
            var playerAlreadyLinkedCopy = new Player { PlayerId = playerAlreadyLinked.PlayerId };
            var secondPlayer = _databaseFixture.TestData.Players.Single(x => x.PlayerId == playerNotLinkedToMember);
            var secondPlayerCopy = new Player { PlayerId = secondPlayer.PlayerId };
            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(playerAlreadyLinked)).Returns(playerAlreadyLinkedCopy);
            copier.Setup(x => x.CreateAuditableCopy(secondPlayer)).Returns(secondPlayerCopy);

            var repo = new SqlServerPlayerRepository(_databaseFixture.ConnectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger<SqlServerPlayerRepository>>(), Mock.Of<IRouteGenerator>(), copier.Object, Mock.Of<IPlayerNameFormatter>());
            await repo.LinkPlayerToMemberAccount(secondPlayer, memberWithExistingPlayer.memberKey, "Member name");

            using (var connectionForAssert = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                connectionForAssert.Open();

                var originalPlayerStillLinkedToMember = await connectionForAssert.QuerySingleAsync<Guid>($"SELECT PlayerId FROM {Tables.Player} WHERE MemberKey = @memberKey", new { memberWithExistingPlayer.memberKey });
                Assert.Equal(memberWithExistingPlayer.playerId, originalPlayerStillLinkedToMember);

                foreach (var identity in secondPlayer.PlayerIdentities)
                {
                    var alreadyLinkedPlayerIdNowLinkedToPlayerIdentityFromSecondPlayer = await connectionForAssert.QuerySingleAsync<Guid>($"SELECT PlayerId FROM {Tables.PlayerIdentity} WHERE PlayerIdentityId = @PlayerIdentityId", identity);
                    Assert.Equal(playerAlreadyLinked.PlayerId, alreadyLinkedPlayerIdNowLinkedToPlayerIdentityFromSecondPlayer);

                    var playerIdsForIdentitiesBelongingToSecondPlayer = await connectionForAssert.QueryAsync<Guid>($"SELECT PlayerId FROM {Tables.PlayerInMatchStatistics} WHERE PlayerIdentityId = @PlayerIdentityId", identity);
                    foreach (var playerIdNowLinkedToSecondPlayerIdentityInStatistics in playerIdsForIdentitiesBelongingToSecondPlayer)
                    {
                        Assert.Equal(playerAlreadyLinked.PlayerId, playerIdNowLinkedToSecondPlayerIdentityInStatistics);
                    }
                }

                var secondPlayerShouldBeRemoved = await connectionForAssert.QuerySingleOrDefaultAsync<int>($"SELECT COUNT(PlayerId) FROM {Tables.Player} WHERE PlayerId = @PlayerId", secondPlayerCopy);
                Assert.Equal(0, secondPlayerShouldBeRemoved);
            }
        }

        [Fact]
        public async Task LinkPlayerToMemberAccount_audits_and_logs()
        {
            var player = _databaseFixture.TestData.Players.First();
            var playerCopy = new Player { PlayerId = player.PlayerId };
            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(player)).Returns(playerCopy);
            var auditRepository = new Mock<IAuditRepository>();
            var logger = new Mock<ILogger<SqlServerPlayerRepository>>();
            var memberName = "Member name";
            var memberKey = _databaseFixture.TestData.Members.First().memberId;

            var repo = new SqlServerPlayerRepository(_databaseFixture.ConnectionFactory, auditRepository.Object, logger.Object, Mock.Of<IRouteGenerator>(), copier.Object, Mock.Of<IPlayerNameFormatter>());
            await repo.LinkPlayerToMemberAccount(player, memberKey, memberName);

            auditRepository.Verify(x => x.CreateAudit(It.IsAny<AuditRecord>(), It.IsAny<IDbTransaction>()), Times.Once);
            logger.Verify(x => x.Info(LoggingTemplates.Updated, JsonConvert.SerializeObject(playerCopy), memberName, memberKey, typeof(SqlServerPlayerRepository), nameof(SqlServerPlayerRepository.LinkPlayerToMemberAccount)));
        }

        public void Dispose() => _scope.Dispose();
    }
}
