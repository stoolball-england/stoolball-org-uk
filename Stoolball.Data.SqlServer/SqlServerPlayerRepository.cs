using System;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using Newtonsoft.Json;
using Stoolball.Logging;
using Stoolball.Routing;
using Stoolball.Teams;
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
        private readonly ILogger _logger;
        private readonly IRouteGenerator _routeGenerator;

        public SqlServerPlayerRepository(IDatabaseConnectionFactory databaseConnectionFactory, IAuditRepository auditRepository, ILogger logger, IRouteGenerator routeGenerator)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
        }

        private static PlayerIdentity CreateAuditableCopy(PlayerIdentity playerIdentity)
        {
            return new PlayerIdentity
            {
                PlayerIdentityId = playerIdentity.PlayerIdentityId,
                PlayerIdentityName = playerIdentity.PlayerIdentityName,
                Team = new Team { TeamId = playerIdentity.Team.TeamId }
            };
        }

        /// <summary>
        /// Finds an existing player identity or creates it if it is not found
        /// </summary>
        /// <returns>The <see cref="PlayerIdentity.PlayerIdentityId"/> of the created or matched player identity</returns>
        public async Task<Guid> CreateOrMatchPlayerIdentity(PlayerIdentity playerIdentity, Guid memberKey, string memberName, IDbTransaction transaction)
        {
            if (playerIdentity is null)
            {
                throw new ArgumentNullException(nameof(playerIdentity));
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

            Player player;
            var matchedPlayer = await transaction.Connection.ExecuteScalarAsync<Guid?>(
                    $"SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE PlayerIdentityComparableName = @PlayerIdentityComparableName AND TeamId = @TeamId",
                    new
                    {
                        PlayerIdentityComparableName = playerIdentity.ComparableName(),
                        playerIdentity.Team.TeamId
                    }, transaction).ConfigureAwait(false);

            if (matchedPlayer.HasValue)
            {
                return matchedPlayer.Value;
            }

            var auditablePlayerIdentity = CreateAuditableCopy(playerIdentity);

            auditablePlayerIdentity.PlayerIdentityId = Guid.NewGuid();
            auditablePlayerIdentity.PlayerIdentityName = CapitaliseName(auditablePlayerIdentity.PlayerIdentityName);
            auditablePlayerIdentity.TotalMatches = 1;

            player = new Player
            {
                PlayerId = Guid.NewGuid(),
                PlayerName = auditablePlayerIdentity.PlayerIdentityName,
                PlayerRoute = _routeGenerator.GenerateRoute($"/players", auditablePlayerIdentity.PlayerIdentityName, NoiseWords.PlayerRoute)
            };
            player.PlayerIdentities.Add(auditablePlayerIdentity);

            int count;
            do
            {
                count = await transaction.Connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Player} WHERE PlayerRoute = @PlayerRoute", new { player.PlayerRoute }, transaction).ConfigureAwait(false);
                if (count > 0)
                {
                    player.PlayerRoute = _routeGenerator.IncrementRoute(player.PlayerRoute);
                }
            }
            while (count > 0);

            await transaction.Connection.ExecuteAsync(
                  $@"INSERT INTO {Tables.Player} 
                                               (PlayerId, PlayerName, PlayerRoute) 
                                               VALUES 
                                               (@PlayerId, @PlayerName, @PlayerRoute)",
                  new
                  {
                      player.PlayerId,
                      player.PlayerName,
                      player.PlayerRoute
                  }, transaction).ConfigureAwait(false);

            await transaction.Connection.ExecuteAsync($@"INSERT INTO {Tables.PlayerIdentity} 
                                (PlayerIdentityId, PlayerId, PlayerIdentityName, PlayerIdentityComparableName, TeamId, TotalMatches) 
                                VALUES (@PlayerIdentityId, @PlayerId, @PlayerIdentityName, @PlayerIdentityComparableName, @TeamId, @TotalMatches)",
                   new
                   {
                       auditablePlayerIdentity.PlayerIdentityId,
                       player.PlayerId,
                       auditablePlayerIdentity.PlayerIdentityName,
                       PlayerIdentityComparableName = auditablePlayerIdentity.ComparableName(),
                       auditablePlayerIdentity.Team.TeamId,
                       auditablePlayerIdentity.TotalMatches
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

            _logger.Info(GetType(), LoggingTemplates.Created, player, memberName, memberKey, GetType(), nameof(SqlServerPlayerRepository.CreateOrMatchPlayerIdentity));

            return auditablePlayerIdentity.PlayerIdentityId.Value;
        }

        private static string CapitaliseName(string playerIdentityName)
        {
            if (playerIdentityName is null)
            {
                throw new ArgumentNullException(nameof(playerIdentityName));
            }

            var segments = Regex.Replace(playerIdentityName, @"\s", " ").Split(' ');
            for (var i = 0; i < segments.Length; i++)
            {
                segments[i] = CapitaliseNameSegment(segments[i]);
            }
            segments = string.Join(" ", segments).Split('-');
            for (var i = 0; i < segments.Length; i++)
            {
                segments[i] = CapitaliseNameSegment(segments[i]);
            }
            return string.Join("-", segments);
        }

        private static string CapitaliseNameSegment(string segment)
        {
            return segment.Length > 1 && Array.IndexOf(new[] { "de", "la", "di", "da", "della", "van", "von" }, segment) == -1
              ? segment.Substring(0, 1).ToUpper(CultureInfo.CurrentCulture) + segment.Substring(1)
              : segment;
        }
    }
}
