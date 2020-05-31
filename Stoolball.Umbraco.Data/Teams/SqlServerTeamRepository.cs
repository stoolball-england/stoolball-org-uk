using Dapper;
using Ganss.XSS;
using Newtonsoft.Json;
using Stoolball.Audit;
using Stoolball.Routing;
using Stoolball.Teams;
using Stoolball.Umbraco.Data.Audit;
using Stoolball.Umbraco.Data.Redirects;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Umbraco.Data.Teams
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
        private readonly IHtmlSanitizer _htmlSanitiser;

        public SqlServerTeamRepository(IDatabaseConnectionFactory databaseConnectionFactory, IAuditRepository auditRepository, ILogger logger, IRouteGenerator routeGenerator,
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
        /// Creates a stoolball team and populates the <see cref="Team.TeamId"/>
        /// </summary>
        /// <returns>The created team</returns>
        public async Task<Team> CreateTeam(Team team, Guid memberKey, string memberName)
        {
            if (team is null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            try
            {
                team.TeamId = Guid.NewGuid();
                team.Introduction = _htmlSanitiser.Sanitize(team.Introduction);
                team.PlayingTimes = _htmlSanitiser.Sanitize(team.PlayingTimes);
                team.Cost = _htmlSanitiser.Sanitize(team.Cost);
                team.PublicContactDetails = _htmlSanitiser.Sanitize(team.PublicContactDetails);
                team.PrivateContactDetails = _htmlSanitiser.Sanitize(team.PrivateContactDetails);
                team.Facebook = PrefixUrlProtocol(team.Facebook);
                team.Twitter = PrefixAtSign(team.Twitter);
                team.Instagram = PrefixAtSign(team.Instagram);
                team.YouTube = PrefixUrlProtocol(team.YouTube);
                team.Website = PrefixUrlProtocol(team.Website);

                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        team.TeamRoute = _routeGenerator.GenerateRoute("/teams", team.TeamName, NoiseWords.TeamRoute);
                        int count;
                        do
                        {
                            count = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Team} WHERE TeamRoute = @TeamRoute", new { team.TeamRoute }, transaction).ConfigureAwait(false);
                            if (count > 0)
                            {
                                team.TeamRoute = _routeGenerator.IncrementRoute(team.TeamRoute);
                            }
                        }
                        while (count > 0);

                        await connection.ExecuteAsync(
                            $@"INSERT INTO {Tables.Team} (TeamId, TeamType, AgeRangeLower, AgeRangeUpper, FromYear, UntilYear, PlayerType, Introduction, 
                                PlayingTimes, Cost, PublicContactDetails, PrivateContactDetails, Facebook, Twitter, Instagram, YouTube, Website, TeamRoute, MemberGroupId, MemberGroupName) 
                                VALUES (@TeamId, @TeamType, @AgeRangeLower, @AgeRangeUpper, @FromYear, @UntilYear, @PlayerType, @Introduction, @PlayingTimes, @Cost, 
                                @PublicContactDetails, @PrivateContactDetails, @Facebook, @Twitter, @Instagram, @YouTube, @Website, @TeamRoute, @MemberGroupId, @MemberGroupName)",
                            new
                            {
                                team.TeamId,
                                TeamType = team.TeamType.ToString(),
                                team.AgeRangeLower,
                                team.AgeRangeUpper,
                                team.FromYear,
                                team.UntilYear,
                                team.PlayerType,
                                team.Introduction,
                                team.PlayingTimes,
                                team.Cost,
                                team.PublicContactDetails,
                                team.PrivateContactDetails,
                                team.Facebook,
                                team.Twitter,
                                team.Instagram,
                                team.YouTube,
                                team.Website,
                                team.TeamRoute,
                                team.MemberGroupId,
                                team.MemberGroupName
                            }, transaction).ConfigureAwait(false);

                        await connection.ExecuteAsync($@"INSERT INTO {Tables.TeamName} 
                                (TeamNameId, TeamId, TeamName, TeamComparableName, FromDate) VALUES (@TeamNameId, @TeamId, @TeamName, @TeamComparableName, GETUTCDATE())",
                            new
                            {
                                TeamNameId = Guid.NewGuid(),
                                team.TeamId,
                                team.TeamName,
                                TeamComparableName = team.ComparableName()
                            }, transaction).ConfigureAwait(false);

                        foreach (var location in team.MatchLocations)
                        {
                            await connection.ExecuteAsync($@"INSERT INTO {Tables.TeamMatchLocation} (TeamMatchLocationId, TeamId, MatchLocationId)
                                VALUES (@TeamMatchLocationId, @TeamId, @MatchLocationId)",
                                new
                                {
                                    TeamMatchLocationId = Guid.NewGuid(),
                                    team.TeamId,
                                    location.MatchLocationId
                                }, transaction).ConfigureAwait(false);
                        }

                        team.PlayerIdentities.Add(new PlayerIdentity
                        {
                            PlayerIdentityId = Guid.NewGuid(),
                            PlayerId = Guid.NewGuid(),
                            PlayerIdentityRoute = team.TeamRoute + "/players/no-balls",
                            PlayerRole = PlayerRole.NoBalls,
                            PlayerIdentityName = "No balls",
                            TotalMatches = 0
                        });

                        team.PlayerIdentities.Add(new PlayerIdentity
                        {
                            PlayerIdentityId = Guid.NewGuid(),
                            PlayerId = Guid.NewGuid(),
                            PlayerIdentityRoute = team.TeamRoute + "/players/wides",
                            PlayerRole = PlayerRole.Wides,
                            PlayerIdentityName = "Wides",
                            TotalMatches = 0
                        });

                        team.PlayerIdentities.Add(new PlayerIdentity
                        {
                            PlayerIdentityId = Guid.NewGuid(),
                            PlayerId = Guid.NewGuid(),
                            PlayerIdentityRoute = team.TeamRoute + "/players/byes",
                            PlayerRole = PlayerRole.Byes,
                            PlayerIdentityName = "Byes",
                            TotalMatches = 0
                        });

                        team.PlayerIdentities.Add(new PlayerIdentity
                        {
                            PlayerIdentityId = Guid.NewGuid(),
                            PlayerId = Guid.NewGuid(),
                            PlayerIdentityRoute = team.TeamRoute + "/players/bonus-runs",
                            PlayerRole = PlayerRole.BonusRuns,
                            PlayerIdentityName = "Bonus runs",
                            TotalMatches = 0
                        });

                        foreach (var extrasIdentity in team.PlayerIdentities)
                        {
                            await transaction.Connection.ExecuteAsync(
                                $@"INSERT INTO {Tables.PlayerIdentity} 
                                (PlayerIdentityId, PlayerId, PlayerRole, PlayerIdentityName, PlayerIdentityComparableName, TeamId, TotalMatches, PlayerIdentityRoute) 
                                VALUES (@PlayerIdentityId, @PlayerId, @PlayerRole, @PlayerIdentityName, @PlayerIdentityComparableName, @TeamId, @TotalMatches, @PlayerIdentityRoute)",
                                new
                                {
                                    extrasIdentity.PlayerIdentityId,
                                    extrasIdentity.PlayerId,
                                    PlayerRole = extrasIdentity.PlayerRole.ToString(),
                                    extrasIdentity.PlayerIdentityName,
                                    PlayerIdentityComparableName = extrasIdentity.ComparableName(),
                                    team.TeamId,
                                    extrasIdentity.TotalMatches,
                                    extrasIdentity.PlayerIdentityRoute
                                }, transaction).ConfigureAwait(false);
                        }

                        transaction.Commit();
                    }
                }

                await _auditRepository.CreateAudit(new AuditRecord
                {
                    Action = AuditAction.Create,
                    MemberKey = memberKey,
                    ActorName = memberName,
                    EntityUri = team.EntityUri,
                    State = JsonConvert.SerializeObject(team),
                    AuditDate = DateTime.UtcNow
                }).ConfigureAwait(false);
            }
            catch (SqlException ex)
            {
                _logger.Error(typeof(SqlServerTeamRepository), ex);
            }

            return team;
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

            try
            {
                string routeBeforeUpdate = team.TeamRoute;
                team.Introduction = _htmlSanitiser.Sanitize(team.Introduction);
                team.PlayingTimes = _htmlSanitiser.Sanitize(team.PlayingTimes);
                team.Cost = _htmlSanitiser.Sanitize(team.Cost);
                team.PublicContactDetails = _htmlSanitiser.Sanitize(team.PublicContactDetails);
                team.PrivateContactDetails = _htmlSanitiser.Sanitize(team.PrivateContactDetails);
                team.Facebook = PrefixUrlProtocol(team.Facebook);
                team.Twitter = PrefixAtSign(team.Twitter);
                team.Instagram = PrefixAtSign(team.Instagram);
                team.YouTube = PrefixUrlProtocol(team.YouTube);
                team.Website = PrefixUrlProtocol(team.Website);

                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {

                        team.TeamRoute = _routeGenerator.GenerateRoute("/teams", team.TeamName, NoiseWords.TeamRoute);
                        if (team.TeamRoute != routeBeforeUpdate)
                        {
                            int count;
                            do
                            {
                                count = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Team} WHERE TeamRoute = @TeamRoute", new { team.TeamRoute }, transaction).ConfigureAwait(false);
                                if (count > 0)
                                {
                                    team.TeamRoute = _routeGenerator.IncrementRoute(team.TeamRoute);
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
                                TeamType = team.TeamType.ToString(),
                                team.AgeRangeLower,
                                team.AgeRangeUpper,
                                team.FromYear,
                                team.UntilYear,
                                team.PlayerType,
                                team.Introduction,
                                team.PlayingTimes,
                                team.Cost,
                                team.PublicContactDetails,
                                team.PrivateContactDetails,
                                team.Facebook,
                                team.Twitter,
                                team.Instagram,
                                team.YouTube,
                                team.Website,
                                team.TeamRoute,
                                team.TeamId
                            }, transaction).ConfigureAwait(false);

                        var currentName = await connection.ExecuteScalarAsync<string>($"SELECT TeamName FROM {Tables.TeamName} WHERE TeamId = @TeamId AND UntilDate IS NULL", new { team.TeamId }, transaction).ConfigureAwait(false);
                        if (team.TeamName?.Trim() != currentName?.Trim())
                        {
                            await connection.ExecuteAsync($"UPDATE {Tables.TeamName} SET UntilDate = GETUTCDATE() WHERE TeamId = @TeamId AND UntilDate IS NULL", new { team.TeamId }, transaction).ConfigureAwait(false);
                            await connection.ExecuteAsync($@"INSERT INTO {Tables.TeamName} 
                                (TeamNameId, TeamId, TeamName, FromDate) VALUES (@TeamNameId, @TeamId, @TeamName, GETUTCDATE())",
                                                            new
                                                            {
                                                                TeamNameId = Guid.NewGuid(),
                                                                team.TeamId,
                                                                team.TeamName
                                                            }, transaction).ConfigureAwait(false);
                        }

                        await connection.ExecuteAsync($"DELETE FROM {Tables.TeamMatchLocation} WHERE TeamId = @TeamId AND MatchLocationId NOT IN @MatchLocationIds", new { team.TeamId, MatchLocationIds = team.MatchLocations.Select(x => x.MatchLocationId) }, transaction).ConfigureAwait(false);
                        var currentLocations = (await connection.QueryAsync<Guid>($"SELECT MatchLocationId FROM {Tables.TeamMatchLocation} WHERE TeamId = @TeamId", new { team.TeamId }, transaction).ConfigureAwait(false)).ToList();
                        foreach (var location in team.MatchLocations)
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
                                        team.TeamId,
                                        location.MatchLocationId
                                    },
                                    transaction).ConfigureAwait(false);
                            }
                        }

                        transaction.Commit();
                    }

                    if (routeBeforeUpdate != team.TeamRoute)
                    {
                        await _redirectsRepository.InsertRedirect(routeBeforeUpdate, team.TeamRoute, null).ConfigureAwait(false);
                    }
                }

                await _auditRepository.CreateAudit(new AuditRecord
                {
                    Action = AuditAction.Update,
                    MemberKey = memberKey,
                    ActorName = memberName,
                    EntityUri = team.EntityUri,
                    State = JsonConvert.SerializeObject(team),
                    AuditDate = DateTime.UtcNow
                }).ConfigureAwait(false);

            }
            catch (SqlException ex)
            {
                _logger.Error(typeof(SqlServerTeamRepository), ex);
            }

            return team;
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

            try
            {
                string routeBeforeUpdate = team.TeamRoute;
                team.Introduction = _htmlSanitiser.Sanitize(team.Introduction);
                team.Cost = _htmlSanitiser.Sanitize(team.Cost);
                team.PublicContactDetails = _htmlSanitiser.Sanitize(team.PublicContactDetails);
                team.PrivateContactDetails = _htmlSanitiser.Sanitize(team.PrivateContactDetails);
                team.Facebook = PrefixUrlProtocol(team.Facebook);
                team.Twitter = PrefixAtSign(team.Twitter);
                team.Instagram = PrefixAtSign(team.Instagram);
                team.YouTube = PrefixUrlProtocol(team.YouTube);
                team.Website = PrefixUrlProtocol(team.Website);

                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        var routePrefix = Regex.Match(team.TeamRoute, @"^\/tournaments\/[a-z0-9-]+\/teams").Value;
                        team.TeamRoute = _routeGenerator.GenerateRoute(routePrefix, team.TeamName, NoiseWords.TeamRoute);
                        if (team.TeamRoute != routeBeforeUpdate)
                        {
                            int count;
                            do
                            {
                                count = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Team} WHERE TeamRoute = @TeamRoute", new { team.TeamRoute }, transaction).ConfigureAwait(false);
                                if (count > 0)
                                {
                                    team.TeamRoute = _routeGenerator.IncrementRoute(team.TeamRoute);
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
                                team.AgeRangeLower,
                                team.AgeRangeUpper,
                                team.PlayerType,
                                team.Introduction,
                                team.Cost,
                                team.PublicContactDetails,
                                team.PrivateContactDetails,
                                team.Facebook,
                                team.Twitter,
                                team.Instagram,
                                team.YouTube,
                                team.Website,
                                team.TeamRoute,
                                team.TeamId
                            }, transaction).ConfigureAwait(false);

                        // No need to support changes of name when the team only lasts for a day
                        await connection.ExecuteAsync($"UPDATE {Tables.TeamName} SET TeamName = @TeamName WHERE TeamId = @TeamId", new { team.TeamId, team.TeamName }, transaction).ConfigureAwait(false);

                        transaction.Commit();
                    }

                    if (routeBeforeUpdate != team.TeamRoute)
                    {
                        await _redirectsRepository.InsertRedirect(routeBeforeUpdate, team.TeamRoute, null).ConfigureAwait(false);
                    }
                }

                await _auditRepository.CreateAudit(new AuditRecord
                {
                    Action = AuditAction.Update,
                    MemberKey = memberKey,
                    ActorName = memberName,
                    EntityUri = team.EntityUri,
                    State = JsonConvert.SerializeObject(team),
                    AuditDate = DateTime.UtcNow
                }).ConfigureAwait(false);

            }
            catch (SqlException ex)
            {
                _logger.Error(typeof(SqlServerTeamRepository), ex);
            }

            return team;
        }

        private string PrefixUrlProtocol(string url)
        {
            url = url?.Trim();
            if (!string.IsNullOrEmpty(url) && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
            }
            return url;
        }

        private string PrefixAtSign(string account)
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
