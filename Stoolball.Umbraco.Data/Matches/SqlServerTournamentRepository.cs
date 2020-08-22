using Dapper;
using Ganss.XSS;
using Newtonsoft.Json;
using Stoolball.Audit;
using Stoolball.Matches;
using Stoolball.Routing;
using Stoolball.Teams;
using Stoolball.Umbraco.Data.Audit;
using Stoolball.Umbraco.Data.Redirects;
using System;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Umbraco.Data.Matches
{
    /// <summary>
    /// Writes stoolball tournament data to the Umbraco database
    /// </summary>
    public class SqlServerTournamentRepository : ITournamentRepository
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger _logger;
        private readonly IRouteGenerator _routeGenerator;
        private readonly IRedirectsRepository _redirectsRepository;
        private readonly IHtmlSanitizer _htmlSanitiser;

        public SqlServerTournamentRepository(IDatabaseConnectionFactory databaseConnectionFactory, IAuditRepository auditRepository, ILogger logger, IRouteGenerator routeGenerator,
            IRedirectsRepository redirectsRepository, IHtmlSanitizer htmlSanitiser)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
            _htmlSanitiser = htmlSanitiser ?? throw new ArgumentNullException(nameof(htmlSanitiser));

            _htmlSanitiser.AllowedTags.Clear();
            _htmlSanitiser.AllowedTags.Add("p");
            _htmlSanitiser.AllowedTags.Add("h2");
            _htmlSanitiser.AllowedTags.Add("strong");
            _htmlSanitiser.AllowedTags.Add("em");
            _htmlSanitiser.AllowedTags.Add("ul");
            _htmlSanitiser.AllowedTags.Add("ol");
            _htmlSanitiser.AllowedTags.Add("li");
            _htmlSanitiser.AllowedTags.Add("a");
            _htmlSanitiser.AllowedTags.Add("br");
            _htmlSanitiser.AllowedAttributes.Clear();
            _htmlSanitiser.AllowedAttributes.Add("href");
            _htmlSanitiser.AllowedCssProperties.Clear();
            _htmlSanitiser.AllowedAtRules.Clear();
        }

        /// <summary>
        /// Creates a stoolball tournament
        /// </summary>
        public async Task<Tournament> CreateTournament(Tournament tournament, Guid memberKey, string memberName)
        {
            if (tournament is null)
            {
                throw new ArgumentNullException(nameof(tournament));
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            try
            {
                tournament.TournamentId = Guid.NewGuid();
                tournament.TournamentNotes = _htmlSanitiser.Sanitize(tournament.TournamentNotes);

                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        tournament.TournamentRoute = _routeGenerator.GenerateRoute("/tournaments", tournament.TournamentName + " " + tournament.StartTime.Date.ToString("dMMMyyyy", CultureInfo.CurrentCulture), NoiseWords.TournamentRoute);
                        int count;
                        do
                        {
                            count = await transaction.Connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Tournament} WHERE TournamentRoute = @TournamentRoute", new { tournament.TournamentRoute }, transaction).ConfigureAwait(false);
                            if (count > 0)
                            {
                                tournament.TournamentRoute = _routeGenerator.IncrementRoute(tournament.TournamentRoute);
                            }
                        }
                        while (count > 0);

                        await connection.ExecuteAsync($@"INSERT INTO {Tables.Tournament}
						(TournamentId, TournamentName, MatchLocationId, PlayerType, PlayersPerTeam, OversPerInningsDefault,
						 QualificationType, StartTime, StartTimeIsKnown, TournamentNotes, TournamentRoute, MemberKey)
						VALUES (@TournamentId, @TournamentName, @MatchLocationId, @PlayerType, @PlayersPerTeam, @OversPerInningsDefault, 
                        @QualificationType, @StartTime, @StartTimeIsKnown, @TournamentNotes, @TournamentRoute, @MemberKey)",
                        new
                        {
                            tournament.TournamentId,
                            tournament.TournamentName,
                            tournament.TournamentLocation?.MatchLocationId,
                            PlayerType = tournament.PlayerType.ToString(),
                            tournament.PlayersPerTeam,
                            tournament.OversPerInningsDefault,
                            QualificationType = tournament.QualificationType.ToString(),
                            StartTime = tournament.StartTime.UtcDateTime,
                            tournament.StartTimeIsKnown,
                            tournament.TournamentNotes,
                            tournament.TournamentRoute,
                            MemberKey = memberKey
                        }, transaction).ConfigureAwait(false);

                        foreach (var team in tournament.Teams)
                        {
                            await connection.ExecuteAsync($@"INSERT INTO {Tables.TournamentTeam} 
								(TournamentTeamId, TournamentId, TeamId, TeamRole) 
                                VALUES (@TournamentTeamId, @TournamentId, @TeamId, @TeamRole)",
                                new
                                {
                                    TournamentTeamId = Guid.NewGuid(),
                                    tournament.TournamentId,
                                    team.Team.TeamId,
                                    TeamRole = team.TeamRole.ToString()
                                },
                                transaction).ConfigureAwait(false);
                        }

                        foreach (var season in tournament.Seasons)
                        {
                            await connection.ExecuteAsync($@"INSERT INTO {Tables.TournamentSeason} 
								(TournamentSeasonId, TournamentId, SeasonId) 
                                VALUES (@TournamentSeasonId, @TournamentId, @SeasonId)",
                                new
                                {
                                    TournamentSeasonId = Guid.NewGuid(),
                                    tournament.TournamentId,
                                    season.SeasonId
                                },
                                transaction).ConfigureAwait(false);
                        }

                        transaction.Commit();
                    }
                }

                await _auditRepository.CreateAudit(new AuditRecord
                {
                    Action = AuditAction.Create,
                    MemberKey = memberKey,
                    ActorName = memberName,
                    EntityUri = tournament.EntityUri,
                    State = JsonConvert.SerializeObject(tournament),
                    AuditDate = DateTime.UtcNow
                }).ConfigureAwait(false);
            }
            catch (SqlException ex)
            {
                _logger.Error(typeof(SqlServerTournamentRepository), ex);
            }

            return tournament;
        }


        /// <summary>
        /// Updates a stoolball tournament
        /// </summary>
        public async Task<Tournament> UpdateTournament(Tournament tournament, Guid memberKey, string memberName)
        {
            if (tournament is null)
            {
                throw new ArgumentNullException(nameof(tournament));
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            try
            {
                string routeBeforeUpdate = tournament.TournamentRoute;
                tournament.TournamentNotes = _htmlSanitiser.Sanitize(tournament.TournamentNotes);

                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        tournament.TournamentRoute = _routeGenerator.GenerateRoute("/tournaments", tournament.TournamentName + " " + tournament.StartTime.Date.ToString("dMMMyyyy", CultureInfo.CurrentCulture), NoiseWords.TournamentRoute);
                        if (tournament.TournamentRoute != routeBeforeUpdate)
                        {
                            int count;
                            do
                            {
                                count = await transaction.Connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Tournament} WHERE TournamentRoute = @TournamentRoute", new { tournament.TournamentRoute }, transaction).ConfigureAwait(false);
                                if (count > 0)
                                {
                                    tournament.TournamentRoute = _routeGenerator.IncrementRoute(tournament.TournamentRoute);
                                }
                            }
                            while (count > 0);
                        }

                        await connection.ExecuteAsync($@"UPDATE {Tables.Tournament} SET
						    TournamentName = @TournamentName,
                            MatchLocationId = @MatchLocationId, 
                            PlayerType = @PlayerType,
                            PlayersPerTeam = @PlayersPerTeam,
                            OversPerInningsDefault = @OversPerInningsDefault,
						    QualificationType = @QualificationType, 
                            StartTime = @StartTime, 
                            StartTimeIsKnown = @StartTimeIsKnown, 
                            TournamentNotes = @TournamentNotes, 
                            TournamentRoute = @TournamentRoute
                            WHERE TournamentId = @TournamentId",
                        new
                        {
                            tournament.TournamentName,
                            tournament.TournamentLocation?.MatchLocationId,
                            PlayerType = tournament.PlayerType.ToString(),
                            tournament.PlayersPerTeam,
                            tournament.OversPerInningsDefault,
                            QualificationType = tournament.QualificationType.ToString(),
                            StartTime = tournament.StartTime.UtcDateTime,
                            tournament.StartTimeIsKnown,
                            tournament.TournamentNotes,
                            tournament.TournamentRoute,
                            tournament.TournamentId
                        }, transaction).ConfigureAwait(false);

                        // Set approximate start time based on 45 mins per match
                        await connection.ExecuteAsync($@"UPDATE {Tables.Match} SET
                            MatchLocationId = @MatchLocationId,
                            PlayerType = @PlayerType,
                            PlayersPerTeam = @PlayersPerTeam,
                            StartTime = DATEADD(MINUTE, 45*(ISNULL(OrderInTournament,1)-1), @StartTime)
                            WHERE TournamentId = @TournamentId",
                            new
                            {
                                tournament.TournamentLocation?.MatchLocationId,
                                PlayerType = tournament.PlayerType.ToString(),
                                tournament.PlayersPerTeam,
                                tournament.StartTime,
                                tournament.TournamentId
                            },
                            transaction).ConfigureAwait(false);

                        await connection.ExecuteAsync($@"UPDATE {Tables.MatchInnings} SET
                            Overs = @OversPerInningsDefault
                            WHERE MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId)",
                            new
                            {
                                tournament.OversPerInningsDefault,
                                tournament.TournamentId
                            },
                            transaction).ConfigureAwait(false);

                        if (routeBeforeUpdate != tournament.TournamentRoute)
                        {
                            // Update the transient team routes to match the amended tournament route
                            await connection.ExecuteAsync($@"UPDATE {Tables.Team} 
                                SET TeamRoute = CONCAT(@TournamentRoute, SUBSTRING(TeamRoute, {routeBeforeUpdate.Length + 1}, LEN(TeamRoute)-{routeBeforeUpdate.Length})) 
                                WHERE TeamRoute LIKE CONCAT(@routeBeforeUpdate, '/teams/%')",
                                new { tournament.TournamentRoute, routeBeforeUpdate }, transaction).ConfigureAwait(false);
                        }

                        transaction.Commit();
                    }
                }

                if (routeBeforeUpdate != tournament.TournamentRoute)
                {
                    await _redirectsRepository.InsertRedirect(routeBeforeUpdate, tournament.TournamentRoute, null).ConfigureAwait(false);
                }

                await _auditRepository.CreateAudit(new AuditRecord
                {
                    Action = AuditAction.Update,
                    MemberKey = memberKey,
                    ActorName = memberName,
                    EntityUri = tournament.EntityUri,
                    State = JsonConvert.SerializeObject(tournament),
                    AuditDate = DateTime.UtcNow
                }).ConfigureAwait(false);
            }
            catch (SqlException ex)
            {
                _logger.Error(typeof(SqlServerTournamentRepository), ex);
            }

            return tournament;
        }

        /// <summary>
        /// Deletes a stoolball tournament
        /// </summary>
        public async Task DeleteTournament(Tournament tournament, Guid memberKey, string memberName)
        {
            if (tournament is null)
            {
                throw new ArgumentNullException(nameof(tournament));
            }

            try
            {
                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        // Delete all matches and statistics in the tournament
                        await connection.ExecuteAsync($"DELETE FROM {Tables.PlayerMatchStatistics} WHERE TournamentId = @TournamentId", new { tournament.TournamentId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"DELETE FROM {Tables.Over} WHERE MatchInningsId IN (SELECT MatchInningsId FROM {Tables.MatchInnings} WHERE MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId))", new { tournament.TournamentId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"DELETE FROM {Tables.PlayerInnings} WHERE MatchInningsId IN (SELECT MatchInningsId FROM {Tables.MatchInnings} WHERE MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId))", new { tournament.TournamentId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"DELETE FROM {Tables.MatchInnings} WHERE MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId)", new { tournament.TournamentId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"DELETE FROM {Tables.MatchTeam} WHERE MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId)", new { tournament.TournamentId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"DELETE FROM {Tables.MatchComment} WHERE MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId)", new { tournament.TournamentId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"DELETE FROM {Tables.MatchAward} WHERE MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId)", new { tournament.TournamentId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"DELETE FROM {Tables.Match} WHERE MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId)", new { tournament.TournamentId }, transaction).ConfigureAwait(false);

                        // Remove teams from the tournament. Delete the transient teams. (Player performances for transient teams should already be removed above.)
                        var transientTeamIds = await connection.QueryAsync<Guid>($@"SELECT t.TeamId
                            FROM { Tables.TournamentTeam} tt
                            INNER JOIN { Tables.Team} t ON tt.TeamId = t.TeamId
                            WHERE t.TeamType = '{TeamType.Transient.ToString()}' AND tt.TournamentId = @TournamentId"
                            , new { tournament.TournamentId }, transaction).ConfigureAwait(false);

                        await connection.ExecuteAsync($"DELETE FROM {Tables.TournamentTeam} WHERE TournamentId = @TournamentId", new { tournament.TournamentId }, transaction).ConfigureAwait(false);

                        if (transientTeamIds.Any())
                        {
                            await connection.ExecuteAsync($@"DELETE FROM {Tables.PlayerIdentity} WHERE TeamId IN @transientTeamIds", new { transientTeamIds }, transaction).ConfigureAwait(false);
                            await connection.ExecuteAsync($@"DELETE FROM {Tables.TeamMatchLocation} WHERE TeamId IN @transientTeamIds", new { transientTeamIds }, transaction).ConfigureAwait(false);
                            await connection.ExecuteAsync($@"DELETE FROM {Tables.TeamName} WHERE TeamId IN @transientTeamIds", new { transientTeamIds }, transaction).ConfigureAwait(false);
                            await connection.ExecuteAsync($@"DELETE FROM {Tables.Team} WHERE TeamId IN @transientTeamIds", new { transientTeamIds }, transaction).ConfigureAwait(false);
                        }

                        // Delete other related data and the tournament itself
                        await connection.ExecuteAsync($"DELETE FROM {Tables.TournamentSeason} WHERE TournamentId = @TournamentId", new { tournament.TournamentId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"DELETE FROM {Tables.TournamentComment} WHERE TournamentId = @TournamentId", new { tournament.TournamentId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"DELETE FROM {Tables.Tournament} WHERE TournamentId = @TournamentId", new { tournament.TournamentId }, transaction).ConfigureAwait(false);
                        transaction.Commit();
                    }
                }

                await _redirectsRepository.DeleteRedirectsByDestinationPrefix(tournament.TournamentRoute).ConfigureAwait(false);

                await _auditRepository.CreateAudit(new AuditRecord
                {
                    Action = AuditAction.Delete,
                    MemberKey = memberKey,
                    ActorName = memberName,
                    EntityUri = tournament.EntityUri,
                    State = JsonConvert.SerializeObject(tournament),
                    AuditDate = DateTime.UtcNow
                }).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.Error<SqlServerMatchRepository>(e);
                throw;
            }
        }

    }
}
