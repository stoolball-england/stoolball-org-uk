using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Data.SqlServer;
using Stoolball.Logging;
using Stoolball.Routing;
using Stoolball.Teams;
using static Stoolball.Constants;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class SqlServerPlayerDataMigrator : IPlayerDataMigrator
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IRedirectsRepository _redirectsRepository;
        private readonly IAuditHistoryBuilder _auditHistoryBuilder;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger _logger;
        private readonly IRouteGenerator _routeGenerator;

        public SqlServerPlayerDataMigrator(IDatabaseConnectionFactory databaseConnectionFactory, IRedirectsRepository redirectsRepository, IAuditHistoryBuilder auditHistoryBuilder, IAuditRepository auditRepository,
            ILogger logger, IRouteGenerator routeGenerator)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
            _auditHistoryBuilder = auditHistoryBuilder ?? throw new ArgumentNullException(nameof(auditHistoryBuilder));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
        }

        /// <summary>
        /// Clear down all the player data ready for a fresh import
        /// </summary>
        /// <returns></returns>
        public async Task DeletePlayers()
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    await connection.ExecuteAsync("DELETE FROM SkybrudRedirects WHERE DestinationUrl LIKE '/players/%'", null, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.PlayerIdentity}", null, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.Player}", null, transaction).ConfigureAwait(false);
                    transaction.Commit();
                }
            }
        }

        /// <summary>
        /// Save the supplied player to the database with its existing <see cref="PlayerIdentity.PlayerIdentityId"/>
        /// </summary>
        public async Task<Player> MigratePlayer(MigratedPlayerIdentity player)
        {
            if (player is null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            var migratedPlayerIdentity = new MigratedPlayerIdentity
            {
                PlayerIdentityId = Guid.NewGuid(),
                MigratedPlayerIdentityId = player.MigratedPlayerIdentityId,
                PlayerIdentityName = player.PlayerIdentityName,
                MigratedTeamId = player.MigratedTeamId,
                FirstPlayed = player.FirstPlayed,
                LastPlayed = player.LastPlayed,
                TotalMatches = player.TotalMatches,
                MissedMatches = player.MissedMatches,
                Probability = player.Probability
            };

            var migratedPlayer = new Player
            {
                PlayerId = Guid.NewGuid(),
                PlayerName = player.PlayerIdentityName,
                PlayerIdentities = new List<PlayerIdentity> { migratedPlayerIdentity }
            };


            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var teamId = await connection.ExecuteScalarAsync<Guid>($"SELECT TeamId FROM {Tables.Team} WHERE MigratedTeamId = @MigratedTeamId", new { migratedPlayerIdentity.MigratedTeamId }, transaction).ConfigureAwait(false);

                    migratedPlayer.PlayerRoute = _routeGenerator.GenerateRoute($"/players", migratedPlayer.PlayerName, NoiseWords.PlayerRoute);

                    int count;
                    do
                    {
                        count = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Player} WHERE PlayerRoute = @PlayerRoute", new { migratedPlayer.PlayerRoute }, transaction).ConfigureAwait(false);
                        if (count > 0)
                        {
                            migratedPlayer.PlayerRoute = _routeGenerator.IncrementRoute(migratedPlayer.PlayerRoute);
                        }
                    }
                    while (count > 0);

                    _auditHistoryBuilder.BuildInitialAuditHistory(player, migratedPlayerIdentity, nameof(SqlServerPlayerDataMigrator), x => x);

                    await connection.ExecuteAsync($@"INSERT INTO {Tables.Player} (PlayerId, PlayerName, PlayerRoute) VALUES (@PlayerId, @PlayerName, @PlayerRoute)",
                        new
                        {
                            migratedPlayer.PlayerId,
                            migratedPlayer.PlayerName,
                            migratedPlayer.PlayerRoute
                        },
                      transaction).ConfigureAwait(false);

                    await connection.ExecuteAsync($@"INSERT INTO {Tables.PlayerIdentity}
							(PlayerIdentityId, PlayerId, MigratedPlayerIdentityId, PlayerIdentityName, ComparableName, TeamId, 
						  	 FirstPlayed, LastPlayed, TotalMatches, MissedMatches, Probability)
							VALUES 
                            (@PlayerIdentityId, @PlayerId, @MigratedPlayerIdentityId, @PlayerIdentityName, @ComparableName, @TeamId, 
                             @FirstPlayed, @LastPlayed, @TotalMatches, @MissedMatches, @Probability)",
                     new
                     {
                         migratedPlayerIdentity.PlayerIdentityId,
                         migratedPlayer.PlayerId,
                         migratedPlayerIdentity.MigratedPlayerIdentityId,
                         migratedPlayerIdentity.PlayerIdentityName,
                         ComparableName = migratedPlayerIdentity.ComparableName(),
                         TeamId = teamId,
                         migratedPlayerIdentity.FirstPlayed,
                         migratedPlayerIdentity.LastPlayed,
                         migratedPlayerIdentity.TotalMatches,
                         migratedPlayerIdentity.MissedMatches,
                         migratedPlayerIdentity.Probability
                     },
                     transaction).ConfigureAwait(false);

                    await _redirectsRepository.InsertRedirect(player.PlayerIdentityRoute, migratedPlayer.PlayerRoute + "/batting", string.Empty, transaction).ConfigureAwait(false);
                    await _redirectsRepository.InsertRedirect(player.PlayerIdentityRoute, migratedPlayer.PlayerRoute, "/bowling", transaction).ConfigureAwait(false);

                    foreach (var audit in migratedPlayerIdentity.History)
                    {
                        await _auditRepository.CreateAudit(audit, transaction).ConfigureAwait(false);
                    }

                    transaction.Commit();

                    migratedPlayer.History.Clear();
                    migratedPlayerIdentity.History.Clear();
                    _logger.Info(GetType(), LoggingTemplates.Migrated, migratedPlayer, GetType(), nameof(MigratePlayer));
                }

                return migratedPlayer;
            }
        }
    }
}

