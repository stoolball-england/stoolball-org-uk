using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using Ganss.XSS;
using Newtonsoft.Json;
using Stoolball.Audit;
using Stoolball.Competitions;
using Stoolball.Routing;
using Stoolball.Umbraco.Data.Audit;
using Stoolball.Umbraco.Data.Redirects;
using Umbraco.Core.Logging;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Umbraco.Data.Competitions
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

        public SqlServerCompetitionRepository(IDatabaseConnectionFactory databaseConnectionFactory, IAuditRepository auditRepository, ILogger logger, IRouteGenerator routeGenerator,
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

            try
            {
                competition.CompetitionId = Guid.NewGuid();
                competition.Introduction = _htmlSanitiser.Sanitize(competition.Introduction);
                competition.PublicContactDetails = _htmlSanitiser.Sanitize(competition.PublicContactDetails);
                competition.PrivateContactDetails = _htmlSanitiser.Sanitize(competition.PrivateContactDetails);
                competition.Facebook = PrefixUrlProtocol(competition.Facebook);
                competition.Twitter = PrefixAtSign(competition.Twitter);
                competition.Instagram = PrefixAtSign(competition.Instagram);
                competition.YouTube = PrefixUrlProtocol(competition.YouTube);
                competition.Website = PrefixUrlProtocol(competition.Website);

                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        competition.CompetitionRoute = _routeGenerator.GenerateRoute("/competitions", competition.CompetitionName, NoiseWords.CompetitionRoute);
                        int count;
                        do
                        {
                            count = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Competition} WHERE CompetitionRoute = @CompetitionRoute", new { competition.CompetitionRoute }, transaction).ConfigureAwait(false);
                            if (count > 0)
                            {
                                competition.CompetitionRoute = _routeGenerator.IncrementRoute(competition.CompetitionRoute);
                            }
                        }
                        while (count > 0);

                        await connection.ExecuteAsync(
                            $@"INSERT INTO {Tables.Competition} (CompetitionId, CompetitionName, FromYear, UntilYear, PlayerType, 
                                Introduction, PublicContactDetails, PrivateContactDetails, Facebook, Twitter, Instagram, YouTube, Website, CompetitionRoute, 
                                MemberGroupId, MemberGroupName) 
                                VALUES (@CompetitionId, @CompetitionName, @FromYear, @UntilYear, @PlayerType, @Introduction, 
                                @PublicContactDetails, @PrivateContactDetails, @Facebook, @Twitter, @Instagram, @YouTube, @Website, @CompetitionRoute, 
                                @MemberGroupId, @MemberGroupName)",
                            new
                            {
                                competition.CompetitionId,
                                competition.CompetitionName,
                                competition.FromYear,
                                competition.UntilYear,
                                competition.PlayerType,
                                competition.Introduction,
                                competition.PublicContactDetails,
                                competition.PrivateContactDetails,
                                competition.Facebook,
                                competition.Twitter,
                                competition.Instagram,
                                competition.YouTube,
                                competition.Website,
                                competition.CompetitionRoute,
                                competition.MemberGroupId,
                                competition.MemberGroupName
                            }, transaction).ConfigureAwait(false);

                        transaction.Commit();
                    }
                }

                await _auditRepository.CreateAudit(new AuditRecord
                {
                    Action = AuditAction.Create,
                    MemberKey = memberKey,
                    ActorName = memberName,
                    EntityUri = competition.EntityUri,
                    State = JsonConvert.SerializeObject(competition),
                    AuditDate = DateTime.UtcNow
                }).ConfigureAwait(false);
            }
            catch (SqlException ex)
            {
                _logger.Error(typeof(SqlServerCompetitionRepository), ex);
            }

            return competition;
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

            try
            {
                string routeBeforeUpdate = competition.CompetitionRoute;
                competition.Introduction = _htmlSanitiser.Sanitize(competition.Introduction);
                competition.PublicContactDetails = _htmlSanitiser.Sanitize(competition.PublicContactDetails);
                competition.PrivateContactDetails = _htmlSanitiser.Sanitize(competition.PrivateContactDetails);
                competition.Facebook = PrefixUrlProtocol(competition.Facebook);
                competition.Twitter = PrefixAtSign(competition.Twitter);
                competition.Instagram = PrefixAtSign(competition.Instagram);
                competition.YouTube = PrefixUrlProtocol(competition.YouTube);
                competition.Website = PrefixUrlProtocol(competition.Website);

                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {

                        competition.CompetitionRoute = _routeGenerator.GenerateRoute("/competitions", competition.CompetitionName, NoiseWords.CompetitionRoute);
                        if (competition.CompetitionRoute != routeBeforeUpdate)
                        {
                            int count;
                            do
                            {
                                count = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Competition} WHERE CompetitionRoute = @CompetitionRoute", new { competition.CompetitionRoute }, transaction).ConfigureAwait(false);
                                if (count > 0)
                                {
                                    competition.CompetitionRoute = _routeGenerator.IncrementRoute(competition.CompetitionRoute);
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
                                competition.CompetitionName,
                                competition.FromYear,
                                competition.UntilYear,
                                competition.PlayerType,
                                competition.Introduction,
                                competition.PublicContactDetails,
                                competition.PrivateContactDetails,
                                competition.Facebook,
                                competition.Twitter,
                                competition.Instagram,
                                competition.YouTube,
                                competition.Website,
                                competition.CompetitionRoute,
                                competition.CompetitionId
                            }, transaction).ConfigureAwait(false);

                        if (routeBeforeUpdate != competition.CompetitionRoute)
                        {
                            // Update the season routes to match the amended competition route
                            await connection.ExecuteAsync($@"UPDATE {Tables.Season} 
                                SET SeasonRoute = CONCAT(@CompetitionRoute, SUBSTRING(SeasonRoute, {routeBeforeUpdate.Length + 1}, LEN(SeasonRoute)-{routeBeforeUpdate.Length})) 
                                WHERE CompetitionId = @CompetitionId", new { competition.CompetitionId, competition.CompetitionRoute }, transaction).ConfigureAwait(false);
                        }

                        transaction.Commit();
                    }

                    if (routeBeforeUpdate != competition.CompetitionRoute)
                    {
                        await _redirectsRepository.InsertRedirect(routeBeforeUpdate, competition.CompetitionRoute, null).ConfigureAwait(false);
                    }
                }

                await _auditRepository.CreateAudit(new AuditRecord
                {
                    Action = AuditAction.Update,
                    MemberKey = memberKey,
                    ActorName = memberName,
                    EntityUri = competition.EntityUri,
                    State = JsonConvert.SerializeObject(competition),
                    AuditDate = DateTime.UtcNow
                }).ConfigureAwait(false);

            }
            catch (SqlException ex)
            {
                _logger.Error(typeof(SqlServerCompetitionRepository), ex);
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

            try
            {
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
                        transaction.Commit();
                    }
                }

                await _redirectsRepository.DeleteRedirectsByDestinationPrefix(competition.CompetitionRoute).ConfigureAwait(false);

                await _auditRepository.CreateAudit(new AuditRecord
                {
                    Action = AuditAction.Delete,
                    MemberKey = memberKey,
                    ActorName = memberName,
                    EntityUri = competition.EntityUri,
                    State = JsonConvert.SerializeObject(competition),
                    AuditDate = DateTime.UtcNow
                }).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.Error<SqlServerCompetitionRepository>(e);
                throw;
            }
        }

    }
}
