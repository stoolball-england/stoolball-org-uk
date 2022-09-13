using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Newtonsoft.Json;
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
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger<SqlServerPlayerRepository> _logger;
        private readonly IRouteGenerator _routeGenerator;
        private readonly IStoolballEntityCopier _copier;
        private readonly IPlayerNameFormatter _playerNameFormatter;

        public SqlServerPlayerRepository(
            IDatabaseConnectionFactory databaseConnectionFactory,
            IAuditRepository auditRepository,
            ILogger<SqlServerPlayerRepository> logger,
            IRouteGenerator routeGenerator,
            IStoolballEntityCopier copier,
            IPlayerNameFormatter playerNameFormatter)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
            _copier = copier ?? throw new ArgumentNullException(nameof(copier));
            _playerNameFormatter = playerNameFormatter ?? throw new ArgumentNullException(nameof(playerNameFormatter));
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
        public async Task LinkPlayerToMemberAccount(Player player, Guid memberKey, string memberName)
        {
            if (player == null || !player.PlayerId.HasValue)
            {
                throw new ArgumentException(nameof(player), $"{nameof(player)} cannot be null and must have a PlayerId");
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
                    // Is the player already claimed, either by this member or someone else?
                    var existingMemberForPlayer = await connection.QuerySingleOrDefaultAsync<Guid?>($"SELECT MemberKey FROM {Tables.Player} WHERE PlayerId = @PlayerId", player, transaction);
                    if (existingMemberForPlayer.HasValue)
                    {
                        transaction.Rollback();
                        return;
                    }

                    // Is this member already linked to a player?
                    var existingPlayerForMember = await connection.QuerySingleOrDefaultAsync<Player>($"SELECT TOP 1 PlayerId, PlayerRoute FROM {Tables.Player} WHERE MemberKey = @memberKey", new { memberKey }, transaction);
                    if (existingPlayerForMember == null)
                    {
                        // Link member to player record
                        await connection.ExecuteAsync($"UPDATE {Tables.Player} SET MemberKey = @memberKey WHERE PlayerId = @PlayerId", new { memberKey, player.PlayerId }, transaction);
                    }
                    else
                    {
                        // Move the player identities from this player id to the member's player id
                        var replaceWithExistingPlayer = new { ExistingPlayerId = existingPlayerForMember.PlayerId, ExistingPlayerRoute = existingPlayerForMember.PlayerRoute, PlayerId = player.PlayerId };
                        await connection.ExecuteAsync($"UPDATE {Tables.PlayerIdentity} SET PlayerId = @ExistingPlayerId WHERE PlayerId = @PlayerId", replaceWithExistingPlayer, transaction);
                        await connection.ExecuteAsync($"UPDATE {Tables.PlayerInMatchStatistics} SET PlayerId = @ExistingPlayerId, PlayerRoute = @ExistingPlayerRoute WHERE PlayerId = @PlayerId", replaceWithExistingPlayer, transaction);
                        await connection.ExecuteAsync($"DELETE FROM {Tables.Player} WHERE PlayerId = @PlayerId", player, transaction);
                    }

                    var serialisedPlayer = JsonConvert.SerializeObject(_copier.CreateAuditableCopy(player));
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

                    transaction.Commit();

                    _logger.Info(LoggingTemplates.Updated, serialisedPlayer, memberName, memberKey, GetType(), nameof(LinkPlayerToMemberAccount));
                }
            }
        }
    }
}
