using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Newtonsoft.Json;
using Stoolball.Data.Abstractions;
using Stoolball.Logging;
using Stoolball.Routing;
using Stoolball.Statistics;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// Writes stoolball player data to the Umbraco database
    /// </summary>
    public class SqlServerPlayerRepository : IPlayerRepository
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IDapperWrapper _dapperWrapper;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger<SqlServerPlayerRepository> _logger;
        private readonly IRedirectsRepository _redirectsRepository;
        private readonly IRouteGenerator _routeGenerator;
        private readonly IStoolballEntityCopier _copier;
        private readonly IPlayerNameFormatter _playerNameFormatter;
        private readonly IBestRouteSelector _bestRouteSelector;
        private readonly IPlayerCacheInvalidator _playerCacheClearer;
        internal const string PROCESS_ASYNC_STORED_PROCEDURE = "usp_Link_Player_To_Member_Async_Update";
        internal const string LOG_TEMPLATE_WARN_SQL_TIMEOUT = nameof(ProcessAsyncUpdatesForLinkingAndUnlinkingPlayersToMemberAccounts) + " running. Caught SQL Timeout. {allowedRetries} retries remaining";
        internal const string LOG_TEMPLATE_INFO_PLAYERS_AFFECTED = nameof(ProcessAsyncUpdatesForLinkingAndUnlinkingPlayersToMemberAccounts) + " running. Players affected: {affectedRoutes}";
        internal const string LOG_TEMPLATE_ERROR_SQL_EXCEPTION = nameof(ProcessAsyncUpdatesForLinkingAndUnlinkingPlayersToMemberAccounts) + " threw SqlException: {message}";

        public SqlServerPlayerRepository(
            IDatabaseConnectionFactory databaseConnectionFactory,
            IDapperWrapper dapperWrapper,
            IAuditRepository auditRepository,
            ILogger<SqlServerPlayerRepository> logger,
            IRedirectsRepository redirectsRepository,
            IRouteGenerator routeGenerator,
            IStoolballEntityCopier copier,
            IPlayerNameFormatter playerNameFormatter,
            IBestRouteSelector bestRouteSelector,
            IPlayerCacheInvalidator playerCacheClearer)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _dapperWrapper = dapperWrapper ?? throw new ArgumentNullException(nameof(dapperWrapper));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
            _copier = copier ?? throw new ArgumentNullException(nameof(copier));
            _playerNameFormatter = playerNameFormatter ?? throw new ArgumentNullException(nameof(playerNameFormatter));
            _bestRouteSelector = bestRouteSelector ?? throw new ArgumentNullException(nameof(bestRouteSelector));
            _playerCacheClearer = playerCacheClearer ?? throw new ArgumentNullException(nameof(playerCacheClearer));
        }

        /// <summary>
        /// Finds an existing player identity or creates it if it is not found
        /// </summary>
        /// <returns>The <see cref="PlayerIdentity.PlayerIdentityId"/> of the created or matched player identity</returns>
        public async Task<PlayerIdentity> CreateOrMatchPlayerIdentity(PlayerIdentity playerIdentity, Guid memberKey, string memberName, IDbTransaction transaction)
        {
            if (playerIdentity is null)
            {
                throw new ArgumentNullException(nameof(playerIdentity));
            }

            if (playerIdentity.PlayerIdentityId.HasValue && playerIdentity.Player.PlayerId.HasValue)
            {
                return playerIdentity;
            }

            if (string.IsNullOrWhiteSpace(playerIdentity.PlayerIdentityName))
            {
                throw new ArgumentException($"'{nameof(playerIdentity)}.PlayerIdentityName' cannot be null or whitespace", nameof(playerIdentity));
            }

            if (playerIdentity.Team?.TeamId == null)
            {
                throw new ArgumentException($"'{nameof(playerIdentity)}.Team.TeamId' cannot be null", nameof(playerIdentity));
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            if (transaction is null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            var matchedPlayerIdentity = (await transaction.Connection.QueryAsync<PlayerIdentity, Player, PlayerIdentity>(
                    $"SELECT PlayerIdentityId, PlayerIdentityName, PlayerId FROM {Tables.PlayerIdentity} WHERE ComparableName = @ComparableName AND TeamId = @TeamId",
                    (pi, p) =>
                    {
                        pi.Player = p;
                        return pi;
                    },
                    new
                    {
                        ComparableName = playerIdentity.ComparableName(),
                        playerIdentity.Team.TeamId
                    },
                    transaction,
                    splitOn: "PlayerId")).FirstOrDefault();

            if (matchedPlayerIdentity != null && matchedPlayerIdentity.PlayerIdentityId.HasValue && matchedPlayerIdentity.Player.PlayerId.HasValue)
            {
                matchedPlayerIdentity.Team = playerIdentity.Team;
                return matchedPlayerIdentity;
            }

            var auditablePlayerIdentity = _copier.CreateAuditableCopy(playerIdentity);

            auditablePlayerIdentity.PlayerIdentityId = Guid.NewGuid();
            auditablePlayerIdentity.PlayerIdentityName = _playerNameFormatter.CapitaliseName(auditablePlayerIdentity.PlayerIdentityName);

            var player = new Player { PlayerId = Guid.NewGuid() };
            player.PlayerIdentities.Add(auditablePlayerIdentity);

            player.PlayerRoute = await _routeGenerator.GenerateUniqueRoute($"/players", auditablePlayerIdentity.PlayerIdentityName, NoiseWords.PlayerRoute,
               async route => await transaction.Connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Player} WHERE PlayerRoute = @PlayerRoute", new { PlayerRoute = route }, transaction)
            );

            await transaction.Connection.ExecuteAsync(
                  $@"INSERT INTO {Tables.Player} 
                                               (PlayerId, PlayerRoute) 
                                               VALUES 
                                               (@PlayerId, @PlayerRoute)",
                  new
                  {
                      player.PlayerId,
                      player.PlayerRoute
                  }, transaction);

            await transaction.Connection.ExecuteAsync($@"INSERT INTO {Tables.PlayerIdentity} 
                                (PlayerIdentityId, PlayerId, PlayerIdentityName, ComparableName, TeamId) 
                                VALUES (@PlayerIdentityId, @PlayerId, @PlayerIdentityName, @ComparableName, @TeamId)",
                   new
                   {
                       auditablePlayerIdentity.PlayerIdentityId,
                       player.PlayerId,
                       auditablePlayerIdentity.PlayerIdentityName,
                       ComparableName = auditablePlayerIdentity.ComparableName(),
                       auditablePlayerIdentity.Team.TeamId
                   }, transaction);

            var serialisedPlayer = JsonConvert.SerializeObject(player);
            await _auditRepository.CreateAudit(new AuditRecord
            {
                Action = AuditAction.Create,
                MemberKey = memberKey,
                ActorName = memberName,
                EntityUri = player.EntityUri,
                State = serialisedPlayer,
                RedactedState = serialisedPlayer,
                AuditDate = DateTime.UtcNow
            }, transaction);

            _logger.Info(LoggingTemplates.Created, player, memberName, memberKey, GetType(), nameof(CreateOrMatchPlayerIdentity));

            player.PlayerIdentities.Clear();
            auditablePlayerIdentity.Player = player;
            return auditablePlayerIdentity;
        }

        /// <inheritdoc />
        public async Task<Player> LinkPlayerToMemberAccount(Player player, Guid memberKey, string memberName)
        {
            if (player == null || !player.PlayerId.HasValue || string.IsNullOrEmpty(player.PlayerRoute))
            {
                throw new ArgumentException(nameof(player), $"{nameof(player)} cannot be null and must have a PlayerId and PlayerRoute");
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var auditablePlayer = _copier.CreateAuditableCopy(player);

                    // Is the player already linked, either to this member or someone else?
                    var existingMemberForPlayer = await connection.QuerySingleOrDefaultAsync<Guid?>($"SELECT MemberKey FROM {Tables.Player} WHERE PlayerId = @PlayerId", auditablePlayer, transaction);
                    if (existingMemberForPlayer.HasValue)
                    {
                        transaction.Rollback();
                        throw new InvalidOperationException($"Unable to link player {auditablePlayer.PlayerId} to member {memberKey} because it is already linked to member {existingMemberForPlayer}");
                    }

                    // Is this member already linked to a player?
                    var existingPlayerForMember = await connection.QuerySingleOrDefaultAsync<Player>($"SELECT TOP 1 PlayerId, PlayerRoute, MemberKey FROM {Tables.Player} WHERE MemberKey = @memberKey", new { memberKey }, transaction);
                    if (existingPlayerForMember == null)
                    {
                        // Link member to player record
                        await connection.ExecuteAsync($"UPDATE {Tables.Player} SET MemberKey = @memberKey WHERE PlayerId = @PlayerId", new { memberKey, auditablePlayer.PlayerId }, transaction);

                        auditablePlayer.MemberKey = memberKey;

                        var serialisedPlayer = JsonConvert.SerializeObject(auditablePlayer);
                        await _auditRepository.CreateAudit(new AuditRecord
                        {
                            Action = AuditAction.Update,
                            MemberKey = memberKey,
                            ActorName = memberName,
                            EntityUri = player.EntityUri,
                            State = serialisedPlayer,
                            RedactedState = serialisedPlayer,
                            AuditDate = DateTime.UtcNow
                        }, transaction);

                        _logger.Info(LoggingTemplates.Updated, serialisedPlayer, memberName, memberKey, GetType(), nameof(LinkPlayerToMemberAccount));
                    }
                    else
                    {
                        // Select the best route from the two players, and redirect
                        var bestRoute = _bestRouteSelector.SelectBestRoute(existingPlayerForMember.PlayerRoute, auditablePlayer.PlayerRoute);
                        var obsoleteRoute = bestRoute == existingPlayerForMember.PlayerRoute ? auditablePlayer.PlayerRoute : existingPlayerForMember.PlayerRoute;
                        await _redirectsRepository.InsertRedirect(obsoleteRoute, bestRoute, null, transaction);
                        await _redirectsRepository.InsertRedirect(obsoleteRoute, bestRoute, "/batting", transaction);
                        await _redirectsRepository.InsertRedirect(obsoleteRoute, bestRoute, "/bowling", transaction);
                        await _redirectsRepository.InsertRedirect(obsoleteRoute, bestRoute, "/fielding", transaction);
                        await _redirectsRepository.InsertRedirect(obsoleteRoute, bestRoute, "/individual-scores", transaction);
                        await _redirectsRepository.InsertRedirect(obsoleteRoute, bestRoute, "/bowling-figures", transaction);
                        await _redirectsRepository.InsertRedirect(obsoleteRoute, bestRoute, "/catches", transaction);
                        await _redirectsRepository.InsertRedirect(obsoleteRoute, bestRoute, "/run-outs", transaction);

                        // Move the player identities from this player id to the member's player id
                        var replaceWithExistingPlayer = new { ExistingPlayerId = existingPlayerForMember.PlayerId, PlayerRoute = bestRoute, PlayerId = auditablePlayer.PlayerId };
                        if (bestRoute != existingPlayerForMember.PlayerRoute)
                        {
                            await connection.ExecuteAsync($"UPDATE {Tables.Player} SET PlayerRoute = @PlayerRoute WHERE PlayerId = @PlayerId", new { PlayerRoute = bestRoute, PlayerId = existingPlayerForMember.PlayerId }, transaction);
                        }
                        await connection.ExecuteAsync($"UPDATE {Tables.PlayerIdentity} SET PlayerId = @ExistingPlayerId WHERE PlayerId = @PlayerId", replaceWithExistingPlayer, transaction);

                        // We also need to update statistics, and delete the now-unused player that the identity has been moved away from. 
                        // However this is done asynchronously by ProcessAsyncUpdatesForLinkingAndUnlinkingPlayersToMemberAccounts, so we just need to mark the player as safe to delete.
                        await connection.ExecuteAsync($"UPDATE {Tables.Player} SET ForAsyncDelete = 1 WHERE PlayerId = @PlayerId", auditablePlayer, transaction);

                        var serialisedDeletedPlayer = JsonConvert.SerializeObject(auditablePlayer);
                        await _auditRepository.CreateAudit(new AuditRecord
                        {
                            Action = AuditAction.Delete,
                            MemberKey = memberKey,
                            ActorName = memberName,
                            EntityUri = auditablePlayer.EntityUri,
                            State = serialisedDeletedPlayer,
                            RedactedState = serialisedDeletedPlayer,
                            AuditDate = DateTime.UtcNow
                        }, transaction);

                        _logger.Info(LoggingTemplates.Deleted, serialisedDeletedPlayer, memberName, memberKey, GetType(), nameof(LinkPlayerToMemberAccount));

                        // Update the player to return with new details assigned to it
                        auditablePlayer.PlayerId = existingPlayerForMember.PlayerId;
                        auditablePlayer.PlayerRoute = bestRoute;
                        auditablePlayer.MemberKey = existingPlayerForMember.MemberKey;

                        var serialisedUpdatedPlayer = JsonConvert.SerializeObject(auditablePlayer);
                        await _auditRepository.CreateAudit(new AuditRecord
                        {
                            Action = AuditAction.Update,
                            MemberKey = memberKey,
                            ActorName = memberName,
                            EntityUri = auditablePlayer.EntityUri,
                            State = serialisedUpdatedPlayer,
                            RedactedState = serialisedUpdatedPlayer,
                            AuditDate = DateTime.UtcNow
                        }, transaction);

                        _logger.Info(LoggingTemplates.Updated, serialisedUpdatedPlayer, memberName, memberKey, GetType(), nameof(LinkPlayerToMemberAccount));
                    }

                    transaction.Commit();

                    return auditablePlayer;
                }
            }
        }

        /// <inheritdoc />
        public async Task UnlinkPlayerIdentityFromMemberAccount(PlayerIdentity playerIdentity, Guid memberKey, string memberName)
        {
            if (playerIdentity == null || !playerIdentity.PlayerIdentityId.HasValue || string.IsNullOrEmpty(playerIdentity.PlayerIdentityName))
            {
                throw new ArgumentException(nameof(playerIdentity), $"{nameof(playerIdentity)} cannot be null and must have a PlayerIdentityId and PlayerIdentityName");
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var result = await connection.QuerySingleAsync<(int totalIdentitiesLinkedToMember, Guid playerId)>($"SELECT COUNT(*), PlayerId FROM {Tables.PlayerIdentity} WHERE PlayerId = (SELECT PlayerId FROM {Tables.PlayerIdentity} WHERE PlayerIdentityId = @PlayerIdentityId) GROUP BY PlayerId", playerIdentity, transaction).ConfigureAwait(false);

                    if (result.totalIdentitiesLinkedToMember == 1)
                    {
                        await connection.ExecuteAsync($"UPDATE {Tables.Player} SET MemberKey = NULL WHERE MemberKey = @memberKey", new { memberKey }, transaction).ConfigureAwait(false);

                        var player = new Player { PlayerId = result.playerId };
                        var serialisedPlayer = JsonConvert.SerializeObject(player);
                        await _auditRepository.CreateAudit(new AuditRecord
                        {
                            Action = AuditAction.Update,
                            MemberKey = memberKey,
                            ActorName = memberName,
                            EntityUri = player.EntityUri,
                            State = serialisedPlayer,
                            RedactedState = serialisedPlayer,
                            AuditDate = DateTime.UtcNow
                        }, transaction);

                        _logger.Info(LoggingTemplates.Updated, serialisedPlayer, memberName, memberKey, GetType(), nameof(UnlinkPlayerIdentityFromMemberAccount));
                    }
                    else
                    {
                        // Create new player 
                        var player = new Player { PlayerId = Guid.NewGuid() };
                        player.PlayerIdentities.Add(playerIdentity);

                        player.PlayerRoute = await _routeGenerator.GenerateUniqueRoute($"/players", playerIdentity.PlayerIdentityName, NoiseWords.PlayerRoute,
                           async route => await transaction.Connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Player} WHERE PlayerRoute = @PlayerRoute", new { PlayerRoute = route }, transaction)
                        );

                        await transaction.Connection.ExecuteAsync(
                              $@"INSERT INTO {Tables.Player} 
                                               (PlayerId, PlayerRoute) 
                                               VALUES 
                                               (@PlayerId, @PlayerRoute)",
                              new
                              {
                                  player.PlayerId,
                                  player.PlayerRoute
                              }, transaction);

                        // Update identity to point to new player
                        await connection.ExecuteAsync($"UPDATE {Tables.PlayerIdentity} SET PlayerId = @PlayerId WHERE PlayerIdentityId = @PlayerIdentityId", new { player.PlayerId, playerIdentity.PlayerIdentityId }, transaction);

                        // We also need to update statistics to point to new player and new player route.
                        // However this is done asynchronously by ProcessAsyncUpdatesForLinkingAndUnlinkingPlayersToMemberAccounts, so there is nothing to do here.

                        var serialisedPlayer = JsonConvert.SerializeObject(_copier.CreateAuditableCopy(player));
                        await _auditRepository.CreateAudit(new AuditRecord
                        {
                            Action = AuditAction.Create,
                            MemberKey = memberKey,
                            ActorName = memberName,
                            EntityUri = player.EntityUri,
                            State = serialisedPlayer,
                            RedactedState = serialisedPlayer,
                            AuditDate = DateTime.UtcNow
                        }, transaction);

                        _logger.Info(LoggingTemplates.Created, serialisedPlayer, memberName, memberKey, GetType(), nameof(UnlinkPlayerIdentityFromMemberAccount));
                    }

                    transaction.Commit();
                }
            }
        }

        /// <inheritdoc />
        public async Task ProcessAsyncUpdatesForLinkingAndUnlinkingPlayersToMemberAccounts()
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                IEnumerable<string> affectedRoutes = Array.Empty<string>();
                int allowedRetries = 3;
                bool retry = false;
                do
                {
                    try
                    {
                        retry = false;
                        affectedRoutes = await _dapperWrapper.QueryAsync<string>("usp_Link_Player_To_Member_Async_Update", commandType: CommandType.StoredProcedure, connection: connection).ConfigureAwait(false);
                        foreach (var route in affectedRoutes)
                        {
                            _playerCacheClearer.InvalidateCacheForPlayer(new Player { PlayerRoute = route });
                        }
                        _logger.Info(LOG_TEMPLATE_INFO_PLAYERS_AFFECTED, affectedRoutes.Any() ? string.Join<string>(", ", affectedRoutes) : "None");
                    }
                    catch (SqlException ex)
                    {
                        // This updates the heavily-indexed PlayerInMatchStatistics table which is prone to SQL timeouts, so catch them and allow for a limited number of retries.
                        // If it fails every retry, the work will be picked up the next time this method is invoked.
                        if (ex.Message.StartsWith("Timeout expired."))
                        {
                            _logger.Warn(LOG_TEMPLATE_WARN_SQL_TIMEOUT, allowedRetries);
                            if (allowedRetries > 0)
                            {
                                retry = true;
                            }
                            allowedRetries--;
                        }
                        else
                        {
                            _logger.Error(LOG_TEMPLATE_ERROR_SQL_EXCEPTION, ex.Message);
                            break;
                        };
                    }

                }
                while (affectedRoutes.Any() || retry);
            }
        }

    }
}
