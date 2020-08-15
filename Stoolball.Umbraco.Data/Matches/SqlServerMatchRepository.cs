using Dapper;
using Ganss.XSS;
using Newtonsoft.Json;
using Stoolball.Audit;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Routing;
using Stoolball.Teams;
using Stoolball.Umbraco.Data.Audit;
using Stoolball.Umbraco.Data.Redirects;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
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
        private readonly IMatchNameBuilder _matchNameBuilder;
        private readonly IPlayerTypeSelector _playerTypeSelector;

        public SqlServerMatchRepository(IDatabaseConnectionFactory databaseConnectionFactory, IAuditRepository auditRepository, ILogger logger, IRouteGenerator routeGenerator,
            IRedirectsRepository redirectsRepository, IHtmlSanitizer htmlSanitiser, IMatchNameBuilder matchNameBuilder, IPlayerTypeSelector playerTypeSelector)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
            _htmlSanitiser = htmlSanitiser ?? throw new ArgumentNullException(nameof(htmlSanitiser));
            _matchNameBuilder = matchNameBuilder ?? throw new ArgumentNullException(nameof(matchNameBuilder));
            _playerTypeSelector = playerTypeSelector ?? throw new ArgumentNullException(nameof(playerTypeSelector));
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
            if (match is null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            try
            {
                match.MatchId = Guid.NewGuid();
                match.UpdateMatchNameAutomatically = string.IsNullOrEmpty(match.MatchName);
                match.MatchNotes = _htmlSanitiser.Sanitize(match.MatchNotes);

                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        await PopulateTeamNames(match, transaction).ConfigureAwait(false);
                        await UpdateMatchRoute(match, string.Empty, transaction).ConfigureAwait(false);

                        if (match.UpdateMatchNameAutomatically)
                        {
                            match.MatchName = _matchNameBuilder.BuildMatchName(match);
                        }

                        if (match.Season != null)
                        {
                            match.Season.Competition = (await connection.QueryAsync<Competition>($@"SELECT co.PlayerType, co.PlayersPerTeam, co.Overs 
                                    FROM {Tables.Season} AS s INNER JOIN {Tables.Competition} co ON s.CompetitionId = co.CompetitionId 
                                    WHERE s.SeasonId = @SeasonId",
                                    new { match.Season.SeasonId },
                                    transaction
                                ).ConfigureAwait(false)).First();
                            match.PlayersPerTeam = match.Season.Competition.PlayersPerTeam;
                        }
                        match.PlayerType = _playerTypeSelector.SelectPlayerType(match);

                        await connection.ExecuteAsync($@"INSERT INTO {Tables.Match}
						(MatchId, MatchName, UpdateMatchNameAutomatically, MatchLocationId, MatchType, PlayerType, PlayersPerTeam, InningsOrderIsKnown,
						 StartTime, StartTimeIsKnown, MatchNotes, SeasonId, MatchRoute, MemberKey)
						VALUES (@MatchId, @MatchName, @UpdateMatchNameAutomatically, @MatchLocationId, @MatchType, @PlayerType, @PlayersPerTeam, @InningsOrderIsKnown, 
                        @StartTime, @StartTimeIsKnown, @MatchNotes, @SeasonId, @MatchRoute, @MemberKey)",
                        new
                        {
                            match.MatchId,
                            match.MatchName,
                            match.UpdateMatchNameAutomatically,
                            match.MatchLocation?.MatchLocationId,
                            MatchType = match.MatchType.ToString(),
                            PlayerType = match.PlayerType.ToString(),
                            match.PlayersPerTeam,
                            match.InningsOrderIsKnown,
                            StartTime = match.StartTime.UtcDateTime,
                            match.StartTimeIsKnown,
                            match.MatchNotes,
                            match.Season?.SeasonId,
                            match.MatchRoute,
                            MemberKey = memberKey
                        }, transaction).ConfigureAwait(false);

                        Guid? homeMatchTeamId = null;
                        Guid? awayMatchTeamId = null;

                        foreach (var team in match.Teams)
                        {
                            Guid matchTeamId;
                            if (team.TeamRole == TeamRole.Home)
                            {
                                homeMatchTeamId = Guid.NewGuid();
                                matchTeamId = homeMatchTeamId.Value;
                            }
                            else
                            {
                                awayMatchTeamId = Guid.NewGuid();
                                matchTeamId = awayMatchTeamId.Value;
                            }

                            await connection.ExecuteAsync($@"INSERT INTO {Tables.MatchTeam} 
								(MatchTeamId, MatchId, TeamId, TeamRole) VALUES (@MatchTeamId, @MatchId, @TeamId, @TeamRole)",
                                new
                                {
                                    MatchTeamId = matchTeamId,
                                    match.MatchId,
                                    team.Team.TeamId,
                                    TeamRole = team.TeamRole.ToString()
                                },
                                transaction).ConfigureAwait(false);
                        }

                        await connection.ExecuteAsync($@"INSERT INTO {Tables.MatchInnings} 
							(MatchInningsId, MatchId, BattingMatchTeamId, BowlingMatchTeamId, InningsOrderInMatch, Overs)
							VALUES (@MatchInningsId, @MatchId, @BattingMatchTeamId, @BowlingMatchTeamId, @InningsOrderInMatch, @Overs)",
                            new
                            {
                                MatchInningsId = Guid.NewGuid(),
                                match.MatchId,
                                BattingMatchTeamId = homeMatchTeamId,
                                BowlingMatchTeamId = awayMatchTeamId,
                                InningsOrderInMatch = 1,
                                match.Season?.Competition.Overs
                            },
                            transaction).ConfigureAwait(false);

                        await connection.ExecuteAsync($@"INSERT INTO {Tables.MatchInnings} 
							(MatchInningsId, MatchId, BattingMatchTeamId, BowlingMatchTeamId, InningsOrderInMatch, Overs)
							VALUES (@MatchInningsId, @MatchId, @BattingMatchTeamId, @BowlingMatchTeamId, @InningsOrderInMatch, @Overs)",
                            new
                            {
                                MatchInningsId = Guid.NewGuid(),
                                match.MatchId,
                                BattingMatchTeamId = awayMatchTeamId,
                                BowlingMatchTeamId = homeMatchTeamId,
                                InningsOrderInMatch = 2,
                                match.Season?.Competition.Overs
                            },
                            transaction).ConfigureAwait(false);

                        transaction.Commit();
                    }
                }

                await _auditRepository.CreateAudit(new AuditRecord
                {
                    Action = AuditAction.Create,
                    MemberKey = memberKey,
                    ActorName = memberName,
                    EntityUri = match.EntityUri,
                    State = JsonConvert.SerializeObject(match),
                    AuditDate = DateTime.UtcNow
                }).ConfigureAwait(false);
            }
            catch (SqlException ex)
            {
                _logger.Error(typeof(SqlServerMatchRepository), ex);
            }

            return match;
        }

        private class MatchTeamResult
        {
            public Guid? MatchTeamId { get; set; }
            public Guid? TeamId { get; set; }
            public TeamRole TeamRole { get; set; }
        }

        /// <summary>
        /// Updates a stoolball match
        /// </summary>
        public async Task<Match> UpdateMatch(Match match, Guid memberKey, string memberName)
        {
            if (match is null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            try
            {
                string routeBeforeUpdate = match.MatchRoute;
                match.UpdateMatchNameAutomatically = string.IsNullOrEmpty(match.MatchName);
                match.MatchNotes = _htmlSanitiser.Sanitize(match.MatchNotes);

                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        await PopulateTeamNames(match, transaction).ConfigureAwait(false);
                        await UpdateMatchRoute(match, routeBeforeUpdate, transaction).ConfigureAwait(false);

                        if (match.UpdateMatchNameAutomatically)
                        {
                            match.MatchName = _matchNameBuilder.BuildMatchName(match);
                        }

                        await connection.ExecuteAsync($@"UPDATE {Tables.Match} SET
						MatchName = @MatchName, 
                        UpdateMatchNameAutomatically = @UpdateMatchNameAutomatically,
                        MatchLocationId = @MatchLocationId, 
                        StartTime = @StartTime,
                        StartTimeIsKnown = @StartTimeIsKnown, 
                        SeasonId = @SeasonId,
                        MatchNotes = @MatchNotes, 
                        MatchResultType = @MatchResultType,
                        MatchRoute = @MatchRoute
                        WHERE MatchId = @MatchId",
                        new
                        {
                            match.MatchName,
                            match.UpdateMatchNameAutomatically,
                            match.MatchLocation?.MatchLocationId,
                            StartTime = match.StartTime.UtcDateTime,
                            match.StartTimeIsKnown,
                            match.Season?.SeasonId,
                            match.MatchNotes,
                            MatchResultType = match.MatchResultType?.ToString(),
                            match.MatchRoute,
                            match.MatchId
                        }, transaction).ConfigureAwait(false);


                        var currentTeams = await connection.QueryAsync<MatchTeamResult>(
                                $@"SELECT MatchTeamId, TeamId, TeamRole FROM {Tables.MatchTeam} WHERE MatchId = @MatchId", new { match.MatchId }, transaction
                            ).ConfigureAwait(false);

                        foreach (var team in match.Teams)
                        {
                            var currentTeamInRole = currentTeams.SingleOrDefault(x => x.TeamRole == team.TeamRole);

                            // Team added
                            if (currentTeamInRole == null)
                            {
                                await connection.ExecuteAsync($@"INSERT INTO {Tables.MatchTeam} 
								(MatchTeamId, MatchId, TeamId, TeamRole) VALUES (@MatchTeamId, @MatchId, @TeamId, @TeamRole)",
                                    new
                                    {
                                        MatchTeamId = Guid.NewGuid(),
                                        match.MatchId,
                                        team.Team.TeamId,
                                        TeamRole = team.TeamRole.ToString()
                                    },
                                    transaction).ConfigureAwait(false);
                            }
                            // Team changed
                            else if (currentTeamInRole.TeamId != team.Team.TeamId)
                            {
                                await connection.ExecuteAsync($"UPDATE {Tables.MatchTeam} SET TeamId = @TeamId WHERE MatchTeamId = @MatchTeamId",
                                new { team.Team.TeamId, currentTeamInRole.MatchTeamId },
                                transaction).ConfigureAwait(false);
                            }
                        }

                        // Team removed?
                        await RemoveTeamIfRequired(TeamRole.Home, currentTeams, match.Teams, transaction).ConfigureAwait(false);
                        await RemoveTeamIfRequired(TeamRole.Away, currentTeams, match.Teams, transaction).ConfigureAwait(false);

                        // Update innings with the new values for match team ids (assuming the match hasn't happened yet, 
                        // therefore the innings order is home bats first as assumed in CreateMatch)
                        await connection.ExecuteAsync($@"UPDATE { Tables.MatchInnings } SET
                                BattingMatchTeamId = (SELECT MatchTeamId FROM { Tables.MatchTeam } WHERE MatchId = @MatchId AND TeamRole = '{TeamRole.Home.ToString()}'), 
                                BowlingMatchTeamId = (SELECT MatchTeamId FROM { Tables.MatchTeam } WHERE MatchId = @MatchId AND TeamRole = '{TeamRole.Away.ToString()}')
                                WHERE MatchId = @MatchId AND InningsOrderInMatch = 1",
                            new
                            {
                                match.MatchId,
                            },
                            transaction).ConfigureAwait(false);

                        await connection.ExecuteAsync($@"UPDATE {Tables.MatchInnings} SET
                                BattingMatchTeamId = (SELECT MatchTeamId FROM { Tables.MatchTeam } WHERE MatchId = @MatchId AND TeamRole = '{TeamRole.Away.ToString()}'), 
                                BowlingMatchTeamId = (SELECT MatchTeamId FROM { Tables.MatchTeam } WHERE MatchId = @MatchId AND TeamRole = '{TeamRole.Home.ToString()}')
                                WHERE MatchId = @MatchId AND InningsOrderInMatch = 2",
                            new
                            {
                                match.MatchId,
                            },
                            transaction).ConfigureAwait(false);

                        transaction.Commit();
                    }
                }

                await _auditRepository.CreateAudit(new AuditRecord
                {
                    Action = AuditAction.Update,
                    MemberKey = memberKey,
                    ActorName = memberName,
                    EntityUri = match.EntityUri,
                    State = JsonConvert.SerializeObject(match),
                    AuditDate = DateTime.UtcNow
                }).ConfigureAwait(false);
            }
            catch (SqlException ex)
            {
                _logger.Error(typeof(SqlServerMatchRepository), ex);
            }

            return match;
        }

        private static async Task RemoveTeamIfRequired(TeamRole teamRole, IEnumerable<MatchTeamResult> currentTeams, IEnumerable<TeamInMatch> updatedTeams, System.Data.IDbTransaction transaction)
        {
            var currentTeamInRole = currentTeams.SingleOrDefault(x => x.TeamRole == teamRole);
            var newTeamInRole = updatedTeams.SingleOrDefault(x => x.TeamRole == teamRole);
            if (currentTeamInRole != null && newTeamInRole == null)
            {
                await transaction.Connection.ExecuteAsync($@"UPDATE {Tables.MatchInnings} SET BattingMatchTeamId = NULL WHERE BattingMatchTeamId = @MatchTeamId", new { currentTeamInRole.MatchTeamId }, transaction).ConfigureAwait(false);
                await transaction.Connection.ExecuteAsync($@"UPDATE {Tables.MatchInnings} SET BowlingMatchTeamId = NULL WHERE BowlingMatchTeamId = @MatchTeamId", new { currentTeamInRole.MatchTeamId }, transaction).ConfigureAwait(false);
                await transaction.Connection.ExecuteAsync($"DELETE FROM { Tables.MatchTeam } WHERE MatchTeamId = @MatchTeamId", new { currentTeamInRole.MatchTeamId }, transaction).ConfigureAwait(false);
            }
        }

        private async Task UpdateMatchRoute(Match match, string routeBeforeUpdate, System.Data.IDbTransaction transaction)
        {
            string baseRoute = string.Empty;
            if (!match.UpdateMatchNameAutomatically)
            {
                baseRoute = match.MatchName;
            }
            else if (match.Teams.Count > 0)
            {
                baseRoute = string.Join(" ", match.Teams.Select(x => x.Team.TeamName));
            }
            else if (!string.IsNullOrEmpty(match.MatchName))
            {
                baseRoute = match.MatchName;
            }
            else
            {
                baseRoute = "to-be-confirmed";
            }


            match.MatchRoute = _routeGenerator.GenerateRoute("/matches", baseRoute + " " + match.StartTime.Date.ToString("dMMMyyyy", CultureInfo.CurrentCulture), NoiseWords.MatchRoute);
            if (match.MatchRoute != routeBeforeUpdate)
            {
                int count;
                do
                {
                    count = await transaction.Connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Match} WHERE MatchRoute = @MatchRoute", new { match.MatchRoute }, transaction).ConfigureAwait(false);
                    if (count > 0)
                    {
                        match.MatchRoute = _routeGenerator.IncrementRoute(match.MatchRoute);
                    }
                }
                while (count > 0);
            }
        }

        private static async Task PopulateTeamNames(Match match, System.Data.IDbTransaction transaction)
        {
            if (match.Teams.Count > 0)
            {
                var teamsWithNames = await transaction.Connection.QueryAsync<Team>($@"SELECT t.TeamId, t.PlayerType, tn.TeamName 
                                                                                    FROM {Tables.Team} AS t INNER JOIN {Tables.TeamName} tn ON t.TeamId = tn.TeamId AND tn.UntilDate IS NULL 
                                                                                    WHERE t.TeamId IN @TeamIds",
                                                                    new { TeamIds = match.Teams.Select(x => x.Team.TeamId).ToList() },
                                                                    transaction
                                                                ).ConfigureAwait(false);

                // Used in generating the match name
                foreach (var team in match.Teams)
                {
                    team.Team = teamsWithNames.Single(x => x.TeamId == team.Team.TeamId);
                }
            }
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
