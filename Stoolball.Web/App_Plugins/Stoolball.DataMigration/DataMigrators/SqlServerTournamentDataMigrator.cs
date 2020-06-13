using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Teams;
using Stoolball.Umbraco.Data.Audit;
using Stoolball.Umbraco.Data.Redirects;
using System;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using Umbraco.Core.Scoping;
using Tables = Stoolball.Umbraco.Data.Constants.Tables;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class SqlServerTournamentDataMigrator : ITournamentDataMigrator
    {
        private readonly IRedirectsRepository _redirectsRepository;
        private readonly IScopeProvider _scopeProvider;
        private readonly IAuditHistoryBuilder _auditHistoryBuilder;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger _logger;

        public SqlServerTournamentDataMigrator(IRedirectsRepository redirectsRepository, IScopeProvider scopeProvider, IAuditHistoryBuilder auditHistoryBuilder, IAuditRepository auditRepository, ILogger logger)
        {
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
            _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
            _auditHistoryBuilder = auditHistoryBuilder ?? throw new ArgumentNullException(nameof(auditHistoryBuilder));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Clear down all the tournament data ready for a fresh import
        /// </summary>
        /// <returns></returns>
        public async Task DeleteTournaments()
        {
            try
            {
                using (var scope = _scopeProvider.CreateScope())
                {
                    var database = scope.Database;

                    using (var transaction = database.GetTransaction())
                    {
                        await database.ExecuteAsync($"DELETE FROM {Tables.TournamentComment}").ConfigureAwait(false);
                        await database.ExecuteAsync($"DELETE FROM {Tables.TournamentSeason}").ConfigureAwait(false);
                        await database.ExecuteAsync($"DELETE FROM {Tables.TournamentTeam}").ConfigureAwait(false);
                        await database.ExecuteAsync($"DELETE FROM {Tables.Tournament}").ConfigureAwait(false);
                        transaction.Complete();
                    }

                    scope.Complete();
                }
            }
            catch (Exception e)
            {
                _logger.Error<SqlServerTournamentDataMigrator>(e);
                throw;
            }

            await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/tournaments/").ConfigureAwait(false);
        }

        /// <summary>
        /// Save the supplied tournament to the database with its existing <see cref="Tournament.TournamentId"/>
        /// </summary>
        public async Task<Tournament> MigrateTournament(MigratedTournament tournament)
        {
            if (tournament is null)
            {
                throw new System.ArgumentNullException(nameof(tournament));
            }

            var migratedTournament = new MigratedTournament
            {
                TournamentId = Guid.NewGuid(),
                MigratedTournamentId = tournament.MigratedTournamentId,
                TournamentName = tournament.TournamentName?.Trim(),
                MigratedTournamentLocationId = tournament.MigratedTournamentLocationId,
                QualificationType = tournament.QualificationType,
                PlayerType = tournament.PlayerType,
                PlayersPerTeam = tournament.PlayersPerTeam,
                OversPerInningsDefault = tournament.OversPerInningsDefault,
                MaximumTeamsInTournament = tournament.MaximumTeamsInTournament,
                SpacesInTournament = tournament.SpacesInTournament,
                StartTime = tournament.StartTime,
                StartTimeIsKnown = tournament.StartTimeIsKnown,
                MigratedTeams = tournament.MigratedTeams,
                MigratedSeasonIds = tournament.MigratedSeasonIds,
                TournamentRoute = tournament.TournamentRoute,
                TournamentNotes = tournament.TournamentNotes,
            };

            if (migratedTournament.TournamentRoute.StartsWith("match/", StringComparison.OrdinalIgnoreCase))
            {
                migratedTournament.TournamentRoute = migratedTournament.TournamentRoute.Substring(6);
            }

            if (migratedTournament.TournamentRoute.EndsWith("-tournament", StringComparison.OrdinalIgnoreCase))
            {
                migratedTournament.TournamentRoute = migratedTournament.TournamentRoute.Substring(0, migratedTournament.TournamentRoute.Length - 6);
            }
            migratedTournament.TournamentRoute = "/tournaments/" + migratedTournament.TournamentRoute;

            _auditHistoryBuilder.BuildInitialAuditHistory(tournament, migratedTournament, nameof(SqlServerMatchDataMigrator));

            using (var scope = _scopeProvider.CreateScope())
            {
                try
                {
                    var database = scope.Database;
                    using (var transaction = database.GetTransaction())
                    {
                        if (migratedTournament.MigratedTournamentLocationId.HasValue)
                        {
                            migratedTournament.TournamentLocation = new MatchLocation
                            {
                                MatchLocationId = await database.ExecuteScalarAsync<Guid>($"SELECT MatchLocationId FROM {Tables.MatchLocation} WHERE MigratedMatchLocationId = @0", migratedTournament.MigratedTournamentLocationId).ConfigureAwait(false)
                            };
                        }

                        await database.ExecuteAsync($@"INSERT INTO {Tables.Tournament}
						(TournamentId, MigratedMatchId, TournamentName, MatchLocationId, QualificationType, PlayerType, PlayersPerTeam, 
                         OversPerInningsDefault, MaximumTeamsInTournament, SpacesInTournament, StartTime, StartTimeIsKnown, TournamentNotes, TournamentRoute)
						VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10, @11, @12, @13)",
                            migratedTournament.TournamentId,
                            migratedTournament.MigratedTournamentId,
                            migratedTournament.TournamentName,
                            migratedTournament.TournamentLocation?.MatchLocationId,
                            migratedTournament.QualificationType?.ToString(),
                            migratedTournament.PlayerType.ToString(),
                            migratedTournament.PlayersPerTeam,
                            migratedTournament.OversPerInningsDefault,
                            migratedTournament.MaximumTeamsInTournament,
                            migratedTournament.SpacesInTournament,
                            migratedTournament.StartTime,
                            migratedTournament.StartTimeIsKnown,
                            migratedTournament.TournamentNotes,
                            migratedTournament.TournamentRoute).ConfigureAwait(false);
                        foreach (var team in migratedTournament.MigratedTeams)
                        {
                            team.Team = new Team
                            {
                                TeamId = await database.ExecuteScalarAsync<Guid>($"SELECT TeamId FROM {Tables.Team} WHERE MigratedTeamId = @0", team.MigratedTeamId).ConfigureAwait(false)
                            };

                            await database.ExecuteAsync($@"INSERT INTO {Tables.TournamentTeam} 
								(TournamentTeamId, TournamentId, TeamId, TeamRole) VALUES (@0, @1, @2, @3)",
                                Guid.NewGuid(),
                                migratedTournament.TournamentId,
                                team.Team.TeamId,
                                TournamentTeamRole.Confirmed.ToString()).ConfigureAwait(false);
                        }
                        foreach (var season in migratedTournament.MigratedSeasonIds)
                        {
                            var seasonId = await database.ExecuteScalarAsync<Guid>($"SELECT SeasonId FROM {Tables.Season} WHERE MigratedSeasonId = @0", season).ConfigureAwait(false);

                            await database.ExecuteAsync($@"INSERT INTO {Tables.TournamentSeason} 
								(TournamentSeasonId, TournamentId, SeasonId) VALUES (@0, @1, @2)",
                                Guid.NewGuid(),
                                migratedTournament.TournamentId,
                                seasonId).ConfigureAwait(false);
                        }
                        await database.ExecuteAsync($@"UPDATE {Tables.Team} SET 
							TeamRoute = CONCAT(@0, SUBSTRING(TeamRoute, 6, LEN(TeamRoute)-5)),
							FromYear = @1,
							UntilYear = @1
							WHERE TeamType = 'Transient' 
							AND TeamRoute NOT LIKE '/tournaments%'
							AND TeamId IN (
								SELECT TeamId FROM {Tables.TournamentTeam} WHERE MatchId = @2
							)",
                            migratedTournament.TournamentRoute, migratedTournament.StartTime.Year, migratedTournament.TournamentId).ConfigureAwait(false);
                        transaction.Complete();
                    }

                }
                catch (Exception e)
                {
                    _logger.Error<SqlServerTournamentDataMigrator>(e);
                    throw;
                }
                scope.Complete();
            }

            await _redirectsRepository.InsertRedirect(tournament.TournamentRoute, migratedTournament.TournamentRoute, string.Empty).ConfigureAwait(false);
            await _redirectsRepository.InsertRedirect(tournament.TournamentRoute, migratedTournament.TournamentRoute, "/statistics").ConfigureAwait(false);
            await _redirectsRepository.InsertRedirect(tournament.TournamentRoute, migratedTournament.TournamentRoute, "/calendar.ics").ConfigureAwait(false);

            foreach (var audit in migratedTournament.History)
            {
                await _auditRepository.CreateAudit(audit).ConfigureAwait(false);
            }

            return migratedTournament;
        }
    }
}
