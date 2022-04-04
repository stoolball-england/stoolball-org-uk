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
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger<SqlServerPlayerRepository> _logger;
        private readonly IRouteGenerator _routeGenerator;
        private readonly IStoolballEntityCopier _copier;
        private readonly IPlayerNameFormatter _playerNameFormatter;

        public SqlServerPlayerRepository(IAuditRepository auditRepository, ILogger<SqlServerPlayerRepository> logger, IRouteGenerator routeGenerator, IStoolballEntityCopier copier,
            IPlayerNameFormatter playerNameFormatter)
        {
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
                    splitOn: "PlayerId").ConfigureAwait(false)).FirstOrDefault();

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
               async route => await transaction.Connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Player} WHERE PlayerRoute = @PlayerRoute", new { PlayerRoute = route }, transaction).ConfigureAwait(false)
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
                  }, transaction).ConfigureAwait(false);

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
                   }, transaction).ConfigureAwait(false);

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
            }, transaction).ConfigureAwait(false);

            _logger.Info(LoggingTemplates.Created, player, memberName, memberKey, GetType(), nameof(CreateOrMatchPlayerIdentity));

            player.PlayerIdentities.Clear();
            auditablePlayerIdentity.Player = player;
            return auditablePlayerIdentity;
        }

    }
}
