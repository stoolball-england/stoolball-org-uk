using System;
using System.Data.SqlClient;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using Newtonsoft.Json;
using Stoolball.Audit;
using Stoolball.Routing;
using Stoolball.Teams;
using Stoolball.Umbraco.Data.Audit;
using Umbraco.Core;
using Umbraco.Core.Logging;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Umbraco.Data.Teams
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

        /// <summary>
        /// Finds an existing player identity or creates it if it is not found
        /// </summary>
        /// <returns>The <see cref="PlayerIdentity.PlayerIdentityId"/> of the created or matched player identity</returns>
        public async Task<Guid> CreateOrMatchPlayerIdentity(PlayerIdentity playerIdentity, Guid memberKey, string memberName)
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

            try
            {
                Player player;
                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    connection.Open();

                    var matchedPlayer = await connection.ExecuteScalarAsync<Guid?>(
                            $"SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE PlayerIdentityComparableName = @PlayerIdentityComparableName AND TeamId = @TeamId AND PlayerRole = '{PlayerRole.Player.ToString()}'",
                            new
                            {
                                PlayerIdentityComparableName = playerIdentity.ComparableName(),
                                playerIdentity.Team.TeamId
                            }).ConfigureAwait(false);

                    if (matchedPlayer.HasValue)
                    {
                        return matchedPlayer.Value;
                    }

                    using (var transaction = connection.BeginTransaction())
                    {
                        playerIdentity.PlayerIdentityId = Guid.NewGuid();
                        playerIdentity.PlayerRole = PlayerRole.Player;
                        playerIdentity.PlayerIdentityName = CapitaliseName(playerIdentity.PlayerIdentityName);
                        playerIdentity.TotalMatches = 1;

                        player = new Player
                        {
                            PlayerId = Guid.NewGuid(),
                            PlayerName = playerIdentity.PlayerIdentityName,
                            PlayerRoute = _routeGenerator.GenerateRoute($"/players", playerIdentity.PlayerIdentityName, NoiseWords.PlayerRoute)
                        };

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
                                (PlayerIdentityId, PlayerId, PlayerRole, PlayerIdentityName, PlayerIdentityComparableName, TeamId, TotalMatches) 
                                VALUES (@PlayerIdentityId, @PlayerId, @PlayerRole, @PlayerIdentityName, @PlayerIdentityComparableName, @TeamId, @TotalMatches)",
                               new
                               {
                                   playerIdentity.PlayerIdentityId,
                                   player.PlayerId,
                                   PlayerRole = playerIdentity.PlayerRole.ToString(),
                                   playerIdentity.PlayerIdentityName,
                                   PlayerIdentityComparableName = playerIdentity.ComparableName(),
                                   playerIdentity.Team.TeamId,
                                   playerIdentity.TotalMatches
                               }, transaction).ConfigureAwait(false);

                        transaction.Commit();
                    }
                }

                await _auditRepository.CreateAudit(new AuditRecord
                {
                    Action = AuditAction.Create,
                    MemberKey = memberKey,
                    ActorName = memberName,
                    EntityUri = player.EntityUri,
                    State = JsonConvert.SerializeObject(playerIdentity),
                    AuditDate = DateTime.UtcNow
                }).ConfigureAwait(false);
            }
            catch (SqlException ex)
            {
                _logger.Error(typeof(SqlServerTeamRepository), ex);
            }

            return playerIdentity.PlayerIdentityId.Value;
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
            return segment.Length > 1 && new[] { "de", "la", "di", "da", "della", "van", "von" }.IndexOf(segment) == -1
              ? segment.Substring(0, 1).ToUpper(CultureInfo.CurrentCulture) + segment.Substring(1)
              : segment;
        }
    }
}
