using Dapper;
using Ganss.XSS;
using Newtonsoft.Json;
using Stoolball.Audit;
using Stoolball.Matches;
using Stoolball.Routing;
using Stoolball.Umbraco.Data.Audit;
using Stoolball.Umbraco.Data.Redirects;
using System;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Umbraco.Data.Matches
{
    /// <summary>
    /// Writes stoolball match data to the Umbraco database
    /// </summary>
    public class SqlServerMatchRepository : IMatchRepository
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger _logger;
        private readonly IRouteGenerator _routeGenerator;
        private readonly IRedirectsRepository _redirectsRepository;
        private readonly IHtmlSanitizer _htmlSanitiser;

        public SqlServerMatchRepository(IDatabaseConnectionFactory databaseConnectionFactory, IAuditRepository auditRepository, ILogger logger, IRouteGenerator routeGenerator,
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
        /// Creates a stoolball match
        /// </summary>
        public async Task<Match> CreateMatch(Match match, Guid memberKey, string memberName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Deletes a stoolball match
        /// </summary>
        public async Task DeleteMatch(Match match, Guid memberKey, string memberName)
        {
            if (match is null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            try
            {
                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        await connection.ExecuteAsync($"DELETE FROM {Tables.PlayerMatchStatistics} WHERE MatchId = @MatchId", new { match.MatchId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"DELETE FROM {Tables.Over} WHERE MatchInningsId IN (SELECT MatchInningsId FROM {Tables.MatchInnings} WHERE MatchId = @MatchId)", new { match.MatchId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"DELETE FROM {Tables.PlayerInnings} WHERE MatchInningsId IN (SELECT MatchInningsId FROM {Tables.MatchInnings} WHERE MatchId = @MatchId)", new { match.MatchId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"DELETE FROM {Tables.MatchInnings} WHERE MatchId = @MatchId", new { match.MatchId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"DELETE FROM {Tables.MatchTeam} WHERE MatchId = @MatchId", new { match.MatchId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"DELETE FROM {Tables.MatchComment} WHERE MatchId = @MatchId", new { match.MatchId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"DELETE FROM {Tables.MatchAward} WHERE MatchId = @MatchId", new { match.MatchId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"DELETE FROM {Tables.Match} WHERE MatchId = @MatchId", new { match.MatchId }, transaction).ConfigureAwait(false);
                        transaction.Commit();
                    }
                }

                await _redirectsRepository.DeleteRedirectsByDestinationPrefix(match.MatchRoute).ConfigureAwait(false);

                await _auditRepository.CreateAudit(new AuditRecord
                {
                    Action = AuditAction.Delete,
                    MemberKey = memberKey,
                    ActorName = memberName,
                    EntityUri = match.EntityUri,
                    State = JsonConvert.SerializeObject(match),
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
