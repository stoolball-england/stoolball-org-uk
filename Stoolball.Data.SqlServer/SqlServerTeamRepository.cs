﻿using System;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using Ganss.XSS;
using Newtonsoft.Json;
using Stoolball.Logging;
using Stoolball.MatchLocations;
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
        private readonly IDataRedactor _dataRedactor;

        public SqlServerTeamRepository(IDatabaseConnectionFactory databaseConnectionFactory, IAuditRepository auditRepository, ILogger logger, IRouteGenerator routeGenerator,
            IRedirectsRepository redirectsRepository, IMemberGroupHelper memberGroupHelper, IHtmlSanitizer htmlSanitiser, IDataRedactor dataRedactor)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
            _memberGroupHelper = memberGroupHelper ?? throw new ArgumentNullException(nameof(memberGroupHelper));
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

        private static Team CreateAuditableCopy(Team team)
        {
            return new Team
            {
                TeamId = team.TeamId,
                TeamName = team.TeamName,
                TeamType = team.TeamType,
                AgeRangeLower = team.AgeRangeLower,
                AgeRangeUpper = team.AgeRangeUpper,
                FromYear = team.FromYear,
                UntilYear = team.UntilYear,
                PlayerType = team.PlayerType,
                Introduction = team.Introduction,
                PlayingTimes = team.PlayingTimes,
                Cost = team.Cost,
                MatchLocations = team.MatchLocations.Select(x => new MatchLocation { MatchLocationId = x.MatchLocationId }).ToList(),
                PublicContactDetails = team.PublicContactDetails,
                PrivateContactDetails = team.PrivateContactDetails,
                Facebook = team.Facebook,
                Twitter = team.Twitter,
                Instagram = team.Instagram,
                YouTube = team.YouTube,
                Website = team.Website,
                TeamRoute = team.TeamRoute,
                MemberGroupKey = team.MemberGroupKey,
                MemberGroupName = team.MemberGroupName
            };
        }

        private Team CreateRedactedCopy(Team team)
        {
            var redacted = CreateAuditableCopy(team);
            redacted.Introduction = _dataRedactor.RedactPersonalData(team.Introduction);
            redacted.PlayingTimes = _dataRedactor.RedactPersonalData(team.PlayingTimes);
            redacted.Cost = _dataRedactor.RedactPersonalData(team.Cost);
            redacted.PublicContactDetails = _dataRedactor.RedactAll(team.PublicContactDetails);
            redacted.PrivateContactDetails = _dataRedactor.RedactAll(team.PrivateContactDetails);
            return redacted;
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

                    var redacted = CreateRedactedCopy(auditableTeam);
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
                }
            }


            return team;
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
                throw new ArgumentException($"'{nameof(memberUsername)}' cannot be null or whitespace", nameof(memberUsername));
            }

            var auditableTeam = CreateAuditableCopy(team);

            auditableTeam.TeamId = Guid.NewGuid();
            auditableTeam.Introduction = _htmlSanitiser.Sanitize(auditableTeam.Introduction);
            auditableTeam.PlayingTimes = _htmlSanitiser.Sanitize(auditableTeam.PlayingTimes);
            auditableTeam.Cost = _htmlSanitiser.Sanitize(auditableTeam.Cost);
            auditableTeam.PublicContactDetails = _htmlSanitiser.Sanitize(auditableTeam.PublicContactDetails);
            auditableTeam.PrivateContactDetails = _htmlSanitiser.Sanitize(auditableTeam.PrivateContactDetails);
            auditableTeam.Facebook = PrefixUrlProtocol(auditableTeam.Facebook);
            auditableTeam.Twitter = PrefixAtSign(auditableTeam.Twitter);
            auditableTeam.Instagram = PrefixAtSign(auditableTeam.Instagram);
            auditableTeam.YouTube = PrefixUrlProtocol(auditableTeam.YouTube);
            auditableTeam.Website = PrefixUrlProtocol(auditableTeam.Website);

            // Create a route. Generally {team.teamRoute} will be blank, but allowing a pre-populated prefix is useful for transient teams
            auditableTeam.TeamRoute = _routeGenerator.GenerateRoute($"{auditableTeam.TeamRoute}/teams", auditableTeam.TeamName, NoiseWords.TeamRoute);
            int count;
            do
            {
                count = await transaction.Connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Team} WHERE TeamRoute = @TeamRoute", new { auditableTeam.TeamRoute }, transaction).ConfigureAwait(false);
                if (count > 0)
                {
                    auditableTeam.TeamRoute = _routeGenerator.IncrementRoute(auditableTeam.TeamRoute);
                }
            }
            while (count > 0);

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
                $@"INSERT INTO {Tables.Team} (TeamId, TeamType, AgeRangeLower, AgeRangeUpper, FromYear, UntilYear, PlayerType, Introduction, 
                                PlayingTimes, Cost, PublicContactDetails, PrivateContactDetails, Facebook, Twitter, Instagram, YouTube, Website, TeamRoute, MemberGroupKey, MemberGroupName) 
                                VALUES (@TeamId, @TeamType, @AgeRangeLower, @AgeRangeUpper, @FromYear, @UntilYear, @PlayerType, @Introduction, @PlayingTimes, @Cost, 
                                @PublicContactDetails, @PrivateContactDetails, @Facebook, @Twitter, @Instagram, @YouTube, @Website, @TeamRoute, @MemberGroupKey, @MemberGroupName)",
                new
                {
                    auditableTeam.TeamId,
                    TeamType = auditableTeam.TeamType.ToString(),
                    auditableTeam.AgeRangeLower,
                    auditableTeam.AgeRangeUpper,
                    auditableTeam.FromYear,
                    auditableTeam.UntilYear,
                    auditableTeam.PlayerType,
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
                    auditableTeam.MemberGroupKey,
                    auditableTeam.MemberGroupName
                }, transaction).ConfigureAwait(false);

            await transaction.Connection.ExecuteAsync($@"INSERT INTO {Tables.TeamName} 
                                (TeamNameId, TeamId, TeamName, TeamComparableName, FromDate) VALUES (@TeamNameId, @TeamId, @TeamName, @TeamComparableName, GETUTCDATE())",
                new
                {
                    TeamNameId = Guid.NewGuid(),
                    auditableTeam.TeamId,
                    auditableTeam.TeamName,
                    TeamComparableName = auditableTeam.ComparableName()
                }, transaction).ConfigureAwait(false);

            foreach (var location in auditableTeam.MatchLocations)
            {
                await transaction.Connection.ExecuteAsync($@"INSERT INTO {Tables.TeamMatchLocation} (TeamMatchLocationId, TeamId, MatchLocationId)
                                VALUES (@TeamMatchLocationId, @TeamId, @MatchLocationId)",
                    new
                    {
                        TeamMatchLocationId = Guid.NewGuid(),
                        auditableTeam.TeamId,
                        location.MatchLocationId
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

            var auditableTeam = CreateAuditableCopy(team);
            auditableTeam.Introduction = _htmlSanitiser.Sanitize(auditableTeam.Introduction);
            auditableTeam.PlayingTimes = _htmlSanitiser.Sanitize(auditableTeam.PlayingTimes);
            auditableTeam.Cost = _htmlSanitiser.Sanitize(auditableTeam.Cost);
            auditableTeam.PublicContactDetails = _htmlSanitiser.Sanitize(auditableTeam.PublicContactDetails);
            auditableTeam.PrivateContactDetails = _htmlSanitiser.Sanitize(auditableTeam.PrivateContactDetails);
            auditableTeam.Facebook = PrefixUrlProtocol(auditableTeam.Facebook);
            auditableTeam.Twitter = PrefixAtSign(auditableTeam.Twitter);
            auditableTeam.Instagram = PrefixAtSign(auditableTeam.Instagram);
            auditableTeam.YouTube = PrefixUrlProtocol(auditableTeam.YouTube);
            auditableTeam.Website = PrefixUrlProtocol(auditableTeam.Website);

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {

                    auditableTeam.TeamRoute = _routeGenerator.GenerateRoute("/teams", auditableTeam.TeamName, NoiseWords.TeamRoute);
                    if (auditableTeam.TeamRoute != team.TeamRoute)
                    {
                        int count;
                        do
                        {
                            count = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Team} WHERE TeamRoute = @TeamRoute", new { auditableTeam.TeamRoute }, transaction).ConfigureAwait(false);
                            if (count > 0)
                            {
                                auditableTeam.TeamRoute = _routeGenerator.IncrementRoute(auditableTeam.TeamRoute);
                            }
                        }
                        while (count > 0);
                    }

                    await connection.ExecuteAsync(
                        $@"UPDATE {Tables.Team} SET
                                TeamType = @TeamType, 
                                AgeRangeLower = @AgeRangeLower, 
                                AgeRangeUpper = @AgeRangeUpper, 
                                FromYear = @FromYear, 
                                UntilYear = @UntilYear, 
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
                            auditableTeam.FromYear,
                            auditableTeam.UntilYear,
                            auditableTeam.PlayerType,
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

                    var currentName = await connection.ExecuteScalarAsync<string>($"SELECT TeamName FROM {Tables.TeamName} WHERE TeamId = @TeamId AND UntilDate IS NULL", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    if (auditableTeam.TeamName?.Trim() != currentName?.Trim())
                    {
                        await connection.ExecuteAsync($"UPDATE {Tables.TeamName} SET UntilDate = GETUTCDATE() WHERE TeamId = @TeamId AND UntilDate IS NULL", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($@"INSERT INTO {Tables.TeamName} 
                                (TeamNameId, TeamId, TeamName, FromDate) VALUES (@TeamNameId, @TeamId, @TeamName, GETUTCDATE())",
                                                        new
                                                        {
                                                            TeamNameId = Guid.NewGuid(),
                                                            auditableTeam.TeamId,
                                                            auditableTeam.TeamName
                                                        }, transaction).ConfigureAwait(false);
                    }

                    await connection.ExecuteAsync($"DELETE FROM {Tables.TeamMatchLocation} WHERE TeamId = @TeamId AND MatchLocationId NOT IN @MatchLocationIds", new { auditableTeam.TeamId, MatchLocationIds = auditableTeam.MatchLocations.Select(x => x.MatchLocationId) }, transaction).ConfigureAwait(false);
                    var currentLocations = (await connection.QueryAsync<Guid>($"SELECT MatchLocationId FROM {Tables.TeamMatchLocation} WHERE TeamId = @TeamId", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false)).ToList();
                    foreach (var location in auditableTeam.MatchLocations)
                    {
                        if (!currentLocations.Contains(location.MatchLocationId.Value))
                        {
                            await connection.ExecuteAsync($@"INSERT INTO {Tables.TeamMatchLocation} 
                                    (TeamMatchLocationId, TeamId, MatchLocationId)
                                    VALUES 
                                    (@TeamMatchLocationId, @TeamId, @MatchLocationId)",
                                new
                                {
                                    TeamMatchLocationId = Guid.NewGuid(),
                                    auditableTeam.TeamId,
                                    location.MatchLocationId
                                },
                                transaction).ConfigureAwait(false);
                        }
                    }

                    if (team.TeamRoute != auditableTeam.TeamRoute)
                    {
                        await _redirectsRepository.InsertRedirect(team.TeamRoute, auditableTeam.TeamRoute, null, transaction).ConfigureAwait(false);
                    }

                    var redacted = CreateRedactedCopy(auditableTeam);
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

            var auditableTeam = CreateAuditableCopy(team);
            auditableTeam.Introduction = _htmlSanitiser.Sanitize(auditableTeam.Introduction);
            auditableTeam.Cost = _htmlSanitiser.Sanitize(auditableTeam.Cost);
            auditableTeam.PublicContactDetails = _htmlSanitiser.Sanitize(auditableTeam.PublicContactDetails);
            auditableTeam.PrivateContactDetails = _htmlSanitiser.Sanitize(auditableTeam.PrivateContactDetails);
            auditableTeam.Facebook = PrefixUrlProtocol(auditableTeam.Facebook);
            auditableTeam.Twitter = PrefixAtSign(auditableTeam.Twitter);
            auditableTeam.Instagram = PrefixAtSign(auditableTeam.Instagram);
            auditableTeam.YouTube = PrefixUrlProtocol(auditableTeam.YouTube);
            auditableTeam.Website = PrefixUrlProtocol(auditableTeam.Website);

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var routePrefix = Regex.Match(auditableTeam.TeamRoute, @"^\/tournaments\/[a-z0-9-]+\/teams").Value;
                    auditableTeam.TeamRoute = _routeGenerator.GenerateRoute(routePrefix, auditableTeam.TeamName, NoiseWords.TeamRoute);
                    if (auditableTeam.TeamRoute != team.TeamRoute)
                    {
                        int count;
                        do
                        {
                            count = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Team} WHERE TeamRoute = @TeamRoute", new { auditableTeam.TeamRoute }, transaction).ConfigureAwait(false);
                            if (count > 0)
                            {
                                auditableTeam.TeamRoute = _routeGenerator.IncrementRoute(auditableTeam.TeamRoute);
                            }
                        }
                        while (count > 0);
                    }

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
                            auditableTeam.PlayerType,
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
                    await connection.ExecuteAsync($"UPDATE {Tables.TeamName} SET TeamName = @TeamName WHERE TeamId = @TeamId", new { auditableTeam.TeamId, auditableTeam.TeamName }, transaction).ConfigureAwait(false);

                    if (team.TeamRoute != auditableTeam.TeamRoute)
                    {
                        await _redirectsRepository.InsertRedirect(team.TeamRoute, auditableTeam.TeamRoute, null, transaction).ConfigureAwait(false);
                    }

                    var redacted = CreateRedactedCopy(auditableTeam);
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

            var auditableTeam = CreateAuditableCopy(team);

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    await connection.ExecuteAsync($"UPDATE {Tables.StatisticsPlayerMatch} SET OppositionTeamId = NULL, OppositionTeamName = NULL WHERE OppositionTeamId = @TeamId", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.StatisticsPlayerMatch} WHERE TeamId = @TeamId", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"UPDATE {Tables.StatisticsPlayerMatch} SET BowledById = NULL WHERE BowledById IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId)", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"UPDATE {Tables.StatisticsPlayerMatch} SET CaughtById = NULL WHERE CaughtById IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId)", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"UPDATE {Tables.StatisticsPlayerMatch} SET RunOutById = NULL WHERE RunOutById IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId)", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"UPDATE {Tables.PlayerInnings} SET DismissedById = NULL WHERE DismissedById IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId)", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"UPDATE {Tables.PlayerInnings} SET BowlerId = NULL WHERE BowlerId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId)", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.PlayerInnings} WHERE PlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId)", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.Over} WHERE PlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId)", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.Bowling} WHERE PlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId)", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.MatchAward} WHERE PlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId)", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    var playerIds = await connection.QueryAsync<Guid>($"SELECT PlayerId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.Player} WHERE PlayerId IN @playerIds AND NOT IN (SELECT PlayerId FROM {Tables.PlayerIdentity} WHERE PlayerId IN @playerIds)", new { playerIds }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"UPDATE {Tables.MatchInnings} SET BattingMatchTeamId = NULL WHERE BattingMatchTeamId IN (SELECT MatchTeamId FROM {Tables.MatchTeam} WHERE TeamId = @TeamId)", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"UPDATE {Tables.MatchInnings} SET BowlingMatchTeamId = NULL WHERE BowlingMatchTeamId IN (SELECT MatchTeamId FROM {Tables.MatchTeam} WHERE TeamId = @TeamId)", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.MatchTeam} WHERE TeamId = @TeamId", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.TournamentTeam} WHERE TeamId = @TeamId", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.SeasonPointsAdjustment} WHERE TeamId = @TeamId", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.SeasonTeam} WHERE TeamId = @TeamId", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.TeamMatchLocation} WHERE TeamId = @TeamId", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.TeamName} WHERE TeamId = @TeamId", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.Team} WHERE TeamId = @TeamId", new { auditableTeam.TeamId }, transaction).ConfigureAwait(false);

                    await _redirectsRepository.DeleteRedirectsByDestinationPrefix(auditableTeam.TeamRoute, transaction).ConfigureAwait(false);

                    var redacted = CreateRedactedCopy(auditableTeam);
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
    }
}