using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Logging;
using Stoolball.Routing;
using Stoolball.Statistics;
using Stoolball.Teams;
using Xunit;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer.UnitTests
{
    public class SqlServerPlayerRepositoryUnitTests
    {
        private readonly Mock<IDatabaseConnectionFactory> _connectionFactory = new();
        private readonly Mock<IDbConnection> _databaseConnection = new();
        private readonly Mock<IDapperWrapper> _dapperWrapper = new();
        private readonly Mock<IAuditRepository> _auditRepository = new();
        private readonly Mock<ILogger<SqlServerPlayerRepository>> _logger = new();
        private readonly Mock<IStoolballEntityCopier> _copier = new();
        private readonly Mock<IPlayerNameFormatter> _playerNameFormatter = new();
        private readonly Mock<IRouteGenerator> _routeGenerator = new();
        private readonly Mock<IBestRouteSelector> _routeSelector = new();
        private readonly Mock<IRedirectsRepository> _redirectsRepository = new();
        private readonly Mock<IPlayerCacheInvalidator> _playerCacheClearer = new();
        private readonly Mock<IDbTransaction> _transaction = new();

        public SqlServerPlayerRepositoryUnitTests()
        {
            _connectionFactory.Setup(x => x.CreateDatabaseConnection()).Returns(_databaseConnection.Object);
            _databaseConnection.Setup(x => x.BeginTransaction()).Returns(_transaction.Object);
        }

        private SqlServerPlayerRepository CreateRepository()
        {
            return new SqlServerPlayerRepository(_connectionFactory.Object,
                _dapperWrapper.Object,
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
        private static PlayerIdentity CreateValidPlayerIdentity() => new()
        {
            PlayerIdentityId = Guid.NewGuid(),
            PlayerIdentityName = "John Smith",
            Player = CreateValidPlayer()
        };

        private static Player CreateValidPlayer() => new()
        {
            PlayerId = Guid.NewGuid(),
            PlayerRoute = "/players/example"
        };

        [Fact]
        public async Task LinkPlayerIdentity_throws_ArgumentException_if_target_PlayerId_is_null()
        {
            var repo = CreateRepository();

            var invalidPlayer = CreateValidPlayer();
            invalidPlayer.PlayerId = null;

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.LinkPlayerIdentity(invalidPlayer, Guid.NewGuid(), PlayerIdentityLinkedBy.ClubOrTeam, Guid.NewGuid(), "Member name"));
        }


        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task LinkPlayerIdentity_throws_ArgumentException_if_target_PlayerRoute_is_missing(string playerRoute)
        {
            var repo = CreateRepository();

            var invalidPlayer = CreateValidPlayer();
            invalidPlayer.PlayerRoute = playerRoute;

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.LinkPlayerIdentity(invalidPlayer, Guid.NewGuid(), PlayerIdentityLinkedBy.ClubOrTeam, Guid.NewGuid(), "Member name"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task LinkPlayerIdentity_throws_ArgumentNullException_if_memberName_is_missing(string memberName)
        {
            var repo = CreateRepository();

            var validPlayer = CreateValidPlayer();
            var validIdentity = CreateValidPlayerIdentity();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.LinkPlayerIdentity(validPlayer, Guid.NewGuid(), PlayerIdentityLinkedBy.ClubOrTeam, Guid.NewGuid(), memberName));
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
        public async Task UnlinkPlayerIdentity_throws_ArgumentException_if_PlayerIdentityId_is_null()
        {
            var repo = CreateRepository();

            var invalidIdentity = CreateValidPlayerIdentity();
            invalidIdentity.PlayerIdentityId = null;

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.UnlinkPlayerIdentity(invalidIdentity, Guid.NewGuid(), "Member name"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task UnlinkPlayerIdentity_throws_ArgumentException_if_PlayerIdentityName_is_null_or_empty(string? name)
        {
            var repo = CreateRepository();

            var invalidIdentity = CreateValidPlayerIdentity();
            invalidIdentity.PlayerIdentityName = name;

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.UnlinkPlayerIdentity(invalidIdentity, Guid.NewGuid(), "Member name"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task UnlinkPlayerIdentity_throws_ArgumentNullException_if_memberName_is_missing(string memberName)
        {
            var repo = CreateRepository();

            var validIdentity = CreateValidPlayerIdentity();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.UnlinkPlayerIdentity(validIdentity, Guid.NewGuid(), memberName));
        }

#nullable disable
        [Fact]
        public async Task UpdatePlayerIdentity_throws_ArgumentNullException_if_playerIdentity_is_null()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.UpdatePlayerIdentity(null, Guid.NewGuid(), "Member name"));
        }

        [Fact]
        public async Task UpdatePlayerIdentity_throws_ArgumentException_if_PlayerIdentityId_is_null()
        {
            var repo = CreateRepository();
            var playerIdentity = new PlayerIdentity { PlayerIdentityName = "Example name", Team = new Team { TeamId = Guid.NewGuid() }, Player = new Player { PlayerId = Guid.NewGuid() } };

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.UpdatePlayerIdentity(playerIdentity, Guid.NewGuid(), "Member name"));
        }


        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task UpdatePlayerIdentity_throws_ArgumentException_if_PlayerIdentityName_is_missing(string? playerIdentityName)
        {
            var repo = CreateRepository();
            var playerIdentity = new PlayerIdentity { PlayerIdentityId = Guid.NewGuid(), PlayerIdentityName = playerIdentityName, Team = new Team { TeamId = Guid.NewGuid() }, Player = new Player { PlayerId = Guid.NewGuid() } };

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.UpdatePlayerIdentity(playerIdentity, Guid.NewGuid(), "Member name"));
        }

        [Fact]
        public async Task UpdatePlayerIdentity_throws_ArgumentException_if_TeamId_is_null()
        {
            var repo = CreateRepository();
            var playerIdentity = new PlayerIdentity { PlayerIdentityId = Guid.NewGuid(), PlayerIdentityName = "Example name", Team = new Team(), Player = new Player { PlayerId = Guid.NewGuid() } };

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.UpdatePlayerIdentity(playerIdentity, Guid.NewGuid(), "Member name"));
        }


        [Fact]
        public async Task UpdatePlayerIdentity_throws_ArgumentException_if_PlayerId_is_null()
        {
            var repo = CreateRepository();
            var playerIdentity = new PlayerIdentity { PlayerIdentityId = Guid.NewGuid(), PlayerIdentityName = "Example name", Team = new Team { TeamId = Guid.NewGuid() }, Player = new Player() };

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.UpdatePlayerIdentity(playerIdentity, Guid.NewGuid(), "Member name"));
        }
#nullable enable

        [Fact]
        public async Task ProcessAsyncUpdates_allows_3_retries_for_SQL_timeouts_and_logs_warnings()
        {
            _dapperWrapper.Setup(x => x.QueryAsync<string>(SqlServerPlayerRepository.PROCESS_ASYNC_STORED_PROCEDURE, CommandType.StoredProcedure, _databaseConnection.Object)).Throws(SqlExceptionFactory.Create(SqlExceptionType.Timeout));

            var repo = CreateRepository();

            await repo.ProcessAsyncUpdatesForPlayers();

            _dapperWrapper.Verify(x => x.QueryAsync<string>(SqlServerPlayerRepository.PROCESS_ASYNC_STORED_PROCEDURE, CommandType.StoredProcedure, _databaseConnection.Object), Times.Exactly(4));
            _logger.Verify(x => x.Warn(SqlServerPlayerRepository.LOG_TEMPLATE_WARN_SQL_TIMEOUT, It.IsAny<int>()), Times.Exactly(4));
        }

        [Fact]
        public async Task ProcessAsyncUpdates_clears_cache_for_affected_routes()
        {
            var affectedRoutesFirstIteration = new[] { "/players/one", "/players/two" };
            var affectedRoutesSecondIteration = new[] { "/players/three", "/players/four" };

            _dapperWrapper.SetupSequence(x => x.QueryAsync<string>(SqlServerPlayerRepository.PROCESS_ASYNC_STORED_PROCEDURE, CommandType.StoredProcedure, _databaseConnection.Object))
                .Returns(Task.FromResult(affectedRoutesFirstIteration as IEnumerable<string>))
                .Returns(Task.FromResult(affectedRoutesSecondIteration as IEnumerable<string>))
                .Returns(Task.FromResult(Array.Empty<string>() as IEnumerable<string>));

            var repo = CreateRepository();

            await repo.ProcessAsyncUpdatesForPlayers();

            foreach (var route in affectedRoutesFirstIteration)
            {
                _playerCacheClearer.Verify(x => x.InvalidateCacheForPlayer(It.Is<Player>(x => x.PlayerRoute == route)), Times.Once);
            }
            foreach (var route in affectedRoutesSecondIteration)
            {
                _playerCacheClearer.Verify(x => x.InvalidateCacheForPlayer(It.Is<Player>(x => x.PlayerRoute == route)), Times.Once);
            }
        }


        [Fact]
        public async Task ProcessAsyncUpdates_logs_affected_routes()
        {
            var affectedRoutes = new[] { "/players/one", "/players/two" };

            _dapperWrapper.SetupSequence(x => x.QueryAsync<string>(SqlServerPlayerRepository.PROCESS_ASYNC_STORED_PROCEDURE, CommandType.StoredProcedure, _databaseConnection.Object))
                .Returns(Task.FromResult(affectedRoutes as IEnumerable<string>))
                .Returns(Task.FromResult(Array.Empty<string>() as IEnumerable<string>));

            var repo = CreateRepository();

            await repo.ProcessAsyncUpdatesForPlayers();

            _logger.Verify(x => x.Info(SqlServerPlayerRepository.LOG_TEMPLATE_INFO_PLAYERS_AFFECTED, It.Is<string>(x => x == string.Join(", ", affectedRoutes))), Times.Once);
            _logger.Verify(x => x.Info(SqlServerPlayerRepository.LOG_TEMPLATE_INFO_PLAYERS_AFFECTED, It.Is<string>(x => x == "None")), Times.Once);
        }

        [Fact]
        public async Task ProcessAsyncUpdates_logs_SQL_connection_error_and_exits()
        {
            _dapperWrapper.Setup(x => x.QueryAsync<string>(SqlServerPlayerRepository.PROCESS_ASYNC_STORED_PROCEDURE, CommandType.StoredProcedure, _databaseConnection.Object)).Throws(SqlExceptionFactory.Create(SqlExceptionType.Connection));

            var repo = CreateRepository();

            await repo.ProcessAsyncUpdatesForPlayers();

            _dapperWrapper.Verify(x => x.QueryAsync<string>(SqlServerPlayerRepository.PROCESS_ASYNC_STORED_PROCEDURE, CommandType.StoredProcedure, _databaseConnection.Object), Times.Exactly(1));
            _logger.Verify(x => x.Error(
                SqlServerPlayerRepository.LOG_TEMPLATE_ERROR_SQL_EXCEPTION,
                SqlExceptionFactory.ERROR_ESTABLISHING_CONNECTION
            ), Times.Exactly(1));
        }

        [Fact]
        public async Task UpdatePlayerIdentity_where_name_does_not_match_existing_player_identity_audits_and_logs()
        {
            var repo = CreateRepository();

            var playerIdentityToUpdate = new PlayerIdentity
            {
                PlayerIdentityId = Guid.NewGuid(),
                PlayerIdentityName = "New name",
                Player = new Player
                {
                    PlayerId = Guid.NewGuid(),
                },
                Team = new Team
                {
                    TeamId = Guid.NewGuid()
                }
            };
            var memberKey = Guid.NewGuid();
            var memberName = "Member name";

            _copier.Setup(x => x.CreateAuditableCopy(playerIdentityToUpdate.Player)).Returns(new Player
            {
                PlayerId = playerIdentityToUpdate.Player.PlayerId
            });
            _copier.Setup(x => x.CreateAuditableCopy(playerIdentityToUpdate)).Returns(new PlayerIdentity
            {
                PlayerIdentityId = playerIdentityToUpdate.PlayerIdentityId,
                PlayerIdentityName = playerIdentityToUpdate.PlayerIdentityName,
                Player = playerIdentityToUpdate.Player,
                Team = playerIdentityToUpdate.Team
            });
            _playerNameFormatter.Setup(x => x.CapitaliseName(playerIdentityToUpdate.PlayerIdentityName)).Returns(playerIdentityToUpdate.PlayerIdentityName);
            _dapperWrapper.Setup(x => x.QueryAsync<(string, string, int)>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IDbTransaction>())).ReturnsAsync(new[] { ("/players/example-player", "Example player", 10) });

            var result = await repo.UpdatePlayerIdentity(playerIdentityToUpdate, memberKey, memberName);

            _auditRepository.Verify(x => x.CreateAudit(It.Is<AuditRecord>(a => a.Action == AuditAction.Update), _transaction.Object), Times.Once);
            _logger.Verify(x => x.Info(LoggingTemplates.Updated, It.IsAny<string>(), memberName, memberKey, typeof(SqlServerPlayerRepository), nameof(SqlServerPlayerRepository.UpdatePlayerIdentity)), Times.Once);
        }
    }
}
