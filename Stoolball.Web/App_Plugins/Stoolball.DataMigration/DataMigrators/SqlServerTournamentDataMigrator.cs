using System;
using System.Globalization;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Competitions;
using Stoolball.Data.SqlServer;
using Stoolball.Logging;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Routing;
using Stoolball.Security;
using Stoolball.Teams;
using static Stoolball.Constants;
using Tables = Stoolball.Constants.Tables;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class SqlServerTournamentDataMigrator : ITournamentDataMigrator
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IRedirectsRepository _redirectsRepository;
        private readonly IAuditHistoryBuilder _auditHistoryBuilder;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger _logger;
        private readonly IRouteGenerator _routeGenerator;
        private readonly IDataRedactor _dataRedactor;

        public SqlServerTournamentDataMigrator(IDatabaseConnectionFactory databaseConnectionFactory, IRedirectsRepository redirectsRepository, IAuditHistoryBuilder auditHistoryBuilder,
            IAuditRepository auditRepository, ILogger logger, IRouteGenerator routeGenerator, IDataRedactor dataRedactor)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
            _auditHistoryBuilder = auditHistoryBuilder ?? throw new ArgumentNullException(nameof(auditHistoryBuilder));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
            _dataRedactor = dataRedactor ?? throw new ArgumentNullException(nameof(dataRedactor));
        }

        /// <summary>
        /// Clear down all the tournament data ready for a fresh import
        /// </summary>
        /// <returns></returns>
        public async Task DeleteTournaments()
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    await connection.ExecuteAsync($"DELETE FROM {Tables.Comment} WHERE TournamentId IS NOT NULL", null, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.TournamentSeason}", null, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.TournamentTeam}", null, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.Tournament}", null, transaction).ConfigureAwait(false);

                    await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/tournaments/", transaction).ConfigureAwait(false);

                    transaction.Commit();
                }
            }
        }

        /// <summary>
        /// Save the supplied tournament to the database with its existing <see cref="Tournament.TournamentId"/>
        /// </summary>
        public async Task<Tournament> MigrateTournament(MigratedTournament tournament)
        {
            if (tournament is null)
            {
                throw new ArgumentNullException(nameof(tournament));
            }

            var migratedTournament = CreateAuditableCopy(tournament);
            migratedTournament.TournamentId = Guid.NewGuid();

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    migratedTournament.TournamentRoute = _routeGenerator.GenerateRoute("/tournaments", migratedTournament.TournamentName + " " + migratedTournament.StartTime.Date.ToString("dMMMyyyy", CultureInfo.CurrentCulture), NoiseWords.TournamentRoute);
                    int count;
                    do
                    {
                        count = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Tournament} WHERE TournamentRoute = @TournamentRoute", new { migratedTournament.TournamentRoute }, transaction).ConfigureAwait(false);
                        if (count > 0)
                        {
                            migratedTournament.TournamentRoute = _routeGenerator.IncrementRoute(migratedTournament.TournamentRoute);
                        }
                    }
                    while (count > 0);

                    _auditHistoryBuilder.BuildInitialAuditHistory(tournament, migratedTournament, nameof(SqlServerTournamentDataMigrator), CreateRedactedCopy);
                    migratedTournament.MemberKey = migratedTournament.History.Count > 0 ? migratedTournament.History[0].MemberKey : null;

                    if (migratedTournament.MigratedTournamentLocationId.HasValue)
                    {
                        migratedTournament.TournamentLocation = new MatchLocation
                        {
                            MatchLocationId = await connection.ExecuteScalarAsync<Guid>($"SELECT MatchLocationId FROM {Tables.MatchLocation} WHERE MigratedMatchLocationId = @MigratedTournamentLocationId", new { migratedTournament.MigratedTournamentLocationId }, transaction).ConfigureAwait(false)
                        };
                    }

                    await connection.ExecuteAsync($@"INSERT INTO {Tables.Tournament}
						(TournamentId, MigratedTournamentId, TournamentName, MatchLocationId, QualificationType, PlayerType, PlayersPerTeam, OversPerInningsDefault, 
                         MaximumTeamsInTournament, SpacesInTournament, StartTime, StartTimeIsKnown, TournamentNotes, TournamentRoute, MemberKey)
						VALUES 
                        (@TournamentId, @MigratedTournamentId, @TournamentName, @MatchLocationId, @QualificationType, @PlayerType, @PlayersPerTeam, @OversPerInningsDefault, 
                         @MaximumTeamsInTournament, @SpacesInTournament, @StartTime, @StartTimeIsKnown, @TournamentNotes, @TournamentRoute, @MemberKey)",
                     new
                     {
                         migratedTournament.TournamentId,
                         migratedTournament.MigratedTournamentId,
                         migratedTournament.TournamentName,
                         migratedTournament.TournamentLocation?.MatchLocationId,
                         QualificationType = migratedTournament.QualificationType?.ToString(),
                         PlayerType = migratedTournament.PlayerType.ToString(),
                         migratedTournament.PlayersPerTeam,
                         migratedTournament.OversPerInningsDefault,
                         migratedTournament.MaximumTeamsInTournament,
                         migratedTournament.SpacesInTournament,
                         migratedTournament.StartTime,
                         migratedTournament.StartTimeIsKnown,
                         migratedTournament.TournamentNotes,
                         migratedTournament.TournamentRoute,
                         migratedTournament.MemberKey
                     },
                    transaction).ConfigureAwait(false);

                    foreach (var team in migratedTournament.MigratedTeams)
                    {
                        team.Team = new Team
                        {
                            TeamId = await connection.ExecuteScalarAsync<Guid>($"SELECT TeamId FROM {Tables.Team} WHERE MigratedTeamId = @MigratedTeamId", new { team.MigratedTeamId }, transaction).ConfigureAwait(false)
                        };

                        team.MatchTeamId = Guid.NewGuid();

                        await connection.ExecuteAsync($@"INSERT INTO {Tables.TournamentTeam} 
								(TournamentTeamId, TournamentId, TeamId, TeamRole) VALUES (@TournamentTeamId, @TournamentId, @TeamId, @TeamRole)",
                            new
                            {
                                TournamentTeamId = team.MatchTeamId,
                                migratedTournament.TournamentId,
                                team.Team.TeamId,
                                TeamRole = TournamentTeamRole.Confirmed.ToString()
                            },
                            transaction).ConfigureAwait(false);
                    }

                    foreach (var migratedSeasonId in migratedTournament.MigratedSeasonIds)
                    {
                        var season = new Season { SeasonId = await connection.ExecuteScalarAsync<Guid>($"SELECT SeasonId FROM {Tables.Season} WHERE MigratedSeasonId = @MigratedSeasonId", new { MigratedSeasonId = migratedSeasonId }, transaction).ConfigureAwait(false) };
                        migratedTournament.Seasons.Add(season);

                        await connection.ExecuteAsync($@"INSERT INTO {Tables.TournamentSeason} 
								(TournamentSeasonId, TournamentId, SeasonId) VALUES (@TournamentSeasonId, @TournamentId, @SeasonId)",
                            new
                            {
                                TournamentSeasonId = Guid.NewGuid(),
                                migratedTournament.TournamentId,
                                season.SeasonId
                            },
                            transaction).ConfigureAwait(false);
                    }

                    await connection.ExecuteAsync($@"UPDATE {Tables.Team} SET 
							TeamRoute = CONCAT(@TournamentRoute, SUBSTRING(TeamRoute, 6, LEN(TeamRoute)-5))
							WHERE TeamType = 'Transient' 
							AND TeamRoute NOT LIKE '/tournaments%'
							AND TeamId IN (
								SELECT TeamId FROM {Tables.TournamentTeam} WHERE TournamentId = @TournamentId
							)",
                        new
                        {
                            migratedTournament.TournamentRoute,
                            migratedTournament.TournamentId
                        },
                        transaction).ConfigureAwait(false);

                    await connection.ExecuteAsync($@"UPDATE {Tables.TeamVersion} SET 
							UntilDate = @UntilDate
							WHERE TeamId IN (
								SELECT t.TeamId FROM {Tables.Team} t INNER JOIN {Tables.TournamentTeam} tt ON t.TeamId = tt.TeamId WHERE t.TeamType = 'Transient' AND tt.TournamentId = @TournamentId
							)",
                    new
                    {
                        UntilDate = new DateTime(migratedTournament.StartTime.Year, 12, 31),
                        migratedTournament.TournamentId
                    },
                    transaction).ConfigureAwait(false);

                    await connection.ExecuteAsync($@"UPDATE SkybrudRedirects SET 
							DestinationUrl = CONCAT(@TournamentRoute, SUBSTRING(DestinationUrl, 6, LEN(DestinationUrl)-5))
							WHERE DestinationUrl LIKE '/[0-9][0-9][0-9][0-9][0-9]%'
							AND Url LIKE '/{tournament.TournamentRoute.TrimStart('/')}%'",
                       new
                       {
                           migratedTournament.TournamentRoute
                       },
                       transaction).ConfigureAwait(false);

                    await _redirectsRepository.InsertRedirect(tournament.TournamentRoute, migratedTournament.TournamentRoute, string.Empty, transaction).ConfigureAwait(false);
                    await _redirectsRepository.InsertRedirect(tournament.TournamentRoute, migratedTournament.TournamentRoute, "/statistics", transaction).ConfigureAwait(false);
                    await _redirectsRepository.InsertRedirect(tournament.TournamentRoute, migratedTournament.TournamentRoute, "/calendar.ics", transaction).ConfigureAwait(false);

                    foreach (var audit in migratedTournament.History)
                    {
                        await _auditRepository.CreateAudit(audit, transaction).ConfigureAwait(false);
                    }

                    transaction.Commit();

                    _logger.Info(GetType(), LoggingTemplates.Migrated, CreateRedactedCopy(migratedTournament), GetType(), nameof(MigrateTournament));
                }
            }

            return migratedTournament;
        }

        private MigratedTournament CreateRedactedCopy(MigratedTournament tournament)
        {
            var redacted = CreateAuditableCopy(tournament);
            redacted.TournamentNotes = _dataRedactor.RedactPersonalData(tournament.TournamentNotes);
            redacted.History.Clear();
            return redacted;
        }

        private static MigratedTournament CreateAuditableCopy(MigratedTournament tournament)
        {
            return new MigratedTournament
            {
                TournamentId = tournament.TournamentId,
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
                TournamentNotes = tournament.TournamentNotes,
            };
        }
    }
}
