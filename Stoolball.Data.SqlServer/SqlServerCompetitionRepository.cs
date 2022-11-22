using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Newtonsoft.Json;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Html;
using Stoolball.Logging;
using Stoolball.Routing;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// Writes stoolball competition data to the Umbraco database
    /// </summary>
    public class SqlServerCompetitionRepository : ICompetitionRepository
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger<SqlServerCompetitionRepository> _logger;
        private readonly ISeasonRepository _seasonRepository;
        private readonly IRouteGenerator _routeGenerator;
        private readonly IRedirectsRepository _redirectsRepository;
        private readonly IHtmlSanitizer _htmlSanitiser;
        private readonly IStoolballEntityCopier _copier;
        private readonly IUrlFormatter _urlFormatter;
        private readonly ISocialMediaAccountFormatter _socialMediaAccountFormatter;

        public SqlServerCompetitionRepository(
            IDatabaseConnectionFactory databaseConnectionFactory,
            IAuditRepository auditRepository,
            ILogger<SqlServerCompetitionRepository> logger,
            ISeasonRepository seasonRepository,
            IRouteGenerator routeGenerator,
            IRedirectsRepository redirectsRepository,
            IHtmlSanitizer htmlSanitiser,
            IStoolballEntityCopier copier,
            IUrlFormatter urlFormatter,
            ISocialMediaAccountFormatter socialMediaAccountFormatter)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
            _htmlSanitiser = htmlSanitiser ?? throw new ArgumentNullException(nameof(htmlSanitiser));
            _copier = copier ?? throw new ArgumentNullException(nameof(copier));
            _urlFormatter = urlFormatter ?? throw new ArgumentNullException(nameof(urlFormatter));
            _socialMediaAccountFormatter = socialMediaAccountFormatter ?? throw new ArgumentNullException(nameof(socialMediaAccountFormatter));
        }

        /// <summary>
        /// Creates a stoolball competition and populates the <see cref="Competition.CompetitionId"/>
        /// </summary>
        /// <returns>The created competition</returns>
        public async Task<Competition> CreateCompetition(Competition competition, Guid memberKey, string memberName)
        {
            if (competition is null)
            {
                throw new ArgumentNullException(nameof(competition));
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            var auditableCompetition = _copier.CreateAuditableCopy(competition);
            auditableCompetition.CompetitionId = Guid.NewGuid();
            auditableCompetition.Introduction = _htmlSanitiser.Sanitize(auditableCompetition.Introduction);
            auditableCompetition.PublicContactDetails = _htmlSanitiser.Sanitize(auditableCompetition.PublicContactDetails);
            auditableCompetition.PrivateContactDetails = _htmlSanitiser.Sanitize(auditableCompetition.PrivateContactDetails);
            auditableCompetition.Facebook = _urlFormatter.PrefixHttpsProtocol(auditableCompetition.Facebook)?.ToString();
            auditableCompetition.Twitter = _socialMediaAccountFormatter.PrefixAtSign(auditableCompetition.Twitter);
            auditableCompetition.Instagram = _socialMediaAccountFormatter.PrefixAtSign(auditableCompetition.Instagram);
            auditableCompetition.YouTube = _urlFormatter.PrefixHttpsProtocol(auditableCompetition.YouTube)?.ToString();
            auditableCompetition.Website = _urlFormatter.PrefixHttpsProtocol(auditableCompetition.Website)?.ToString();

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    auditableCompetition.CompetitionRoute = await _routeGenerator.GenerateUniqueRoute(
                      "/competitions", auditableCompetition.CompetitionName, NoiseWords.CompetitionRoute,
                      async route => await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Competition} WHERE CompetitionRoute = @CompetitionRoute", new { CompetitionRoute = route }, transaction).ConfigureAwait(false)
                    ).ConfigureAwait(false);

                    await connection.ExecuteAsync(
                        $@"INSERT INTO {Tables.Competition} (CompetitionId, PlayerType, Introduction, PublicContactDetails, PrivateContactDetails, 
                                Facebook, Twitter, Instagram, YouTube, Website, CompetitionRoute, MemberGroupKey, MemberGroupName) 
                                VALUES (@CompetitionId, @PlayerType, @Introduction, @PublicContactDetails, @PrivateContactDetails, 
                                @Facebook, @Twitter, @Instagram, @YouTube, @Website, @CompetitionRoute, @MemberGroupKey, @MemberGroupName)",
                        new
                        {
                            auditableCompetition.CompetitionId,
                            auditableCompetition.PlayerType,
                            auditableCompetition.Introduction,
                            auditableCompetition.PublicContactDetails,
                            auditableCompetition.PrivateContactDetails,
                            auditableCompetition.Facebook,
                            auditableCompetition.Twitter,
                            auditableCompetition.Instagram,
                            auditableCompetition.YouTube,
                            auditableCompetition.Website,
                            auditableCompetition.CompetitionRoute,
                            auditableCompetition.MemberGroupKey,
                            auditableCompetition.MemberGroupName
                        }, transaction).ConfigureAwait(false);

                    await connection.ExecuteAsync($@"INSERT INTO {Tables.CompetitionVersion} 
                                (CompetitionVersionId, CompetitionId, CompetitionName, ComparableName, FromDate, UntilDate) 
                                VALUES (@CompetitionVersionId, @CompetitionId, @CompetitionName, @ComparableName, @FromDate, @UntilDate)",
                       new
                       {
                           CompetitionVersionId = Guid.NewGuid(),
                           auditableCompetition.CompetitionId,
                           auditableCompetition.CompetitionName,
                           ComparableName = auditableCompetition.ComparableName(),
                           FromDate = auditableCompetition.FromYear.HasValue ? new DateTime(auditableCompetition.FromYear.Value, 1, 1) : DateTime.UtcNow.Date,
                           UntilDate = auditableCompetition.UntilYear.HasValue ? new DateTime(auditableCompetition.UntilYear.Value, 12, 31) : (DateTime?)null
                       }, transaction).ConfigureAwait(false);

                    var redacted = _copier.CreateRedactedCopy(auditableCompetition);
                    await _auditRepository.CreateAudit(new AuditRecord
                    {
                        Action = AuditAction.Create,
                        MemberKey = memberKey,
                        ActorName = memberName,
                        EntityUri = auditableCompetition.EntityUri,
                        State = JsonConvert.SerializeObject(auditableCompetition),
                        RedactedState = JsonConvert.SerializeObject(redacted),
                        AuditDate = DateTime.UtcNow
                    }, transaction).ConfigureAwait(false);

                    transaction.Commit();

                    _logger.Info(LoggingTemplates.Created, redacted, memberName, memberKey, GetType(), nameof(SqlServerCompetitionRepository.CreateCompetition));
                }
            }

            return auditableCompetition;
        }


        /// <summary>
        /// Updates a stoolball competition
        /// </summary>
        public async Task<Competition> UpdateCompetition(Competition competition, Guid memberKey, string memberName)
        {
            if (competition is null)
            {
                throw new ArgumentNullException(nameof(competition));
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            var auditableCompetition = _copier.CreateAuditableCopy(competition);
            auditableCompetition.Introduction = _htmlSanitiser.Sanitize(auditableCompetition.Introduction);
            auditableCompetition.PublicContactDetails = _htmlSanitiser.Sanitize(auditableCompetition.PublicContactDetails);
            auditableCompetition.PrivateContactDetails = _htmlSanitiser.Sanitize(auditableCompetition.PrivateContactDetails);
            auditableCompetition.Facebook = _urlFormatter.PrefixHttpsProtocol(auditableCompetition.Facebook)?.ToString();
            auditableCompetition.Twitter = _socialMediaAccountFormatter.PrefixAtSign(auditableCompetition.Twitter);
            auditableCompetition.Instagram = _socialMediaAccountFormatter.PrefixAtSign(auditableCompetition.Instagram);
            auditableCompetition.YouTube = _urlFormatter.PrefixHttpsProtocol(auditableCompetition.YouTube)?.ToString();
            auditableCompetition.Website = _urlFormatter.PrefixHttpsProtocol(auditableCompetition.Website)?.ToString();

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    auditableCompetition.CompetitionRoute = await _routeGenerator.GenerateUniqueRoute(
                        competition.CompetitionRoute,
                        "/competitions", auditableCompetition.CompetitionName, NoiseWords.CompetitionRoute,
                        async route => await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Competition} WHERE CompetitionRoute = @CompetitionRoute", new { CompetitionRoute = route }, transaction).ConfigureAwait(false)
                    ).ConfigureAwait(false);

                    await connection.ExecuteAsync(
                        $@"UPDATE {Tables.Competition} SET
                                PlayerType = @PlayerType, 
                                Introduction = @Introduction, 
                                PublicContactDetails = @PublicContactDetails, 
                                PrivateContactDetails = @PrivateContactDetails, 
                                Facebook = @Facebook, 
                                Twitter = @Twitter, 
                                Instagram = @Instagram, 
                                YouTube = @YouTube, 
                                Website = @Website,
                                CompetitionRoute = @CompetitionRoute
						        WHERE CompetitionId = @CompetitionId",
                        new
                        {
                            auditableCompetition.PlayerType,
                            auditableCompetition.Introduction,
                            auditableCompetition.PublicContactDetails,
                            auditableCompetition.PrivateContactDetails,
                            auditableCompetition.Facebook,
                            auditableCompetition.Twitter,
                            auditableCompetition.Instagram,
                            auditableCompetition.YouTube,
                            auditableCompetition.Website,
                            auditableCompetition.CompetitionRoute,
                            auditableCompetition.CompetitionId
                        }, transaction).ConfigureAwait(false);

                    await connection.ExecuteAsync($@"UPDATE {Tables.CompetitionVersion} SET 
                            CompetitionName = @CompetitionName, 
                            ComparableName = @ComparableName,
                            UntilDate = @UntilDate
                            WHERE CompetitionId = @CompetitionId",
                        new
                        {
                            auditableCompetition.CompetitionName,
                            ComparableName = auditableCompetition.ComparableName(),
                            UntilDate = auditableCompetition.UntilYear.HasValue ? new DateTime(auditableCompetition.UntilYear.Value, 12, 31).ToUniversalTime() : (DateTime?)null,
                            auditableCompetition.CompetitionId
                        },
                        transaction).ConfigureAwait(false);

                    if (competition.CompetitionRoute != auditableCompetition.CompetitionRoute)
                    {
                        await _redirectsRepository.InsertRedirect(competition.CompetitionRoute, auditableCompetition.CompetitionRoute, null, transaction).ConfigureAwait(false);

                        // Update the season routes to match the amended competition route
                        var seasonRoutes = await connection.QueryAsync<string>($"SELECT SeasonRoute FROM {Tables.Season} WHERE CompetitionId = @CompetitionId", new { auditableCompetition.CompetitionId }, transaction).ConfigureAwait(false);
                        foreach (var route in seasonRoutes)
                        {
                            await _redirectsRepository.InsertRedirect(route, auditableCompetition.CompetitionRoute + route.Substring(competition.CompetitionRoute.Length), null, transaction).ConfigureAwait(false);
                        }

                        await connection.ExecuteAsync($@"UPDATE {Tables.Season} 
                                SET SeasonRoute = CONCAT(@CompetitionRoute, SUBSTRING(SeasonRoute, {competition.CompetitionRoute.Length + 1}, LEN(SeasonRoute)-{competition.CompetitionRoute.Length})) 
                                WHERE CompetitionId = @CompetitionId", new { auditableCompetition.CompetitionId, auditableCompetition.CompetitionRoute }, transaction).ConfigureAwait(false);
                    }

                    var redacted = _copier.CreateRedactedCopy(auditableCompetition);
                    await _auditRepository.CreateAudit(new AuditRecord
                    {
                        Action = AuditAction.Update,
                        MemberKey = memberKey,
                        ActorName = memberName,
                        EntityUri = auditableCompetition.EntityUri,
                        State = JsonConvert.SerializeObject(auditableCompetition),
                        RedactedState = JsonConvert.SerializeObject(redacted),
                        AuditDate = DateTime.UtcNow
                    }, transaction).ConfigureAwait(false);

                    transaction.Commit();

                    _logger.Info(LoggingTemplates.Updated, redacted, memberName, memberKey, GetType(), nameof(SqlServerCompetitionRepository.UpdateCompetition));
                }
            }

            return auditableCompetition;
        }

        /// <summary>
        /// Deletes a stoolball competition
        /// </summary>
        public async Task DeleteCompetition(Competition competition, Guid memberKey, string memberName)
        {
            if (competition is null)
            {
                throw new ArgumentNullException(nameof(competition));
            }

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var seasonIdsNotSupplied = await connection.QueryAsync<Guid>($"SELECT SeasonId FROM {Tables.Season} WHERE CompetitionId = @CompetitionId AND SeasonId NOT IN @seasonIds",
                        new
                        {
                            competition.CompetitionId,
                            seasonIds = competition.Seasons.Select(x => x.SeasonId).OfType<Guid>()
                        },
                        transaction).ConfigureAwait(false);
                    if (seasonIdsNotSupplied.Any())
                    {
                        competition.Seasons.AddRange(seasonIdsNotSupplied.Select(x => new Season { SeasonId = x }));
                    }

                    await _seasonRepository.DeleteSeasons(competition.Seasons, memberKey, memberName, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"UPDATE {Tables.PlayerInMatchStatistics} SET CompetitionId = NULL WHERE CompetitionId = @CompetitionId", new { competition.CompetitionId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.CompetitionVersion} WHERE CompetitionId = @CompetitionId", new { competition.CompetitionId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.Competition} WHERE CompetitionId = @CompetitionId", new { competition.CompetitionId }, transaction).ConfigureAwait(false);

                    await _redirectsRepository.DeleteRedirectsByDestinationPrefix(competition.CompetitionRoute, transaction).ConfigureAwait(false);

                    var auditableCompetition = _copier.CreateAuditableCopy(competition);
                    var redacted = _copier.CreateRedactedCopy(auditableCompetition);
                    await _auditRepository.CreateAudit(new AuditRecord
                    {
                        Action = AuditAction.Delete,
                        MemberKey = memberKey,
                        ActorName = memberName,
                        EntityUri = auditableCompetition.EntityUri,
                        State = JsonConvert.SerializeObject(auditableCompetition),
                        RedactedState = JsonConvert.SerializeObject(redacted),
                        AuditDate = DateTime.UtcNow
                    }, transaction).ConfigureAwait(false);

                    transaction.Commit();

                    _logger.Info(LoggingTemplates.Deleted, redacted, memberName, memberKey, GetType(), nameof(DeleteCompetition));
                }
            }
        }
    }
}
