using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Moq;
using Stoolball.Logging;
using Stoolball.Routing;
using Stoolball.Statistics;
using Xunit;

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
        private readonly Mock<IPlayerCacheClearer> _playerCacheClearer = new();

        public SqlServerPlayerRepositoryUnitTests()
        {
            _connectionFactory.Setup(x => x.CreateDatabaseConnection()).Returns(_databaseConnection.Object);
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

        [Fact]
        public async Task ProcessAsyncUpdates_allows_3_retries_for_SQL_timeouts_and_logs_warnings()
        {
            _dapperWrapper.Setup(x => x.QueryAsync<string>(SqlServerPlayerRepository.PROCESS_ASYNC_STORED_PROCEDURE, CommandType.StoredProcedure, _databaseConnection.Object)).Throws(SqlExceptionFactory.Create(SqlExceptionType.Timeout));

            var repo = CreateRepository();

            await repo.ProcessAsyncUpdatesForLinkingAndUnlinkingPlayersToMemberAccounts();

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

            await repo.ProcessAsyncUpdatesForLinkingAndUnlinkingPlayersToMemberAccounts();

            foreach (var route in affectedRoutesFirstIteration)
            {
                _playerCacheClearer.Verify(x => x.ClearCacheForPlayer(It.Is<Player>(x => x.PlayerRoute == route)), Times.Once);
            }
            foreach (var route in affectedRoutesSecondIteration)
            {
                _playerCacheClearer.Verify(x => x.ClearCacheForPlayer(It.Is<Player>(x => x.PlayerRoute == route)), Times.Once);
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

            await repo.ProcessAsyncUpdatesForLinkingAndUnlinkingPlayersToMemberAccounts();

            _logger.Verify(x => x.Info(SqlServerPlayerRepository.LOG_TEMPLATE_INFO_PLAYERS_AFFECTED, It.Is<string>(x => x == string.Join(", ", affectedRoutes))), Times.Once);
            _logger.Verify(x => x.Info(SqlServerPlayerRepository.LOG_TEMPLATE_INFO_PLAYERS_AFFECTED, It.Is<string>(x => x == "None")), Times.Once);
        }

        [Fact]
        public async Task ProcessAsyncUpdates_logs_SQL_connection_error_and_exits()
        {
            _dapperWrapper.Setup(x => x.QueryAsync<string>(SqlServerPlayerRepository.PROCESS_ASYNC_STORED_PROCEDURE, CommandType.StoredProcedure, _databaseConnection.Object)).Throws(SqlExceptionFactory.Create(SqlExceptionType.Connection));

            var repo = CreateRepository();

            await repo.ProcessAsyncUpdatesForLinkingAndUnlinkingPlayersToMemberAccounts();

            _dapperWrapper.Verify(x => x.QueryAsync<string>(SqlServerPlayerRepository.PROCESS_ASYNC_STORED_PROCEDURE, CommandType.StoredProcedure, _databaseConnection.Object), Times.Exactly(1));
            _logger.Verify(x => x.Error(
                SqlServerPlayerRepository.LOG_TEMPLATE_ERROR_SQL_EXCEPTION,
                SqlExceptionFactory.ERROR_ESTABLISHING_CONNECTION
            ), Times.Exactly(1));
        }
    }
}
