using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stoolball.Logging;
using Stoolball.Routing;
using Stoolball.Teams;
using Umbraco.Core.Scoping;
using static Stoolball.Data.SqlServer.Constants;
using Tables = Stoolball.Data.SqlServer.Constants.Tables;
using UmbracoLogging = Umbraco.Core.Logging;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class SqlServerPlayerDataMigrator : IPlayerDataMigrator
    {
        private readonly IRedirectsRepository _redirectsRepository;
        private readonly IScopeProvider _scopeProvider;
        private readonly IAuditHistoryBuilder _auditHistoryBuilder;
        private readonly IAuditRepository _auditRepository;
        private readonly UmbracoLogging.ILogger _logger;
        private readonly IRouteGenerator _routeGenerator;

        public SqlServerPlayerDataMigrator(IRedirectsRepository redirectsRepository, IScopeProvider scopeProvider, IAuditHistoryBuilder auditHistoryBuilder, IAuditRepository auditRepository,
            UmbracoLogging.ILogger logger, IRouteGenerator routeGenerator)
        {
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
            _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
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
            try
            {
                using (var scope = _scopeProvider.CreateScope())
                {
                    var database = scope.Database;

                    using (var transaction = database.GetTransaction())
                    {
                        await database.ExecuteAsync("DELETE FROM SkybrudRedirects WHERE DestinationUrl LIKE '/players/%'").ConfigureAwait(false);
                        await database.ExecuteAsync($"DELETE FROM {Tables.PlayerIdentity}").ConfigureAwait(false);
                        await database.ExecuteAsync($"DELETE FROM {Tables.Player}").ConfigureAwait(false);
                        transaction.Complete();
                    }

                    scope.Complete();
                }
            }
            catch (Exception e)
            {
                _logger.Error(typeof(SqlServerPlayerDataMigrator), e);
                throw;
            }
        }

        /// <summary>
        /// Save the supplied player to the database with its existing <see cref="PlayerIdentity.PlayerIdentityId"/>
        /// </summary>
        public async Task<Player> MigratePlayer(MigratedPlayerIdentity player)
        {
            if (player is null)
            {
                throw new System.ArgumentNullException(nameof(player));
            }
            try
            {
                var migratedPlayerIdentity = new MigratedPlayerIdentity
                {
                    PlayerIdentityId = Guid.NewGuid(),
                    PlayerId = Guid.NewGuid(),
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
                    PlayerId = migratedPlayerIdentity.PlayerId,
                    PlayerName = player.PlayerIdentityName,
                    PlayerIdentities = new List<PlayerIdentity> { migratedPlayerIdentity }
                };

                using (var scope = _scopeProvider.CreateScope())
                {
                    try
                    {
                        var database = scope.Database;

                        var teamId = await database.ExecuteScalarAsync<Guid>($"SELECT TeamId FROM {Tables.Team} WHERE MigratedTeamId = @0", migratedPlayerIdentity.MigratedTeamId).ConfigureAwait(false);

                        migratedPlayer.PlayerRoute = _routeGenerator.GenerateRoute($"/players", migratedPlayer.PlayerName, NoiseWords.PlayerRoute);

                        int count;
                        do
                        {
                            count = await database.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Player} WHERE PlayerRoute = @PlayerRoute", new { migratedPlayer.PlayerRoute }).ConfigureAwait(false);
                            if (count > 0)
                            {
                                migratedPlayer.PlayerRoute = _routeGenerator.IncrementRoute(migratedPlayer.PlayerRoute);
                            }
                        }
                        while (count > 0);

                        _auditHistoryBuilder.BuildInitialAuditHistory(player, migratedPlayerIdentity, nameof(SqlServerPlayerDataMigrator));

                        using (var transaction = database.GetTransaction())
                        {
                            await database.ExecuteAsync($@"INSERT INTO {Tables.Player} (PlayerId, PlayerName, PlayerRoute) VALUES (@0, @1, @2)",
                                  migratedPlayer.PlayerId,
                                  migratedPlayer.PlayerName,
                                  migratedPlayer.PlayerRoute
                              ).ConfigureAwait(false);

                            await database.ExecuteAsync($@"INSERT INTO {Tables.PlayerIdentity}
							(PlayerIdentityId, PlayerId, MigratedPlayerIdentityId, PlayerIdentityName, PlayerIdentityComparableName, TeamId, 
								FirstPlayed, LastPlayed, TotalMatches, MissedMatches, Probability)
							VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10)",
                                migratedPlayerIdentity.PlayerIdentityId,
                                migratedPlayerIdentity.PlayerId,
                                migratedPlayerIdentity.MigratedPlayerIdentityId,
                                migratedPlayerIdentity.PlayerIdentityName,
                                migratedPlayerIdentity.ComparableName(),
                                teamId,
                                migratedPlayerIdentity.FirstPlayed,
                                migratedPlayerIdentity.LastPlayed,
                                migratedPlayerIdentity.TotalMatches,
                                migratedPlayerIdentity.MissedMatches,
                                migratedPlayerIdentity.Probability).ConfigureAwait(false);

                            transaction.Complete();
                        }

                    }
                    catch (Exception e)
                    {
                        _logger.Error(typeof(SqlServerPlayerDataMigrator), e);
                        throw;
                    }
                    scope.Complete();
                }

                await _redirectsRepository.InsertRedirect(player.PlayerIdentityRoute, migratedPlayer.PlayerRoute + "/batting", string.Empty).ConfigureAwait(false);
                await _redirectsRepository.InsertRedirect(player.PlayerIdentityRoute, migratedPlayer.PlayerRoute, "/bowling").ConfigureAwait(false);

                foreach (var audit in migratedPlayerIdentity.History)
                {
                    await _auditRepository.CreateAudit(audit).ConfigureAwait(false);
                }
                return migratedPlayer;
            }
            catch (Exception e)
            {
                _logger.Error(typeof(SqlServerPlayerDataMigrator), e);
                throw;
            }
        }
    }
}
