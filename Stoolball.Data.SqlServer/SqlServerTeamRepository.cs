using System;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using Newtonsoft.Json;
using Stoolball.Html;
using Stoolball.Logging;
using Stoolball.Routing;
using Stoolball.Security;
using Stoolball.Teams;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// Writes stoolball team data to the Umbraco database
    /// </summary>
    public class SqlServerTeamRepository : ITeamRepository
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger _logger;
        private readonly IRouteGenerator _routeGenerator;
        private readonly IRedirectsRepository _redirectsRepository;
        private readonly IMemberGroupHelper _memberGroupHelper;
        private readonly IHtmlSanitizer _htmlSanitiser;
        private readonly IStoolballEntityCopier _copier;
        private readonly IUrlFormatter _urlFormatter;
        private readonly ISocialMediaAccountFormatter _socialMediaAccountFormatter;

        public SqlServerTeamRepository(IDatabaseConnectionFactory databaseConnectionFactory, IAuditRepository auditRepository, ILogger logger, IRouteGenerator routeGenerator,
            IRedirectsRepository redirectsRepository, IMemberGroupHelper memberGroupHelper, IHtmlSanitizer htmlSanitiser, IStoolballEntityCopier copier,
            IUrlFormatter urlFormatter, ISocialMediaAccountFormatter socialMediaAccountFormatter)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
            _memberGroupHelper = memberGroupHelper ?? throw new ArgumentNullException(nameof(memberGroupHelper));
            _htmlSanitiser = htmlSanitiser ?? throw new ArgumentNullException(nameof(htmlSanitiser));
            _copier = copier ?? throw new ArgumentNullException(nameof(copier));
            _urlFormatter = urlFormatter ?? throw new ArgumentNullException(nameof(urlFormatter));
            _socialMediaAccountFormatter = socialMediaAccountFormatter ?? throw new ArgumentNullException(nameof(socialMediaAccountFormatter));
        }

        /// <summary>
        /// Creates a stoolball team and populates the <see cref="Team.TeamId"/>
        /// </summary>
        /// <returns>The created team</returns>
        public async Task<Team> CreateTeam(Team team, Guid memberKey, string memberUsername, string memberName)
        {
            if (team is null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var auditableTeam = await CreateTeam(team, transaction, memberUsername).ConfigureAwait(false);

                    var redacted = _copier.CreateRedactedCopy(auditableTeam);
                    await _auditRepository.CreateAudit(new AuditRecord
                    {
                        Action = AuditAction.Create,
                        MemberKey = memberKey,
                        ActorName = memberName,
                        EntityUri = auditableTeam.EntityUri,
                        State = JsonConvert.SerializeObject(auditableTeam),
                        RedactedState = JsonConvert.SerializeObject(redacted),
                        AuditDate = DateTime.UtcNow
                    }, transaction).ConfigureAwait(false);

                    transaction.Commit();

                    _logger.Info(GetType(), LoggingTemplates.Created, redacted, memberName, memberKey, GetType(), nameof(SqlServerTeamRepository.CreateTeam));

                    return auditableTeam;
                }
            }
        }

        /// <summary>
        /// Creates a team using an existing transaction
        /// </summary>
        public async Task<Team> CreateTeam(Team team, IDbTransaction transaction, string memberUsername)
        {
            if (team is null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            if (transaction is null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            if (string.IsNullOrWhiteSpace(memberUsername))
            {
                throw new ArgumentNullException(nameof(memberUsername));
            }

            var auditableTeam = _copier.CreateAuditableCopy(team);

            auditableTeam.TeamId = Guid.NewGuid();
            auditableTeam.Introduction = _htmlSanitiser.Sanitize(auditableTeam.Introduction);
            auditableTeam.PlayingTimes = _htmlSanitiser.Sanitize(auditableTeam.PlayingTimes);
            auditableTeam.Cost = _htmlSanitiser.Sanitize(auditableTeam.Cost);
            auditableTeam.PublicContactDetails = _htmlSanitiser.Sanitize(auditableTeam.PublicContactDetails);
            auditableTeam.PrivateContactDetails = _htmlSanitiser.Sanitize(auditableTeam.PrivateContactDetails);
            auditableTeam.Facebook = _urlFormatter.PrefixHttpsProtocol(auditableTeam.Facebook)?.ToString();
            auditableTeam.Twitter = _socialMediaAccountFormatter.PrefixAtSign(auditableTeam.Twitter);
            auditableTeam.Instagram = _socialMediaAccountFormatter.PrefixAtSign(auditableTeam.Instagram);
            auditableTeam.YouTube = _urlFormatter.PrefixHttpsProtocol(auditableTeam.YouTube)?.ToString();
            auditableTeam.Website = _urlFormatter.PrefixHttpsProtocol(auditableTeam.Website)?.ToString();

            // Create a route. Generally {team.teamRoute} will be blank, but allowing a pre-populated prefix is useful for transient teams
            auditableTeam.TeamRoute = await _routeGenerator.GenerateUniqueRoute(
                $"{auditableTeam.TeamRoute}/teams", auditableTeam.TeamName, NoiseWords.TeamRoute,
                async route => await transaction.Connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Team} WHERE TeamRoute = @TeamRoute", new { auditableTeam.TeamRoute }, transaction).ConfigureAwait(false)
            ).ConfigureAwait(false);

            // Create an owner group
            var group = _memberGroupHelper.CreateOrFindGroup("team", auditableTeam.TeamName, NoiseWords.TeamRoute);
            auditableTeam.MemberGroupKey = group.Key;
            auditableTeam.MemberGroupName = group.Name;

            // Assign the member to the group unless they're already admin
            if (!_memberGroupHelper.MemberIsAdministrator(memberUsername))
            {
                _memberGroupHelper.AssignRole(memberUsername, group.Name);
            }

            await transaction.Connection.ExecuteAsync(
                $@"INSERT INTO {Tables.Team} (TeamId, TeamType, AgeRangeLower, AgeRangeUpper, PlayerType, Introduction, PlayingTimes, Cost, ClubMark,
                                PublicContactDetails, PrivateContactDetails, Facebook, Twitter, Instagram, YouTube, Website, TeamRoute, MemberGroupKey, MemberGroupName) 
                                VALUES (@TeamId, @TeamType, @AgeRangeLower, @AgeRangeUpper, @PlayerType, @Introduction, @PlayingTimes, @Cost, @ClubMark,
                                @PublicContactDetails, @PrivateContactDetails, @Facebook, @Twitter, @Instagram, @YouTube, @Website, @TeamRoute, @MemberGroupKey, @MemberGroupName)",
                new
                {
                    auditableTeam.TeamId,
                    TeamType = auditableTeam.TeamType.ToString(),
                    auditableTeam.AgeRangeLower,
                    auditableTeam.AgeRangeUpper,
                    PlayerType = auditableTeam.PlayerType.ToString(),
                    auditableTeam.Introduction,
                    auditableTeam.PlayingTimes,
                    auditableTeam.Cost,
                    auditableTeam.ClubMark,
                    auditableTeam.PublicContactDetails,
                    auditableTeam.PrivateContactDetails,
                    auditableTeam.Facebook,
                    auditableTeam.Twitter,
                    auditableTeam.Instagram,
                    auditableTeam.YouTube,
                    auditableTeam.Website,
                    auditableTeam.TeamRoute,
                    auditableTeam.MemberGroupKey,
                    auditableTeam.MemberGroupName
                }, transaction).ConfigureAwait(false);

            await transaction.Connection.ExecuteAsync($@"INSERT INTO {Tables.TeamVersion} 
                                (TeamVersionId, TeamId, TeamName, ComparableName, FromDate, UntilDate) VALUES (@TeamVersionId, @TeamId, @TeamName, @ComparableName, @FromDate, @UntilDate)",
                new
                {
                    TeamVersionId = Guid.NewGuid(),
                    auditableTeam.TeamId,
                    auditableTeam.TeamName,
                    ComparableName = auditableTeam.ComparableName(),
                    FromDate = DateTime.UtcNow.Date,
                    UntilDate = auditableTeam.UntilYear.HasValue ? new DateTime(auditableTeam.UntilYear.Value, 12, 31).ToUniversalTime() : (DateTime?)null
                }, transaction).ConfigureAwait(false);

            foreach (var location in auditableTeam.MatchLocations)
            {
                await transaction.Connection.ExecuteAsync($@"INSERT INTO {Tables.TeamMatchLocation} (TeamMatchLocationId, TeamId, MatchLocationId, FromDate)
                                VALUES (@TeamMatchLocationId, @TeamId, @MatchLocationId, @FromDate)",
                    new
                    {
                        TeamMatchLocationId = Guid.NewGuid(),
                        auditableTeam.TeamId,
                        location.MatchLocationId,
                        FromDate = DateTime.UtcNow.Date
                    }, transaction).ConfigureAwait(false);
            }

            return auditableTeam;
        }


        /// <summary>
        /// Updates a stoolball team
        /// </summary>
        public async Task<Team> UpdateTeam(Team team, Guid memberKey, string memberName)
        {
            if (team is null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            var auditableTeam = _copier.CreateAuditableCopy(team);
            auditableTeam.Introduction = _htmlSanitiser.Sanitize(auditableTeam.Introduction);
            auditableTeam.PlayingTimes = _htmlSanitiser.Sanitize(auditableTeam.PlayingTimes);
            auditableTeam.Cost = _htmlSanitiser.Sanitize(auditableTeam.Cost);
            auditableTeam.PublicContactDetails = _htmlSanitiser.Sanitize(auditableTeam.PublicContactDetails);
            auditableTeam.PrivateContactDetails = _htmlSanitiser.Sanitize(auditableTeam.PrivateContactDetails);
            auditableTeam.Facebook = _urlFormatter.PrefixHttpsProtocol(auditableTeam.Facebook)?.ToString();
            auditableTeam.Twitter = _socialMediaAccountFormatter.PrefixAtSign(auditableTeam.Twitter);
            auditableTeam.Instagram = _socialMediaAccountFormatter.PrefixAtSign(auditableTeam.Instagram);
            auditableTeam.YouTube = _urlFormatter.PrefixHttpsProtocol(auditableTeam.YouTube)?.ToString();
            auditableTeam.Website = _urlFormatter.PrefixHttpsProtocol(auditableTeam.Website)?.ToString();

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    auditableTeam.TeamRoute = await _routeGenerator.GenerateUniqueRoute(
                        team.TeamRoute,
                        "/teams", auditableTeam.TeamName, NoiseWords.TeamRoute,
                        async route => await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Team} WHERE TeamRoute = @TeamRoute", new { auditableTeam.TeamRoute }, transaction).ConfigureAwait(false)
                    ).ConfigureAwait(false);

                    await connection.ExecuteAsync(
                        $@"UPDATE {Tables.Team} SET
                                TeamType = @TeamType, 
                                AgeRangeLower = @AgeRangeLower, 
                                AgeRangeUpper = @AgeRangeUpper, 
                                PlayerType = @PlayerType, 
                                Introduction = @Introduction, 
                                PlayingTimes = @PlayingTimes, 
                                Cost = @Cost, 
                                PublicContactDetails = @PublicContactDetails, 
                                PrivateContactDetails = @PrivateContactDetails,
                                Facebook = @Facebook,
                                Twitter = @Twitter,
                                Instagram = @Instagram,
                                YouTube = @YouTube,
                                Website = @Website,
                                TeamRoute = @TeamRoute
						        WHERE TeamId = @TeamId",
                        new
                        {
                            TeamType = auditableTeam.TeamType.ToString(),
                            auditableTeam.AgeRangeLower,
                            auditableTeam.AgeRangeUpper,
                            PlayerType = auditableTeam.PlayerType.ToString(),
                            auditableTeam.Introduction,
                            auditableTeam.PlayingTimes,
                            auditableTeam.Cost,
                            auditableTeam.PublicContactDetails,
                            auditableTeam.PrivateContactDetails,
                            auditableTeam.Facebook,
                            auditableTeam.Twitter,
                            auditableTeam.Instagram,
                            auditableTeam.YouTube,
                            auditableTeam.Website,
                            auditableTeam.TeamRoute,
                            auditableTeam.TeamId
                        }, transaction).ConfigureAwait(false);

                    await connection.ExecuteAsync($"UPDATE {Tables.TeamVersion} SET TeamName = @TeamName, ComparableName = @ComparableName, UntilDate = @UntilDate WHERE TeamId = @TeamId",
                        new
                        {
                            auditableTeam.TeamName,
                            ComparableName = auditableTeam.ComparableName(),
                            UntilDate = auditableTeam.UntilYear.HasValue ? new DateTime(auditableTeam.UntilYear.Value, 12, 31).ToUniversalTime() : (DateTime?)null,
                            auditableTeam.TeamId
                        },
                        transaction).ConfigureAwait(false);

                    await connection.ExecuteAsync($"UPDATE {Tables.TeamMatchLocation} SET UntilDate = @UntilDate WHERE TeamId = @TeamId AND UntilDate IS NULL AND MatchLocationId NOT IN @MatchLocationIds", new { UntilDate = DateTime.UtcNow.Date.AddDays(1), auditableTeam.TeamId, MatchLocationIds = auditableTeam.MatchLocations.Select(x => x.MatchLocationId) }, transaction).ConfigureAwait(false);
                    var currentLocations = (await connection.QueryAsync<Guid>($"SELECT MatchLocationId FROM {Tables.TeamMatchLocation} tml WHERE TeamId = @TeamId AND tml.UntilDate IS NULL", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false)).ToList();
                    foreach (var location in auditableTeam.MatchLocations)
                    {
                        if (!currentLocations.Contains(location.MatchLocationId.Value))
                        {
                            await connection.ExecuteAsync($@"INSERT INTO {Tables.TeamMatchLocation} 
                                    (TeamMatchLocationId, TeamId, MatchLocationId, FromDate)
                                    VALUES 
                                    (@TeamMatchLocationId, @TeamId, @MatchLocationId, @FromDate)",
                                new
                                {
                                    TeamMatchLocationId = Guid.NewGuid(),
                                    auditableTeam.TeamId,
                                    location.MatchLocationId,
                                    FromDate = DateTime.UtcNow.Date
                                },
                                transaction).ConfigureAwait(false);
                        }
                    }

                    if (team.TeamRoute != auditableTeam.TeamRoute)
                    {
                        await _redirectsRepository.InsertRedirect(team.TeamRoute, auditableTeam.TeamRoute, null, transaction).ConfigureAwait(false);
                    }

                    var redacted = _copier.CreateRedactedCopy(auditableTeam);
                    await _auditRepository.CreateAudit(new AuditRecord
                    {
                        Action = AuditAction.Update,
                        MemberKey = memberKey,
                        ActorName = memberName,
                        EntityUri = auditableTeam.EntityUri,
                        State = JsonConvert.SerializeObject(auditableTeam),
                        RedactedState = JsonConvert.SerializeObject(redacted),
                        AuditDate = DateTime.UtcNow
                    }, transaction).ConfigureAwait(false);

                    transaction.Commit();

                    _logger.Info(GetType(), LoggingTemplates.Updated, redacted, memberName, memberKey, GetType(), nameof(SqlServerTeamRepository.UpdateTeam));
                }
            }

            return auditableTeam;
        }

        /// <summary>
        /// Updates a stoolball team
        /// </summary>
        public async Task<Team> UpdateTransientTeam(Team team, Guid memberKey, string memberName)
        {
            if (team is null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            var auditableTeam = _copier.CreateAuditableCopy(team);
            auditableTeam.Introduction = _htmlSanitiser.Sanitize(auditableTeam.Introduction);
            auditableTeam.Cost = _htmlSanitiser.Sanitize(auditableTeam.Cost);
            auditableTeam.PublicContactDetails = _htmlSanitiser.Sanitize(auditableTeam.PublicContactDetails);
            auditableTeam.PrivateContactDetails = _htmlSanitiser.Sanitize(auditableTeam.PrivateContactDetails);
            auditableTeam.Facebook = _urlFormatter.PrefixHttpsProtocol(auditableTeam.Facebook)?.ToString();
            auditableTeam.Twitter = _socialMediaAccountFormatter.PrefixAtSign(auditableTeam.Twitter);
            auditableTeam.Instagram = _socialMediaAccountFormatter.PrefixAtSign(auditableTeam.Instagram);
            auditableTeam.YouTube = _urlFormatter.PrefixHttpsProtocol(auditableTeam.YouTube)?.ToString();
            auditableTeam.Website = _urlFormatter.PrefixHttpsProtocol(auditableTeam.Website)?.ToString();

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var routePrefix = Regex.Match(auditableTeam.TeamRoute, @"^\/tournaments\/[a-z0-9-]+\/teams").Value;
                    auditableTeam.TeamRoute = await _routeGenerator.GenerateUniqueRoute(
                        team.TeamRoute,
                        routePrefix, auditableTeam.TeamName, NoiseWords.TeamRoute,
                        async route => await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Team} WHERE TeamRoute = @TeamRoute", new { auditableTeam.TeamRoute }, transaction).ConfigureAwait(false)
                    ).ConfigureAwait(false);

                    await connection.ExecuteAsync(
                        $@"UPDATE {Tables.Team} SET
                                 AgeRangeLower = @AgeRangeLower, 
                                 AgeRangeUpper = @AgeRangeUpper, 
                                 PlayerType = @PlayerType, 
                                 Introduction = @Introduction, 
                                 Cost = @Cost, 
                                 PublicContactDetails = @PublicContactDetails, 
                                 PrivateContactDetails = @PrivateContactDetails,
                                 Facebook = @Facebook,
                                 Twitter = @Twitter,
                                 Instagram = @Instagram,
                                 YouTube = @YouTube,
                                 Website = @Website,
                                 TeamRoute = @TeamRoute
                                 WHERE TeamId = @TeamId",
                        new
                        {
                            auditableTeam.AgeRangeLower,
                            auditableTeam.AgeRangeUpper,
                            PlayerType = auditableTeam.PlayerType.ToString(),
                            auditableTeam.Introduction,
                            auditableTeam.Cost,
                            auditableTeam.PublicContactDetails,
                            auditableTeam.PrivateContactDetails,
                            auditableTeam.Facebook,
                            auditableTeam.Twitter,
                            auditableTeam.Instagram,
                            auditableTeam.YouTube,
                            auditableTeam.Website,
                            auditableTeam.TeamRoute,
                            auditableTeam.TeamId
                        }, transaction).ConfigureAwait(false);

                    // No need to support changes of name when the team only lasts for a day
                    await connection.ExecuteAsync($"UPDATE {Tables.TeamVersion} SET TeamName = @TeamName WHERE TeamId = @TeamId", new { auditableTeam.TeamId, auditableTeam.TeamName }, transaction).ConfigureAwait(false);

                    if (team.TeamRoute != auditableTeam.TeamRoute)
                    {
                        await _redirectsRepository.InsertRedirect(team.TeamRoute, auditableTeam.TeamRoute, null, transaction).ConfigureAwait(false);
                    }

                    var redacted = _copier.CreateRedactedCopy(auditableTeam);
                    await _auditRepository.CreateAudit(new AuditRecord
                    {
                        Action = AuditAction.Update,
                        MemberKey = memberKey,
                        ActorName = memberName,
                        EntityUri = auditableTeam.EntityUri,
                        State = JsonConvert.SerializeObject(auditableTeam),
                        RedactedState = JsonConvert.SerializeObject(redacted),
                        AuditDate = DateTime.UtcNow
                    }, transaction).ConfigureAwait(false);

                    transaction.Commit();

                    _logger.Info(GetType(), LoggingTemplates.Updated, redacted, memberName, memberKey, GetType(), nameof(SqlServerTeamRepository.UpdateTransientTeam));
                }
            }

            return auditableTeam;
        }


        /// <summary>
        /// Deletes a stoolball team
        /// </summary>
        public async Task DeleteTeam(Team team, Guid memberKey, string memberName)
        {
            if (team is null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            var auditableTeam = _copier.CreateAuditableCopy(team);

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    await connection.ExecuteAsync($"DELETE FROM {Tables.PlayerInMatchStatistics} WHERE TeamId = @TeamId OR OppositionTeamId = @TeamId", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"UPDATE {Tables.PlayerInMatchStatistics} SET BowledByPlayerIdentityId = NULL WHERE BowledByPlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId)", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"UPDATE {Tables.PlayerInMatchStatistics} SET CaughtByPlayerIdentityId = NULL WHERE CaughtByPlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId)", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"UPDATE {Tables.PlayerInMatchStatistics} SET RunOutByPlayerIdentityId = NULL WHERE RunOutByPlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId)", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"UPDATE {Tables.PlayerInnings} SET DismissedByPlayerIdentityId = NULL WHERE DismissedByPlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId)", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"UPDATE {Tables.PlayerInnings} SET BowlerPlayerIdentityId = NULL WHERE BowlerPlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId)", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.PlayerInnings} WHERE BatterPlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId)", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.Over} WHERE BowlerPlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId)", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.BowlingFigures} WHERE BowlerPlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId)", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.AwardedTo} WHERE PlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId)", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    var playerIds = await connection.QueryAsync<Guid>($"SELECT PlayerId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.Player} WHERE PlayerId IN @playerIds AND PlayerId NOT IN (SELECT PlayerId FROM {Tables.PlayerIdentity} WHERE PlayerId IN @playerIds)", new { playerIds }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"UPDATE {Tables.MatchInnings} SET BattingMatchTeamId = NULL WHERE BattingMatchTeamId IN (SELECT MatchTeamId FROM {Tables.MatchTeam} WHERE TeamId = @TeamId)", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"UPDATE {Tables.MatchInnings} SET BowlingMatchTeamId = NULL WHERE BowlingMatchTeamId IN (SELECT MatchTeamId FROM {Tables.MatchTeam} WHERE TeamId = @TeamId)", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.MatchTeam} WHERE TeamId = @TeamId", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.TournamentTeam} WHERE TeamId = @TeamId", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.PointsAdjustment} WHERE TeamId = @TeamId", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.SeasonTeam} WHERE TeamId = @TeamId", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.TeamMatchLocation} WHERE TeamId = @TeamId", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.TeamVersion} WHERE TeamId = @TeamId", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.Team} WHERE TeamId = @TeamId", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);

                    await _redirectsRepository.DeleteRedirectsByDestinationPrefix(auditableTeam.TeamRoute, transaction).ConfigureAwait(false);

                    var redacted = _copier.CreateRedactedCopy(auditableTeam);
                    await _auditRepository.CreateAudit(new AuditRecord
                    {
                        Action = AuditAction.Delete,
                        MemberKey = memberKey,
                        ActorName = memberName,
                        EntityUri = auditableTeam.EntityUri,
                        State = JsonConvert.SerializeObject(auditableTeam),
                        RedactedState = JsonConvert.SerializeObject(redacted),
                        AuditDate = DateTime.UtcNow
                    }, transaction).ConfigureAwait(false);

                    transaction.Commit();

                    _logger.Info(GetType(), LoggingTemplates.Deleted, redacted, memberName, memberKey, GetType(), nameof(SqlServerTeamRepository.DeleteTeam));
                }
            }

        }
    }
}
