using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Ganss.XSS;
using Newtonsoft.Json;
using Stoolball.Competitions;
using Stoolball.Logging;
using Stoolball.Matches;
using Stoolball.Routing;
using Stoolball.Teams;
using static Stoolball.Data.SqlServer.Constants;

namespace Stoolball.Data.SqlServer
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
        private readonly IBowlingScorecardComparer _bowlingScorecardComparer;
        private readonly IBattingScorecardComparer _battingScorecardComparer;
        private readonly IPlayerRepository _playerRepository;

        public SqlServerMatchRepository(IDatabaseConnectionFactory databaseConnectionFactory, IAuditRepository auditRepository, ILogger logger, IRouteGenerator routeGenerator,
            IRedirectsRepository redirectsRepository, IHtmlSanitizer htmlSanitiser, IMatchNameBuilder matchNameBuilder, IPlayerTypeSelector playerTypeSelector,
            IBowlingScorecardComparer bowlingScorecardComparer, IBattingScorecardComparer battingScorecardComparer, IPlayerRepository playerRepository)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
            _htmlSanitiser = htmlSanitiser ?? throw new ArgumentNullException(nameof(htmlSanitiser));
            _matchNameBuilder = matchNameBuilder ?? throw new ArgumentNullException(nameof(matchNameBuilder));
            _playerTypeSelector = playerTypeSelector ?? throw new ArgumentNullException(nameof(playerTypeSelector));
            _bowlingScorecardComparer = bowlingScorecardComparer ?? throw new ArgumentNullException(nameof(bowlingScorecardComparer));
            _battingScorecardComparer = battingScorecardComparer ?? throw new ArgumentNullException(nameof(battingScorecardComparer));
            _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
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

                    match.EnableBonusOrPenaltyRuns = true;
                    if (match.Season != null)
                    {
                        match.Season = (await connection.QueryAsync<Season, Competition, Season>(
                             $@"SELECT s.SeasonId, s.PlayersPerTeam, s.Overs, s.EnableLastPlayerBatsOn, s.EnableBonusOrPenaltyRuns, 
                                    co.PlayerType
                                    FROM {Tables.Season} AS s INNER JOIN {Tables.Competition} co ON s.CompetitionId = co.CompetitionId 
                                    WHERE s.SeasonId = @SeasonId",
                                (season, competition) =>
                                {
                                    season.Competition = competition;
                                    return season;
                                },
                                new { match.Season.SeasonId },
                                transaction,
                                splitOn: "PlayerType"
                            ).ConfigureAwait(false)).First();
                        match.PlayersPerTeam = match.Season.PlayersPerTeam;
                        match.LastPlayerBatsOn = match.Season.EnableLastPlayerBatsOn;
                        match.EnableBonusOrPenaltyRuns = match.Season.EnableBonusOrPenaltyRuns;
                    }
                    match.PlayerType = _playerTypeSelector.SelectPlayerType(match);

                    await connection.ExecuteAsync($@"INSERT INTO {Tables.Match}
						(MatchId, MatchName, UpdateMatchNameAutomatically, MatchLocationId, MatchType, PlayerType, PlayersPerTeam, InningsOrderIsKnown,
						 StartTime, StartTimeIsKnown, LastPlayerBatsOn, EnableBonusOrPenaltyRuns, MatchNotes, SeasonId, MatchRoute, MemberKey)
						VALUES (@MatchId, @MatchName, @UpdateMatchNameAutomatically, @MatchLocationId, @MatchType, @PlayerType, @PlayersPerTeam, @InningsOrderIsKnown, 
                        @StartTime, @StartTimeIsKnown, @LastPlayerBatsOn, @EnableBonusOrPenaltyRuns, @MatchNotes, @SeasonId, @MatchRoute, @MemberKey)",
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
                        match.LastPlayerBatsOn,
                        match.EnableBonusOrPenaltyRuns,
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
                            match.Season?.Overs
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
                            match.Season?.Overs
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

            return match;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Dapper uses it.")]
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

            var routeBeforeUpdate = match.MatchRoute;
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

                    if (routeBeforeUpdate != match.MatchRoute)
                    {
                        await _redirectsRepository.InsertRedirect(routeBeforeUpdate, match.MatchRoute, null, transaction).ConfigureAwait(false);
                    }

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

            return match;
        }

        private static async Task RemoveTeamIfRequired(TeamRole teamRole, IEnumerable<MatchTeamResult> currentTeams, IEnumerable<TeamInMatch> updatedTeams, IDbTransaction transaction)
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

        /// <summary>
        /// Updates details known at the start of play - the location, who won the toss, who is batting, or why cancellation occurred
        /// </summary>
        public async Task<Match> UpdateStartOfPlay(Match match, Guid memberKey, string memberName)
        {
            if (match is null)
            {
                throw new ArgumentNullException(nameof(match));
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
                    var beforeUpdate = await connection.QuerySingleAsync<Match>(
                        $@"SELECT MatchResultType, UpdateMatchNameAutomatically, MatchName, MatchRoute, StartTime
                            FROM {Tables.Match} 
                            WHERE MatchId = @MatchId",
                        new { match.MatchId },
                        transaction).ConfigureAwait(false);

                    // the route might change if teams were missing and are only now being added
                    match.StartTime = beforeUpdate.StartTime;
                    await UpdateMatchRoute(match, beforeUpdate.MatchRoute, transaction).ConfigureAwait(false);

                    if (!match.MatchResultType.HasValue && beforeUpdate.MatchResultType.HasValue &&
                        new List<MatchResultType> { MatchResultType.HomeWin, MatchResultType.AwayWin, MatchResultType.Tie }.Contains(beforeUpdate.MatchResultType.Value))
                    {
                        // don't update result type because we've only submitted "match went ahead" and the result is already recorded, 
                        // therefore also don't update match name
                        await connection.ExecuteAsync($@"UPDATE {Tables.Match} SET
                                MatchLocationId = @MatchLocationId, 
                                InningsOrderIsKnown = @InningsOrderIsKnown,
                                MatchRoute = @MatchRoute
                                WHERE MatchId = @MatchId",
                        new
                        {
                            match.MatchLocation?.MatchLocationId,
                            match.InningsOrderIsKnown,
                            match.MatchRoute,
                            match.MatchId
                        }, transaction).ConfigureAwait(false);
                    }
                    else
                    {
                        // safe to update result type and name
                        if (match.UpdateMatchNameAutomatically)
                        {
                            match.MatchName = _matchNameBuilder.BuildMatchName(match);
                        }
                        else
                        {
                            match.MatchName = beforeUpdate.MatchName;
                        }

                        await connection.ExecuteAsync($@"UPDATE {Tables.Match} SET
                                MatchName = @MatchName,                                
                                MatchLocationId = @MatchLocationId, 
                                InningsOrderIsKnown = @InningsOrderIsKnown, 
                                MatchResultType = @MatchResultType,
                                MatchRoute = @MatchRoute
                                WHERE MatchId = @MatchId",
                        new
                        {
                            match.MatchName,
                            match.MatchLocation?.MatchLocationId,
                            match.InningsOrderIsKnown,
                            MatchResultType = match.MatchResultType?.ToString(),
                            match.MatchRoute,
                            match.MatchId
                        }, transaction).ConfigureAwait(false);
                    }

                    foreach (var team in match.Teams)
                    {
                        if (team.MatchTeamId.HasValue)
                        {
                            await connection.ExecuteAsync($"UPDATE {Tables.MatchTeam} SET WonToss = @WonToss WHERE MatchTeamId = @MatchTeamId",
                            new { team.WonToss, team.MatchTeamId },
                            transaction).ConfigureAwait(false);
                        }
                        else
                        {
                            team.MatchTeamId = Guid.NewGuid();
                            await connection.ExecuteAsync($@"INSERT INTO {Tables.MatchTeam} 
                                    (MatchTeamId, MatchId, TeamId, TeamRole, WonToss)
                                     VALUES (@MatchTeamId, @MatchId, @TeamId, @TeamRole, @WonToss)",
                                 new
                                 {
                                     team.MatchTeamId,
                                     match.MatchId,
                                     team.Team.TeamId,
                                     team.TeamRole,
                                     team.WonToss
                                 },
                                 transaction).ConfigureAwait(false);

                            // You cannot set the order of innings in a match until both teams are fixed, so this is the last point where 
                            // we know that, in the database, match.InningsOrderIsKnown is false and the assumption is that the home team 
                            // batted first. Update the MatchInnings accordingly so that the MatchTeamIds are in there, knowing that the 
                            // innings order may be changed a few lines later.
                            var oddInnings = match.MatchInnings.Where(x => x.InningsOrderInMatch % 2 == 1);
                            var evenInnings = match.MatchInnings.Where(x => x.InningsOrderInMatch % 2 == 0);
                            if (team.TeamRole == TeamRole.Home)
                            {
                                await InsertMatchTeamIdIntoMatchInnings(transaction, team.MatchTeamId.Value, oddInnings.Select(x => x.MatchInningsId), evenInnings.Select(x => x.MatchInningsId)).ConfigureAwait(false);
                                foreach (var innings in oddInnings) { innings.BattingMatchTeamId = team.MatchTeamId; }
                                foreach (var innings in evenInnings) { innings.BowlingMatchTeamId = team.MatchTeamId; }
                            }
                            else if (team.TeamRole == TeamRole.Away)
                            {
                                await InsertMatchTeamIdIntoMatchInnings(transaction, team.MatchTeamId.Value, evenInnings.Select(x => x.MatchInningsId), oddInnings.Select(x => x.MatchInningsId)).ConfigureAwait(false);
                                foreach (var innings in evenInnings) { innings.BattingMatchTeamId = team.MatchTeamId; }
                                foreach (var innings in oddInnings) { innings.BowlingMatchTeamId = team.MatchTeamId; }
                            }
                        }
                    }

                    if (match.InningsOrderIsKnown)
                    {
                        // All teams and innings should now have match team ids to work with
                        var battedFirst = match.Teams.Single(x => x.BattedFirst == true).MatchTeamId;
                        var shouldBeOddInnings = match.MatchInnings.Where(x => x.BattingMatchTeamId == battedFirst);
                        var shouldBeEvenInnings = match.MatchInnings.Where(x => x.BattingMatchTeamId != battedFirst);
                        foreach (var innings in shouldBeOddInnings)
                        {
                            var alreadyOdd = innings.InningsOrderInMatch % 2 == 1;
                            innings.InningsOrderInMatch = alreadyOdd ? innings.InningsOrderInMatch : innings.InningsOrderInMatch - 1;
                        }
                        foreach (var innings in shouldBeEvenInnings)
                        {
                            var alreadyEven = innings.InningsOrderInMatch % 2 == 0;
                            innings.InningsOrderInMatch = alreadyEven ? innings.InningsOrderInMatch : innings.InningsOrderInMatch + 1;
                        }

                        foreach (var innings in match.MatchInnings)
                        {
                            await connection.ExecuteAsync($@"UPDATE { Tables.MatchInnings } SET
                                InningsOrderInMatch = @InningsOrderInMatch
                                WHERE MatchInningsId = @MatchInningsId",
                                new
                                {
                                    innings.InningsOrderInMatch,
                                    innings.MatchInningsId
                                },
                                transaction).ConfigureAwait(false);
                        }
                    }

                    if (beforeUpdate.MatchRoute != match.MatchRoute)
                    {
                        await _redirectsRepository.InsertRedirect(beforeUpdate.MatchRoute, match.MatchRoute, null, transaction).ConfigureAwait(false);
                    }

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

            return match;
        }

        /// <summary>
        /// Updates the battings scorecard for a single innings of a match
        /// </summary>
        public async Task<MatchInnings> UpdateBattingScorecard(MatchInnings innings, Guid memberKey, string memberName)
        {
            if (innings is null)
            {
                throw new ArgumentNullException(nameof(innings));
            }

            if (memberName is null)
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    // Select existing innings and work out which ones have changed.
                    var inningsBefore = await connection.QueryAsync<PlayerInnings, PlayerIdentity, PlayerIdentity, PlayerIdentity, PlayerInnings>(
                        $@"SELECT i.PlayerInningsId, i.BattingPosition, i.DismissalType, i.RunsScored, i.BallsFaced,
                               bat.PlayerIdentityName,
                               field.PlayerIdentityName,
                               bowl.PlayerIdentityName
                               FROM {Tables.PlayerInnings} i 
                               INNER JOIN {Tables.PlayerIdentity} bat ON i.PlayerIdentityId = bat.PlayerIdentityId
                               LEFT JOIN {Tables.PlayerIdentity} field ON i.DismissedById = field.PlayerIdentityId
                               LEFT JOIN {Tables.PlayerIdentity} bowl ON i.BowlerId = bowl.PlayerIdentityId
                               WHERE i.MatchInningsId = @MatchInningsId",
                        (playerInnings, batter, fielder, bowler) =>
                        {
                            playerInnings.PlayerIdentity = batter;
                            playerInnings.DismissedBy = fielder;
                            playerInnings.Bowler = bowler;
                            return playerInnings;
                        },
                           new { innings.MatchInningsId },
                           transaction,
                           splitOn: "PlayerIdentityName, PlayerIdentityName, PlayerIdentityName").ConfigureAwait(false);

                    for (var i = 0; i < innings.PlayerInnings.Count; i++)
                    {
                        innings.PlayerInnings[i].BattingPosition = i + 1;
                    }

                    var comparison = _battingScorecardComparer.CompareScorecards(inningsBefore, innings.PlayerInnings);

                    // Now got lists of:
                    // - unchanged innings 
                    // - new innings
                    // - changed innings
                    // - deleted innings
                    // - affected players from the new/changed/deleted lists

                    foreach (var playerInnings in comparison.PlayerInningsAdded)
                    {
                        playerInnings.PlayerIdentity.Team = innings.BattingTeam.Team;
                        playerInnings.PlayerIdentity.PlayerIdentityId = await _playerRepository.CreateOrMatchPlayerIdentity(playerInnings.PlayerIdentity, memberKey, memberName).ConfigureAwait(false);

                        if (playerInnings.DismissedBy != null)
                        {
                            playerInnings.DismissedBy.Team = innings.BowlingTeam.Team;
                            playerInnings.DismissedBy.PlayerIdentityId = await _playerRepository.CreateOrMatchPlayerIdentity(playerInnings.DismissedBy, memberKey, memberName).ConfigureAwait(false);
                        }

                        if (playerInnings.Bowler != null)
                        {
                            playerInnings.Bowler.Team = innings.BowlingTeam.Team;
                            playerInnings.Bowler.PlayerIdentityId = await _playerRepository.CreateOrMatchPlayerIdentity(playerInnings.Bowler, memberKey, memberName).ConfigureAwait(false);
                        }

                        await connection.ExecuteAsync($@"INSERT INTO {Tables.PlayerInnings} 
                                (PlayerInningsId, BattingPosition, MatchInningsId, PlayerIdentityId, DismissalType, DismissedById, BowlerId, RunsScored, BallsFaced) 
                                VALUES 
                                (@PlayerInningsId, @BattingPosition, @MatchInningsId, @PlayerIdentityId, @DismissalType, @DismissedById, @BowlerId, @RunsScored, @BallsFaced)",
                            new
                            {
                                PlayerInningsId = Guid.NewGuid(),
                                playerInnings.BattingPosition,
                                innings.MatchInningsId,
                                playerInnings.PlayerIdentity.PlayerIdentityId,
                                DismissalType = playerInnings.DismissalType?.ToString(),
                                DismissedById = playerInnings.DismissedBy?.PlayerIdentityId,
                                BowlerId = playerInnings.Bowler?.PlayerIdentityId,
                                playerInnings.RunsScored,
                                playerInnings.BallsFaced
                            }, transaction).ConfigureAwait(false);

                    }

                    foreach (var (before, after) in comparison.PlayerInningsChanged)
                    {
                        after.PlayerIdentity.Team = innings.BattingTeam.Team;
                        after.PlayerIdentity.PlayerIdentityId = await _playerRepository.CreateOrMatchPlayerIdentity(after.PlayerIdentity, memberKey, memberName).ConfigureAwait(false);

                        if (after.DismissedBy != null)
                        {
                            after.DismissedBy.Team = innings.BowlingTeam.Team;
                            after.DismissedBy.PlayerIdentityId = await _playerRepository.CreateOrMatchPlayerIdentity(after.DismissedBy, memberKey, memberName).ConfigureAwait(false);
                        }

                        if (after.Bowler != null)
                        {
                            after.Bowler.Team = innings.BowlingTeam.Team;
                            after.Bowler.PlayerIdentityId = await _playerRepository.CreateOrMatchPlayerIdentity(after.Bowler, memberKey, memberName).ConfigureAwait(false);
                        }

                        await connection.ExecuteAsync($@"UPDATE {Tables.PlayerInnings} SET 
                                PlayerIdentityId = @PlayerIdentityId,
                                DismissalType = @DismissalType,
                                DismissedById = @DismissedById,
                                BowlerId = @BowlerId,
                                RunsScored = @RunsScored,
                                BallsFaced = @BallsFaced
                                WHERE PlayerInningsId = @PlayerInningsId",
                            new
                            {
                                after.PlayerIdentity.PlayerIdentityId,
                                DismissalType = after.DismissalType?.ToString(),
                                DismissedById = after.DismissedBy?.PlayerIdentityId,
                                BowlerId = after.Bowler?.PlayerIdentityId,
                                after.RunsScored,
                                after.BallsFaced,
                                before.PlayerInningsId
                            }, transaction).ConfigureAwait(false);

                    }

                    await connection.ExecuteAsync($"DELETE FROM {Tables.PlayerInnings} WHERE PlayerInningsId IN @PlayerInningsIds", new { PlayerInningsIds = comparison.PlayerInningsRemoved.Select(x => x.PlayerInningsId) }, transaction).ConfigureAwait(false);

                    // Update the extras and final score
                    await connection.ExecuteAsync(
                         $@"UPDATE {Tables.MatchInnings} SET 
                                Byes = @Byes,
                                Wides = @Wides,
                                NoBalls = @NoBalls,
                                BonusOrPenaltyRuns = @BonusOrPenaltyRuns,
                                Runs = @Runs,
                                Wickets = @Wickets
                                WHERE MatchInningsId = @MatchInningsId",
                        new
                        {
                            innings.Byes,
                            innings.Wides,
                            innings.NoBalls,
                            innings.BonusOrPenaltyRuns,
                            innings.Runs,
                            innings.Wickets,
                            innings.MatchInningsId
                        },
                        transaction).ConfigureAwait(false);

                    // Update the number of players per team
                    await connection.ExecuteAsync(
                         $@"UPDATE {Tables.Match} SET 
                                PlayersPerTeam = @PlayersPerTeam 
                                WHERE MatchId = (SELECT MatchId FROM {Tables.MatchInnings} WHERE MatchInningsId = @MatchInningsId)",
                        new
                        {
                            PlayersPerTeam = innings.PlayerInnings.Count,
                            innings.MatchInningsId
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
                EntityUri = innings.EntityUri,
                State = JsonConvert.SerializeObject(innings),
                AuditDate = DateTime.UtcNow
            }).ConfigureAwait(false);

            return innings;
        }

        /// <summary>
        /// Updates the bowling scorecard for a single innings of a match
        /// </summary>
        public async Task<MatchInnings> UpdateBowlingScorecard(MatchInnings innings, Guid memberKey, string memberName)
        {
            if (innings is null)
            {
                throw new ArgumentNullException(nameof(innings));
            }

            if (memberName is null)
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    // Select existing overs and work out which ones have changed.
                    var oversBefore = await connection.QueryAsync<Over, PlayerIdentity, Over>(
                        $@"SELECT o.OverId, o.OverNumber, o.BallsBowled, o.NoBalls, o.Wides, o.RunsConceded,
                               p.PlayerIdentityName
                               FROM {Tables.Over} o INNER JOIN {Tables.PlayerIdentity} p ON o.PlayerIdentityId = p.PlayerIdentityId
                               WHERE o.MatchInningsId = @MatchInningsId",
                        (over, playerIdentity) =>
                        {
                            over.PlayerIdentity = playerIdentity;
                            return over;
                        },
                           new { innings.MatchInningsId },
                           transaction,
                           splitOn: "PlayerIdentityName").ConfigureAwait(false);

                    for (var i = 0; i < innings.OversBowled.Count; i++)
                    {
                        innings.OversBowled[i].OverNumber = i + 1;
                    }

                    var comparison = _bowlingScorecardComparer.CompareScorecards(oversBefore, innings.OversBowled);

                    // Now got lists of:
                    // - unchanged overs 
                    // - new overs
                    // - changed overs
                    // - deleted overs
                    // - affected players from the new/changed/deleted lists

                    foreach (var over in comparison.OversAdded)
                    {
                        over.PlayerIdentity.Team = innings.BowlingTeam.Team;
                        over.PlayerIdentity.PlayerIdentityId = await _playerRepository.CreateOrMatchPlayerIdentity(over.PlayerIdentity, memberKey, memberName).ConfigureAwait(false);
                        await connection.ExecuteAsync($@"INSERT INTO {Tables.Over} 
                                (OverId, OverNumber, MatchInningsId, PlayerIdentityId, BallsBowled, NoBalls, Wides, RunsConceded) 
                                VALUES 
                                (@OverId, @OverNumber, @MatchInningsId, @PlayerIdentityId, @BallsBowled, @NoBalls, @Wides, @RunsConceded)",
                            new
                            {
                                OverId = Guid.NewGuid(),
                                over.OverNumber,
                                innings.MatchInningsId,
                                over.PlayerIdentity.PlayerIdentityId,
                                over.BallsBowled,
                                over.NoBalls,
                                over.Wides,
                                over.RunsConceded,
                            }, transaction).ConfigureAwait(false);

                    }

                    foreach (var (before, after) in comparison.OversChanged)
                    {
                        after.PlayerIdentity.Team = innings.BowlingTeam.Team;
                        after.PlayerIdentity.PlayerIdentityId = await _playerRepository.CreateOrMatchPlayerIdentity(after.PlayerIdentity, memberKey, memberName).ConfigureAwait(false);
                        await connection.ExecuteAsync($@"UPDATE {Tables.Over} SET 
                                PlayerIdentityId = @PlayerIdentityId,
                                BallsBowled = @BallsBowled,
                                NoBalls = @NoBalls,
                                Wides = @Wides,
                                RunsConceded = @RunsConceded
                                WHERE OverId = @OverId",
                            new
                            {
                                after.PlayerIdentity.PlayerIdentityId,
                                after.BallsBowled,
                                after.NoBalls,
                                after.Wides,
                                after.RunsConceded,
                                before.OverId
                            }, transaction).ConfigureAwait(false);

                    }

                    await connection.ExecuteAsync($"DELETE FROM {Tables.Over} WHERE OverId IN @OverIds", new { OverIds = comparison.OversRemoved.Select(x => x.OverId) }, transaction).ConfigureAwait(false);

                    // Update the number of overs
                    await connection.ExecuteAsync($@"UPDATE {Tables.MatchInnings} SET Overs = @Overs WHERE MatchInningsId = @MatchInningsId", new { innings.Overs, innings.MatchInningsId }, transaction).ConfigureAwait(false);

                    transaction.Commit();
                }

            }

            await _auditRepository.CreateAudit(new AuditRecord
            {
                Action = AuditAction.Update,
                MemberKey = memberKey,
                ActorName = memberName,
                EntityUri = innings.EntityUri,
                State = JsonConvert.SerializeObject(innings),
                AuditDate = DateTime.UtcNow
            }).ConfigureAwait(false);

            return innings;
        }

        /// <summary>
        /// Updates details known at the close of play - the winning team and any awards
        /// </summary>
        public async Task<Match> UpdateCloseOfPlay(Match match, Guid memberKey, string memberName)
        {
            if (match is null)
            {
                throw new ArgumentNullException(nameof(match));
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
                    var beforeUpdate = await connection.QuerySingleAsync<Match>(
                        $@"SELECT UpdateMatchNameAutomatically, MatchName
                            FROM {Tables.Match} 
                            WHERE MatchId = @MatchId",
                        new { match.MatchId },
                        transaction).ConfigureAwait(false);

                    if (match.UpdateMatchNameAutomatically)
                    {
                        match.MatchName = _matchNameBuilder.BuildMatchName(match);
                    }
                    else
                    {
                        match.MatchName = beforeUpdate.MatchName;
                    }

                    await connection.ExecuteAsync($@"UPDATE {Tables.Match} SET
                            MatchName = @MatchName,                                
                            MatchResultType = @MatchResultType
                            WHERE MatchId = @MatchId",
                        new
                        {
                            match.MatchName,
                            MatchResultType = match.MatchResultType?.ToString(),
                            match.MatchId
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

            return match;
        }

        private static async Task InsertMatchTeamIdIntoMatchInnings(IDbTransaction transaction, Guid matchTeamId, IEnumerable<Guid?> battingInnings, IEnumerable<Guid?> bowlingInnings)
        {
            await transaction.Connection.ExecuteAsync(
                $"UPDATE {Tables.MatchInnings} SET BattingMatchTeamId = @MatchTeamId WHERE MatchInningsId IN @MatchInningsIds",
                new
                {
                    MatchTeamId = matchTeamId,
                    MatchInningsIds = battingInnings
                },
                transaction).ConfigureAwait(false);

            await transaction.Connection.ExecuteAsync(
                $"UPDATE {Tables.MatchInnings} SET BowlingMatchTeamId = @MatchTeamId WHERE MatchInningsId IN @MatchInningsIds",
                new
                {
                    MatchTeamId = matchTeamId,
                    MatchInningsIds = bowlingInnings
                },
                transaction).ConfigureAwait(false);
        }

        private async Task UpdateMatchRoute(Match match, string routeBeforeUpdate, IDbTransaction transaction)
        {
            var baseRoute = string.Empty;
            if (!match.UpdateMatchNameAutomatically)
            {
                baseRoute = match.MatchName;
            }
            else if (match.Teams.Count > 0)
            {
                baseRoute = string.Join(" ", match.Teams.OrderBy(x => x.TeamRole).Select(x => x.Team.TeamName));
            }
            else if (!string.IsNullOrEmpty(match.MatchName))
            {
                baseRoute = match.MatchName;
            }
            else
            {
                baseRoute = "to-be-confirmed";
            }


            match.MatchRoute = _routeGenerator.GenerateRoute("/matches", baseRoute + " " + match.StartTime.LocalDateTime.Date.ToString("dMMMyyyy", CultureInfo.CurrentCulture), NoiseWords.MatchRoute);
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

        private static async Task PopulateTeamNames(Match match, IDbTransaction transaction)
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

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    await connection.ExecuteAsync($"DELETE FROM {Tables.StatisticsPlayerMatch} WHERE MatchId = @MatchId", new { match.MatchId }, transaction).ConfigureAwait(false);
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

    }
}
