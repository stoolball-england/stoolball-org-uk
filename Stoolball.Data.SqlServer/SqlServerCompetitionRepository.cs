﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Ganss.XSS;
using Newtonsoft.Json;
using Stoolball.Competitions;
using Stoolball.Logging;
using Stoolball.Routing;
using Stoolball.Security;
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
        private readonly ILogger _logger;
        private readonly IRouteGenerator _routeGenerator;
        private readonly IRedirectsRepository _redirectsRepository;
        private readonly IHtmlSanitizer _htmlSanitiser;
        private readonly IDataRedactor _dataRedactor;

        public SqlServerCompetitionRepository(IDatabaseConnectionFactory databaseConnectionFactory, IAuditRepository auditRepository, ILogger logger, IRouteGenerator routeGenerator,
            IRedirectsRepository redirectsRepository, IHtmlSanitizer htmlSanitiser, IDataRedactor dataRedactor)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
            _htmlSanitiser = htmlSanitiser ?? throw new ArgumentNullException(nameof(htmlSanitiser));
            _dataRedactor = dataRedactor ?? throw new ArgumentNullException(nameof(dataRedactor));
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
        private static Competition CreateAuditableCopy(Competition competition)
        {
            return new Competition
            {
                CompetitionId = competition.CompetitionId,
                CompetitionName = competition.CompetitionName,
                FromYear = competition.FromYear,
                UntilYear = competition.UntilYear,
                PlayerType = competition.PlayerType,
                Introduction = competition.Introduction,
                PublicContactDetails = competition.PublicContactDetails,
                PrivateContactDetails = competition.PrivateContactDetails,
                Facebook = competition.Facebook,
                Twitter = competition.Twitter,
                Instagram = competition.Instagram,
                YouTube = competition.YouTube,
                Website = competition.Website,
                CompetitionRoute = competition.CompetitionRoute,
                MemberGroupKey = competition.MemberGroupKey,
                MemberGroupName = competition.MemberGroupName,
                Seasons = competition.Seasons.Select(x => new Season { SeasonId = x.SeasonId }).ToList()
            };
        }

        private Competition CreateRedactedCopy(Competition competition)
        {
            var redacted = CreateAuditableCopy(competition);
            redacted.Introduction = _dataRedactor.RedactPersonalData(redacted.Introduction);
            redacted.PrivateContactDetails = _dataRedactor.RedactAll(redacted.PrivateContactDetails);
            redacted.PublicContactDetails = _dataRedactor.RedactAll(redacted.PublicContactDetails);
            return redacted;
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

            var auditableCompetition = CreateAuditableCopy(competition);
            auditableCompetition.CompetitionId = Guid.NewGuid();
            auditableCompetition.Introduction = _htmlSanitiser.Sanitize(auditableCompetition.Introduction);
            auditableCompetition.PublicContactDetails = _htmlSanitiser.Sanitize(auditableCompetition.PublicContactDetails);
            auditableCompetition.PrivateContactDetails = _htmlSanitiser.Sanitize(auditableCompetition.PrivateContactDetails);
            auditableCompetition.Facebook = PrefixUrlProtocol(auditableCompetition.Facebook);
            auditableCompetition.Twitter = PrefixAtSign(auditableCompetition.Twitter);
            auditableCompetition.Instagram = PrefixAtSign(auditableCompetition.Instagram);
            auditableCompetition.YouTube = PrefixUrlProtocol(auditableCompetition.YouTube);
            auditableCompetition.Website = PrefixUrlProtocol(auditableCompetition.Website);

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    auditableCompetition.CompetitionRoute = _routeGenerator.GenerateRoute("/competitions", auditableCompetition.CompetitionName, NoiseWords.CompetitionRoute);
                    int count;
                    do
                    {
                        count = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Competition} WHERE CompetitionRoute = @CompetitionRoute", new { auditableCompetition.CompetitionRoute }, transaction).ConfigureAwait(false);
                        if (count > 0)
                        {
                            auditableCompetition.CompetitionRoute = _routeGenerator.IncrementRoute(auditableCompetition.CompetitionRoute);
                        }
                    }
                    while (count > 0);

                    await connection.ExecuteAsync(
                        $@"INSERT INTO {Tables.Competition} (CompetitionId, CompetitionName, FromYear, UntilYear, PlayerType, 
                                Introduction, PublicContactDetails, PrivateContactDetails, Facebook, Twitter, Instagram, YouTube, Website, CompetitionRoute, 
                                MemberGroupKey, MemberGroupName) 
                                VALUES (@CompetitionId, @CompetitionName, @FromYear, @UntilYear, @PlayerType, @Introduction, 
                                @PublicContactDetails, @PrivateContactDetails, @Facebook, @Twitter, @Instagram, @YouTube, @Website, @CompetitionRoute, 
                                @MemberGroupKey, @MemberGroupName)",
                        new
                        {
                            auditableCompetition.CompetitionId,
                            auditableCompetition.CompetitionName,
                            auditableCompetition.FromYear,
                            auditableCompetition.UntilYear,
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

                    var redacted = CreateRedactedCopy(auditableCompetition);
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

                    _logger.Info(GetType(), LoggingTemplates.Created, redacted, memberName, memberKey, GetType(), nameof(SqlServerCompetitionRepository.CreateCompetition));
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

            var auditableCompetition = CreateAuditableCopy(competition);
            auditableCompetition.Introduction = _htmlSanitiser.Sanitize(auditableCompetition.Introduction);
            auditableCompetition.PublicContactDetails = _htmlSanitiser.Sanitize(auditableCompetition.PublicContactDetails);
            auditableCompetition.PrivateContactDetails = _htmlSanitiser.Sanitize(auditableCompetition.PrivateContactDetails);
            auditableCompetition.Facebook = PrefixUrlProtocol(auditableCompetition.Facebook);
            auditableCompetition.Twitter = PrefixAtSign(auditableCompetition.Twitter);
            auditableCompetition.Instagram = PrefixAtSign(auditableCompetition.Instagram);
            auditableCompetition.YouTube = PrefixUrlProtocol(auditableCompetition.YouTube);
            auditableCompetition.Website = PrefixUrlProtocol(auditableCompetition.Website);

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {

                    auditableCompetition.CompetitionRoute = _routeGenerator.GenerateRoute("/competitions", auditableCompetition.CompetitionName, NoiseWords.CompetitionRoute);
                    if (auditableCompetition.CompetitionRoute != competition.CompetitionRoute)
                    {
                        int count;
                        do
                        {
                            count = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Competition} WHERE CompetitionRoute = @CompetitionRoute", new { auditableCompetition.CompetitionRoute }, transaction).ConfigureAwait(false);
                            if (count > 0)
                            {
                                auditableCompetition.CompetitionRoute = _routeGenerator.IncrementRoute(auditableCompetition.CompetitionRoute);
                            }
                        }
                        while (count > 0);
                    }

                    await connection.ExecuteAsync(
                        $@"UPDATE {Tables.Competition} SET
                                CompetitionName = @CompetitionName,
                                FromYear = @FromYear,
                                UntilYear = @UntilYear,
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
                            auditableCompetition.CompetitionName,
                            auditableCompetition.FromYear,
                            auditableCompetition.UntilYear,
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

                    if (competition.CompetitionRoute != auditableCompetition.CompetitionRoute)
                    {
                        // Update the season routes to match the amended competition route
                        await connection.ExecuteAsync($@"UPDATE {Tables.Season} 
                                SET SeasonRoute = CONCAT(@CompetitionRoute, SUBSTRING(SeasonRoute, {competition.CompetitionRoute.Length + 1}, LEN(SeasonRoute)-{competition.CompetitionRoute.Length})) 
                                WHERE CompetitionId = @CompetitionId", new { auditableCompetition.CompetitionId, auditableCompetition.CompetitionRoute }, transaction).ConfigureAwait(false);
                    }

                    if (competition.CompetitionRoute != auditableCompetition.CompetitionRoute)
                    {
                        await _redirectsRepository.InsertRedirect(competition.CompetitionRoute, auditableCompetition.CompetitionRoute, null, transaction).ConfigureAwait(false);
                    }

                    var redacted = CreateRedactedCopy(auditableCompetition);
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

                    _logger.Info(GetType(), LoggingTemplates.Updated, redacted, memberName, memberKey, GetType(), nameof(SqlServerCompetitionRepository.UpdateCompetition));
                }
            }

            return competition;
        }

        private static string PrefixUrlProtocol(string url)
        {
            url = url?.Trim();
            if (!string.IsNullOrEmpty(url) && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
            }
            return url;
        }

        private static string PrefixAtSign(string account)
        {
            account = account?.Trim();
            if (!string.IsNullOrEmpty(account) && !account.StartsWith("@", StringComparison.OrdinalIgnoreCase))
            {
                account = "@" + account;
            }
            return account;
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
                    await connection.ExecuteAsync($@"DELETE FROM {Tables.SeasonTeam} WHERE SeasonId IN (SELECT SeasonId FROM {Tables.Season} WHERE CompetitionId = @CompetitionId)", new { competition.CompetitionId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($@"DELETE FROM {Tables.SeasonPointsRule} WHERE SeasonId IN (SELECT SeasonId FROM {Tables.Season} WHERE CompetitionId = @CompetitionId)", new { competition.CompetitionId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($@"DELETE FROM {Tables.SeasonPointsAdjustment} WHERE SeasonId IN (SELECT SeasonId FROM {Tables.Season} WHERE CompetitionId = @CompetitionId)", new { competition.CompetitionId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($@"DELETE FROM {Tables.SeasonMatchType} WHERE SeasonId IN (SELECT SeasonId FROM {Tables.Season} WHERE CompetitionId = @CompetitionId)", new { competition.CompetitionId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.TournamentSeason} WHERE SeasonId IN (SELECT SeasonId FROM {Tables.Season} WHERE CompetitionId = @CompetitionId)", new { competition.CompetitionId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($@"DELETE FROM {Tables.Season} WHERE SeasonId IN (SELECT SeasonId FROM {Tables.Season} WHERE CompetitionId = @CompetitionId)", new { competition.CompetitionId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($@"DELETE FROM {Tables.Competition} WHERE CompetitionId = @CompetitionId", new { competition.CompetitionId }, transaction).ConfigureAwait(false);

                    await _redirectsRepository.DeleteRedirectsByDestinationPrefix(competition.CompetitionRoute, transaction).ConfigureAwait(false);

                    var auditableCompetition = CreateAuditableCopy(competition);
                    var redacted = CreateRedactedCopy(auditableCompetition);
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

                    _logger.Info(GetType(), LoggingTemplates.Deleted, redacted, memberName, memberKey, GetType(), nameof(SqlServerCompetitionRepository.DeleteCompetition));
                }
            }
        }
    }
}