using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Humanizer;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Stoolball.Data.Abstractions;
using Stoolball.Data.Abstractions.Models;
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
        internal const string PROCESS_ASYNC_STORED_PROCEDURE = "usp_Player_Async_Update";
        internal const string LOG_TEMPLATE_WARN_SQL_TIMEOUT = nameof(ProcessAsyncUpdatesForPlayers) + " running. Caught SQL Timeout. {allowedRetries} retries remaining";
        internal const string LOG_TEMPLATE_INFO_PLAYERS_AFFECTED = nameof(ProcessAsyncUpdatesForPlayers) + " running. Players affected: {affectedRoutes}";
        internal const string LOG_TEMPLATE_ERROR_SQL_EXCEPTION = nameof(ProcessAsyncUpdatesForPlayers) + " threw SqlException: {message}";

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
            var matchedPlayerIdentity = await MatchPlayerIdentity(playerIdentity, true, memberName, transaction);
            if (matchedPlayerIdentity != null) { return matchedPlayerIdentity; }

            var auditablePlayerIdentity = _copier.CreateAuditableCopy(playerIdentity);

            auditablePlayerIdentity.PlayerIdentityId = Guid.NewGuid();
            auditablePlayerIdentity.PlayerIdentityName = _playerNameFormatter.CapitaliseName(auditablePlayerIdentity.PlayerIdentityName);
            auditablePlayerIdentity.RouteSegment = (await _routeGenerator.GenerateUniqueRoute(string.Empty, auditablePlayerIdentity.PlayerIdentityName.Kebaberize(), NoiseWords.PlayerRoute,
                async route => await transaction.Connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Views.PlayerIdentity} WHERE RouteSegment = @RouteSegment AND TeamId = @TeamId", new { RouteSegment = route, auditablePlayerIdentity.Team.TeamId }, transaction)
            ).ConfigureAwait(false))?.TrimStart('/');

            var player = new Player { PlayerId = Guid.NewGuid() };
            player.PlayerIdentities.Add(auditablePlayerIdentity);

            player.PlayerRoute = await _routeGenerator.GenerateUniqueRoute($"/players", auditablePlayerIdentity.PlayerIdentityName, NoiseWords.PlayerRoute,
               async route => await transaction.Connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Player} WHERE PlayerRoute = @PlayerRoute AND Deleted = 0", new { PlayerRoute = route }, transaction)
            ).ConfigureAwait(false);

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
                                (PlayerIdentityId, PlayerId, PlayerIdentityName, ComparableName, RouteSegment, TeamId) 
                                VALUES (@PlayerIdentityId, @PlayerId, @PlayerIdentityName, @ComparableName, @RouteSegment, @TeamId)",
                   new
                   {
                       auditablePlayerIdentity.PlayerIdentityId,
                       player.PlayerId,
                       auditablePlayerIdentity.PlayerIdentityName,
                       ComparableName = auditablePlayerIdentity.ComparableName(),
                       auditablePlayerIdentity.RouteSegment,
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

        private async Task<PlayerIdentity?> MatchPlayerIdentity(PlayerIdentity playerIdentity, bool allowMatchByPlayerIdentityIdOrPlayerId, string memberName, IDbTransaction transaction)
        {
            if (playerIdentity is null)
            {
                throw new ArgumentNullException(nameof(playerIdentity));
            }

            if (allowMatchByPlayerIdentityIdOrPlayerId && playerIdentity.PlayerIdentityId.HasValue && playerIdentity.Player != null && playerIdentity.Player.PlayerId.HasValue)
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

            var matchedPlayerIdentity = (await _dapperWrapper.QueryAsync<PlayerIdentity, Player, PlayerIdentity>(
                    $"SELECT PlayerIdentityId, PlayerIdentityName, PlayerId FROM {Views.PlayerIdentity} WHERE ComparableName = @ComparableName AND TeamId = @TeamId",
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

            if (matchedPlayerIdentity != null && matchedPlayerIdentity.PlayerIdentityId.HasValue && matchedPlayerIdentity.Player != null && matchedPlayerIdentity.Player.PlayerId.HasValue)
            {
                matchedPlayerIdentity.Team = playerIdentity.Team;
                return matchedPlayerIdentity;
            }
            return null;
        }

        /// <inheritdoc />
        public async Task<Player> LinkPlayerToMemberAccount(Player player, Guid memberKey, string memberName)
        {
            if (player == null || !player.PlayerId.HasValue || string.IsNullOrEmpty(player.PlayerRoute))
            {
                throw new ArgumentException($"{nameof(player)} cannot be null and must have a PlayerId and PlayerRoute", nameof(player));
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
                    var auditablePlayer = _copier.CreateAuditableCopy(player)!;

                    // Is the player already linked, either to this member or someone else?
                    var existingMemberForPlayer = await connection.QuerySingleOrDefaultAsync<Guid?>($"SELECT MemberKey FROM {Tables.Player} WHERE PlayerId = @PlayerId AND Deleted = 0", auditablePlayer, transaction);
                    if (existingMemberForPlayer.HasValue)
                    {
                        transaction.Rollback();
                        throw new InvalidOperationException($"Unable to link player {auditablePlayer.PlayerId} to member {memberKey} because it is already linked to member {existingMemberForPlayer}");
                    }

                    // Is this member already linked to a player?
                    var existingPlayerForMember = await connection.QuerySingleOrDefaultAsync<Player>($"SELECT TOP 1 PlayerId, PlayerRoute, MemberKey FROM {Tables.Player} WHERE MemberKey = @memberKey AND Deleted = 0", new { memberKey }, transaction);
                    if (existingPlayerForMember == null)
                    {
                        // Link member to player record
                        _ = await connection.ExecuteAsync($"UPDATE {Tables.Player} SET MemberKey = @memberKey WHERE PlayerId = @PlayerId", new { memberKey, auditablePlayer.PlayerId }, transaction);
                        _ = await connection.ExecuteAsync($"UPDATE {Tables.PlayerIdentity} SET LinkedBy = @LinkedBy WHERE PlayerId = @PlayerId", new { LinkedBy = PlayerIdentityLinkedBy.Member.ToString(), auditablePlayer.PlayerId }, transaction);

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
                        var bestRoute = await FindBestRouteAndRedirect(auditablePlayer.PlayerRoute!, existingPlayerForMember.PlayerRoute!, transaction);

                        // Move the player identities from this player id to the member's player id
                        var replaceWithExistingPlayer = new { ExistingPlayerId = existingPlayerForMember.PlayerId, PlayerRoute = bestRoute, auditablePlayer.PlayerId, LinkedBy = PlayerIdentityLinkedBy.Member.ToString() };
                        if (bestRoute != existingPlayerForMember.PlayerRoute)
                        {
                            await connection.ExecuteAsync($"UPDATE {Tables.Player} SET PlayerRoute = @PlayerRoute WHERE PlayerId = @PlayerId", new { PlayerRoute = bestRoute, PlayerId = existingPlayerForMember.PlayerId }, transaction);
                        }
                        await connection.ExecuteAsync($"UPDATE {Tables.PlayerIdentity} SET LinkedBy = @LinkedBy, PlayerId = @ExistingPlayerId WHERE PlayerId = @PlayerId", replaceWithExistingPlayer, transaction);

                        // We also need to update statistics, and delete the now-unused player that the identity has been moved away from. 
                        // However this is done asynchronously by ProcessAsyncUpdatesForPlayers, so we just need to mark the player as safe to delete.
                        await connection.ExecuteAsync($"UPDATE {Tables.Player} SET Deleted = 1 WHERE PlayerId = @PlayerId", auditablePlayer, transaction);

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

        private async Task<string> FindBestRouteAndRedirect(string route1, string route2, IDbTransaction transaction)
        {
            var bestRoute = _bestRouteSelector.SelectBestRoute(route2, route1);
            var obsoleteRoute = bestRoute == route2 ? route1 : route2;
            await RedirectPlayerRoute(obsoleteRoute!, bestRoute, transaction);
            return bestRoute;
        }

        /// <inheritdoc />
        public async Task<MovedPlayerIdentity> LinkPlayerIdentity(Guid targetPlayer, Guid identityToLinkToTarget, PlayerIdentityLinkedBy linkedBy, Guid memberKey, string memberName)
        {
            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var targetPlayerBefore = await connection.QuerySingleAsync<(string PlayerRoute, Guid? MemberKey)>($"SELECT PlayerRoute, MemberKey FROM {Tables.Player} WHERE PlayerId = @PlayerId", new Player { PlayerId = targetPlayer }, transaction);
                    var identityToLinkBefore = await connection.QuerySingleAsync<(Guid PlayerId, string PlayerRoute, Guid? MemberKey, int Identities, string PlayerIdentityName, Guid TeamId)>(
                        @$"SELECT p.PlayerId, p.PlayerRoute, p.MemberKey, (SELECT COUNT(*) FROM {Tables.PlayerIdentity} WHERE PlayerId = p.PlayerId) AS Identities, pi.PlayerIdentityName, pi.TeamId
                           FROM {Tables.PlayerIdentity} pi INNER JOIN {Tables.Player} p ON pi.PlayerId = p.PlayerId
                           WHERE pi.PlayerIdentityId = @PlayerIdentityId", new { PlayerIdentityId = identityToLinkToTarget }, transaction);

                    // Are the players already linked to each other? If so, abort.
                    if (targetPlayer == identityToLinkBefore.PlayerId)
                    {
                        throw new InvalidOperationException("The player identity is already linked to the target player");
                    }

                    // Are both players linked to a member? If so, abort.
                    if (targetPlayerBefore.MemberKey.HasValue && identityToLinkBefore.MemberKey.HasValue)
                    {
                        throw new InvalidOperationException("The target player and the identity to link are are linked to different members and cannot be linked to each other");
                    }

                    // Is either player linked to a member? If so, abort, unless it's the current member.
                    // (When this process is extended to request permissions, this rule no longer applies.)
                    if (targetPlayerBefore.MemberKey.HasValue && targetPlayerBefore.MemberKey != memberKey)
                    {
                        throw new InvalidOperationException("The target player is linked to a member and only that member can choose which identities to link");
                    }

                    if (identityToLinkBefore.MemberKey.HasValue && identityToLinkBefore.MemberKey != memberKey)
                    {
                        throw new InvalidOperationException("The identity to link is linked to a member and only that member can choose which identities to link");
                    }

                    // Does the identity to link already have sibling identities. If so, abort.
                    // (When the UI catches up, this rule no longer applies.)
                    if (identityToLinkBefore.Identities > 1)
                    {
                        throw new InvalidOperationException("The identity to link is linked to other identities and must be separated before it can be moved");
                    }

                    // Does the target player have an identity on the same team as the identity to link? If not, abort.
                    var targetPlayerExistingIdentities = await connection.QueryAsync<(Guid PlayerIdentityId, string PlayerIdentityName, Guid TeamId)>(
                        $"SELECT PlayerIdentityId, PlayerIdentityName, TeamId FROM {Tables.PlayerIdentity} WHERE PlayerId = @PlayerId",
                        new { PlayerId = targetPlayer }, transaction);
                    var targetPlayerHasIdentityOnSameTeam = targetPlayerExistingIdentities.Any(id => id.TeamId == identityToLinkBefore.TeamId);

                    if (!targetPlayerHasIdentityOnSameTeam)
                    {
                        throw new InvalidOperationException("The identity to link must be on the same team as the target player");
                    }

                    // Select the best route from the two players, and redirect.
                    var bestRoute = await FindBestRouteAndRedirect(targetPlayerBefore.PlayerRoute, identityToLinkBefore.PlayerRoute, transaction);

                    // Move the player identities from the identity to link's current player id to the target player's id.
                    if (bestRoute != targetPlayerBefore.PlayerRoute)
                    {
                        await connection.ExecuteAsync($"UPDATE {Tables.Player} SET PlayerRoute = @PlayerRoute WHERE PlayerId = @PlayerId", new { PlayerRoute = bestRoute, PlayerId = targetPlayer }, transaction);
                    }
                    var movePlayerIdentity = new { LinkedBy = linkedBy.ToString(), PlayerId = targetPlayer, PlayerIdentityId = identityToLinkToTarget };
                    await connection.ExecuteAsync($"UPDATE {Tables.PlayerIdentity} SET LinkedBy = @LinkedBy, PlayerId = @PlayerId WHERE PlayerIdentityId = @PlayerIdentityId", movePlayerIdentity, transaction);

                    // We also need to update statistics, and delete the now-unused player that the identity has been moved away from. 
                    // However this is done asynchronously by ProcessAsyncUpdatesForPlayers, so we just need to mark the player as safe to delete.
                    await connection.ExecuteAsync($"UPDATE {Tables.Player} SET Deleted = 1 WHERE PlayerId = @PlayerId", new { identityToLinkBefore.PlayerId }, transaction);

                    var deletedPlayer = new Player { PlayerId = identityToLinkBefore.PlayerId, PlayerRoute = identityToLinkBefore.PlayerRoute };
                    deletedPlayer.PlayerIdentities.Add(new PlayerIdentity { PlayerIdentityId = identityToLinkToTarget });
                    var serialisedDeletedPlayer = JsonConvert.SerializeObject(deletedPlayer);
                    await _auditRepository.CreateAudit(new AuditRecord
                    {
                        Action = AuditAction.Delete,
                        MemberKey = memberKey,
                        ActorName = memberName,
                        EntityUri = deletedPlayer.EntityUri,
                        State = serialisedDeletedPlayer,
                        RedactedState = serialisedDeletedPlayer,
                        AuditDate = DateTime.UtcNow
                    }, transaction);

                    _logger.Info(LoggingTemplates.Deleted, serialisedDeletedPlayer, memberName, memberKey, GetType(), nameof(LinkPlayerIdentity));

                    // Update the player to return with new details assigned to it.
                    var reassignedPlayerIdentity = new PlayerIdentity { PlayerIdentityId = identityToLinkToTarget, PlayerIdentityName = identityToLinkBefore.PlayerIdentityName };
                    var updatedTargetPlayer = new Player { PlayerId = targetPlayer, PlayerRoute = bestRoute, MemberKey = targetPlayerBefore.MemberKey };
                    updatedTargetPlayer.PlayerIdentities.AddRange(targetPlayerExistingIdentities.Select(id => new PlayerIdentity { PlayerIdentityId = id.PlayerIdentityId, PlayerIdentityName = id.PlayerIdentityName, Team = new Teams.Team { TeamId = id.TeamId } }));
                    updatedTargetPlayer.PlayerIdentities.Add(reassignedPlayerIdentity);

                    var serialisedUpdatedPlayer = JsonConvert.SerializeObject(updatedTargetPlayer);
                    await _auditRepository.CreateAudit(new AuditRecord
                    {
                        Action = AuditAction.Update,
                        MemberKey = memberKey,
                        ActorName = memberName,
                        EntityUri = updatedTargetPlayer.EntityUri,
                        State = serialisedUpdatedPlayer,
                        RedactedState = serialisedUpdatedPlayer,
                        AuditDate = DateTime.UtcNow
                    }, transaction);

                    _logger.Info(LoggingTemplates.Updated, serialisedUpdatedPlayer, memberName, memberKey, GetType(), nameof(LinkPlayerIdentity));

                    transaction.Commit();

                    return new MovedPlayerIdentity
                    {
                        PlayerIdentityId = identityToLinkToTarget,
                        PlayerIdForSourcePlayer = identityToLinkBefore.PlayerId,
                        MemberKeyForSourcePlayer = identityToLinkBefore.MemberKey,
                        PreviousRouteForSourcePlayer = identityToLinkBefore.PlayerRoute,
                        PlayerIdForTargetPlayer = targetPlayer,
                        MemberKeyForTargetPlayer = targetPlayerBefore.MemberKey,
                        PreviousRouteForTargetPlayer = targetPlayerBefore.PlayerRoute,
                        NewRouteForTargetPlayer = updatedTargetPlayer.PlayerRoute
                    };
                }
            }
        }

        private async Task RedirectPlayerRoute(string routeBefore, string routeAfter, IDbTransaction transaction)
        {
            await _redirectsRepository.InsertRedirect(routeBefore, routeAfter, null, transaction);
            await _redirectsRepository.InsertRedirect(routeBefore, routeAfter, "/batting", transaction);
            await _redirectsRepository.InsertRedirect(routeBefore, routeAfter, "/bowling", transaction);
            await _redirectsRepository.InsertRedirect(routeBefore, routeAfter, "/fielding", transaction);
            await _redirectsRepository.InsertRedirect(routeBefore, routeAfter, "/individual-scores", transaction);
            await _redirectsRepository.InsertRedirect(routeBefore, routeAfter, "/bowling-figures", transaction);
            await _redirectsRepository.InsertRedirect(routeBefore, routeAfter, "/catches", transaction);
            await _redirectsRepository.InsertRedirect(routeBefore, routeAfter, "/run-outs", transaction);
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
                    var (totalIdentitiesLinkedToMember, playerId) = await connection.QuerySingleAsync<(int totalIdentitiesLinkedToMember, Guid playerId)>(
                        $@"SELECT COUNT(*), PlayerId FROM {Views.PlayerIdentity} 
                           WHERE PlayerId = (SELECT PlayerId FROM {Views.PlayerIdentity} WHERE PlayerIdentityId = @PlayerIdentityId) 
                           GROUP BY PlayerId", playerIdentity, transaction).ConfigureAwait(false);

                    if (totalIdentitiesLinkedToMember == 1)
                    {
                        _ = await connection.ExecuteAsync($"UPDATE {Tables.Player} SET MemberKey = NULL WHERE MemberKey = @memberKey", new { memberKey }, transaction).ConfigureAwait(false);
                        _ = await connection.ExecuteAsync($"UPDATE {Tables.PlayerIdentity} SET LinkedBy = @LinkedBy WHERE PlayerIdentityId = @PlayerIdentityId",
                            new
                            {
                                LinkedBy = PlayerIdentityLinkedBy.DefaultIdentity.ToString(),
                                playerIdentity.PlayerIdentityId
                            }, transaction).ConfigureAwait(false);

                        var player = new Player { PlayerId = playerId };
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
                        await MoveIdentityToNewPlayer(playerIdentity.PlayerIdentityId.Value, playerIdentity.PlayerIdentityName, memberKey, memberName, transaction, nameof(UnlinkPlayerIdentityFromMemberAccount)).ConfigureAwait(false);
                    }

                    transaction.Commit();
                }
            }
        }

        private async Task MoveIdentityToNewPlayer(Guid playerIdentityId, string playerIdentityName, Guid memberKey, string memberName, IDbTransaction transaction, string callingMethodForLog)
        {
            // Create new player 
            var player = new Player { PlayerId = Guid.NewGuid() };
            player.PlayerIdentities.Add(new PlayerIdentity { PlayerIdentityId = playerIdentityId, PlayerIdentityName = playerIdentityName });

            player.PlayerRoute = await _routeGenerator.GenerateUniqueRoute($"/players", playerIdentityName, NoiseWords.PlayerRoute,
               async route => await transaction.Connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Player} WHERE PlayerRoute = @PlayerRoute AND Deleted = 0", new { PlayerRoute = route }, transaction)
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
            await transaction.Connection.ExecuteAsync($"UPDATE {Tables.PlayerIdentity} SET PlayerId = @PlayerId, LinkedBy = @LinkedBy WHERE PlayerIdentityId = @PlayerIdentityId",
                new
                {
                    player.PlayerId,
                    playerIdentityId,
                    LinkedBy = PlayerIdentityLinkedBy.DefaultIdentity.ToString()
                }, transaction);

            // We also need to update statistics to point to new player and new player route.
            // However this is done asynchronously by ProcessAsyncUpdatesForPlayers, so there is nothing to do here.

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

            _logger.Info(LoggingTemplates.Created, serialisedPlayer, memberName, memberKey, GetType(), callingMethodForLog);
        }

        /// <inheritdoc />
        public async Task UnlinkPlayerIdentity(Guid identityToUnlink, Guid memberKey, string memberName)
        {
            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var (totalIdentitiesLinkedToPlayer, playerId, playerIdentityName) = await connection.QuerySingleAsync<(int totalIdentitiesLinkedToMember, Guid playerId, string playerIdentityName)>(
                        $@"SELECT COUNT(*), PlayerId, (SELECT PlayerIdentityName FROM {Views.PlayerIdentity} WHERE PlayerIdentityId = @PlayerIdentityId) AS PlayerIdentityName
                           FROM {Views.PlayerIdentity} 
                           WHERE PlayerId = (SELECT PlayerId FROM {Views.PlayerIdentity} WHERE PlayerIdentityId = @PlayerIdentityId) 
                           GROUP BY PlayerId", new { PlayerIdentityId = identityToUnlink }, transaction).ConfigureAwait(false);

                    if (totalIdentitiesLinkedToPlayer == 1)
                    {
                        throw new InvalidOperationException();
                    }
                    else
                    {
                        await MoveIdentityToNewPlayer(identityToUnlink, playerIdentityName, memberKey, memberName, transaction, nameof(UnlinkPlayerIdentity)).ConfigureAwait(false);
                    }

                    transaction.Commit();
                }
            }
        }

        /// <inheritdoc />
        public async Task ProcessAsyncUpdatesForPlayers()
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
                        affectedRoutes = await _dapperWrapper.QueryAsync<string>(PROCESS_ASYNC_STORED_PROCEDURE, commandType: CommandType.StoredProcedure, connection: connection).ConfigureAwait(false);
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

        /// <inheritdoc />
        public async Task<RepositoryResult<PlayerIdentityUpdateResult, PlayerIdentity>> UpdatePlayerIdentity(PlayerIdentity playerIdentity, Guid memberKey, string memberName)
        {
            if (playerIdentity is null)
            {
                throw new ArgumentNullException(nameof(playerIdentity));
            }

            if (!playerIdentity.PlayerIdentityId.HasValue)
            {
                throw new ArgumentException($"{nameof(playerIdentity.PlayerIdentityId)} must have a value");
            }

            if (string.IsNullOrEmpty(playerIdentity.PlayerIdentityName))
            {
                throw new ArgumentException($"{nameof(playerIdentity.PlayerIdentityName)} cannot be null or empty");
            }

            if (playerIdentity.Team?.TeamId == null)
            {
                throw new ArgumentException("TeamId must have a value");
            }

            if (playerIdentity.Player?.PlayerId == null)
            {
                throw new ArgumentException("PlayerId must have a value");
            }

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var matchedPlayerIdentity = await MatchPlayerIdentity(playerIdentity, false, memberName, transaction).ConfigureAwait(false);
                    if (matchedPlayerIdentity != null && matchedPlayerIdentity.PlayerIdentityId != playerIdentity.PlayerIdentityId)
                    {
                        return new RepositoryResult<PlayerIdentityUpdateResult, PlayerIdentity>
                        {
                            Status = PlayerIdentityUpdateResult.NotUnique, Result = playerIdentity
                        };
                    }
                    else
                    {
                        var auditablePlayer = _copier.CreateAuditableCopy(playerIdentity.Player)!;
                        var auditablePlayerIdentity = _copier.CreateAuditableCopy(playerIdentity)!;
                        auditablePlayerIdentity.PlayerIdentityName = _playerNameFormatter.CapitaliseName(auditablePlayerIdentity.PlayerIdentityName!);

                        auditablePlayerIdentity.RouteSegment = (await _routeGenerator.GenerateUniqueRoute(string.Empty, auditablePlayerIdentity.PlayerIdentityName.Kebaberize(), NoiseWords.PlayerRoute,
                            async route => await transaction.Connection.ExecuteScalarAsync<int>(
                            $"SELECT COUNT(*) FROM {Views.PlayerIdentity} WHERE RouteSegment = @RouteSegment AND TeamId = @TeamId",
                            new { RouteSegment = route, auditablePlayerIdentity.Team!.TeamId }, transaction)
                        ).ConfigureAwait(false))?.TrimStart('/');

                        _ = await _dapperWrapper.ExecuteAsync($@"UPDATE {Tables.PlayerIdentity} SET 
                                    PlayerIdentityName = @PlayerIdentityName,
                                    ComparableName = @ComparableName,
                                    RouteSegment = @RouteSegment
                                    WHERE PlayerIdentityId = @PlayerIdentityId",
                                    new
                                    {
                                        auditablePlayerIdentity.PlayerIdentityId,
                                        auditablePlayerIdentity.PlayerIdentityName,
                                        ComparableName = auditablePlayerIdentity.ComparableName(),
                                        auditablePlayerIdentity.RouteSegment
                                    }, transaction).ConfigureAwait(false);

                        await UpdatePlayerRoute(auditablePlayer.PlayerId!.Value, transaction);

                        // We also need to update statistics to point to new player identity name and new player route.
                        // However this is done asynchronously by ProcessAsyncUpdatesForPlayers, so there is nothing to do here.
                        if (!auditablePlayer.PlayerIdentities.Any(x => x.PlayerIdentityId == auditablePlayerIdentity.PlayerIdentityId))
                        {
                            auditablePlayer.PlayerIdentities.Add(auditablePlayerIdentity);
                        }
                        var serialisedPlayer = JsonConvert.SerializeObject(auditablePlayer);
                        await _auditRepository.CreateAudit(new AuditRecord
                        {
                            Action = AuditAction.Update,
                            MemberKey = memberKey,
                            ActorName = memberName,
                            EntityUri = auditablePlayer.EntityUri,
                            State = serialisedPlayer,
                            RedactedState = serialisedPlayer,
                            AuditDate = DateTime.UtcNow
                        }, transaction);

                        _logger.Info(LoggingTemplates.Updated, serialisedPlayer, memberName, memberKey, GetType(), nameof(UpdatePlayerIdentity));

                        transaction.Commit();

                        return new RepositoryResult<PlayerIdentityUpdateResult, PlayerIdentity> { Status = PlayerIdentityUpdateResult.Success, Result = auditablePlayerIdentity };
                    }
                }
            }
        }

        private async Task UpdatePlayerRoute(Guid playerId, IDbTransaction transaction)
        {
            // Get Player and PlayerIdentity data from the original tables rather than PlayerInMatchStatistics because the original tables
            // will be updated when a player identity is renamed, and we need to see the change immediately.
            // Updates to PlayerInMatchStatistics are done asynchronously and the data will not be updated by the time this is called.

            var sql = $@"SELECT pi.PlayerRoute, pi.PlayerIdentityName, COUNT(DISTINCT MatchId) AS TotalMatches
                         FROM {Views.PlayerIdentity} pi 
                         INNER JOIN {Tables.PlayerInMatchStatistics} stats ON pi.PlayerIdentityId = stats.PlayerIdentityId
                         WHERE pi.PlayerId = @PlayerId
                         GROUP BY pi.PlayerRoute, pi.PlayerIdentityId, pi.PlayerIdentityName";

            var identities = (await _dapperWrapper.QueryAsync<(string playerRoute, string playerIdentityName, int totalMatches)>(sql, new { PlayerId = playerId }, transaction).ConfigureAwait(false)).ToList();

            var currentRoute = identities[0].playerRoute;
            foreach (var playerIdentityName in identities.Select(x => x.playerIdentityName))
            {
                var suggestedRoute = _routeGenerator.GenerateRoute("/players", playerIdentityName!, NoiseWords.PlayerRoute);
                if (_routeGenerator.IsMatchingRoute(currentRoute, suggestedRoute))
                {
                    // Current route still matches one of the identities
                    return;
                }
            }

            // Current route doesn't match any of its identities. Assign a new one based on the identity that's played the most.
            var updatedRoute = (await _routeGenerator.GenerateUniqueRoute("/players", identities.First(x => x.totalMatches == identities.Max(pi => pi.totalMatches)).playerIdentityName, NoiseWords.PlayerRoute,
                            async route => await transaction.Connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Player} WHERE PlayerRoute = @PlayerRoute AND Deleted = 0", new { PlayerRoute = route }, transaction)
                        ).ConfigureAwait(false));

            await _dapperWrapper.ExecuteAsync($"UPDATE {Tables.Player} SET PlayerRoute = @PlayerRoute WHERE PlayerId = @PlayerId", new { PlayerRoute = updatedRoute, PlayerId = playerId }, transaction).ConfigureAwait(false);
            await RedirectPlayerRoute(currentRoute, updatedRoute, transaction);
        }
    }
}
