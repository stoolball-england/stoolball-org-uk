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
