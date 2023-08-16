using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Ganss.XSS;
using Newtonsoft.Json;
using Stoolball.Awards;
using Stoolball.Data.Abstractions;
using Stoolball.Logging;
using Stoolball.Matches;
using Stoolball.Routing;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Teams;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// Writes stoolball match data to the Umbraco database
    /// </summary>
    public class SqlServerMatchRepository : IMatchRepository
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IDapperWrapper _dapperWrapper;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger<SqlServerMatchRepository> _logger;
        private readonly IRouteGenerator _routeGenerator;
        private readonly IRedirectsRepository _redirectsRepository;
        private readonly IHtmlSanitizer _htmlSanitiser;
        private readonly IMatchNameBuilder _matchNameBuilder;
        private readonly IPlayerTypeSelector _playerTypeSelector;
        private readonly IBowlingScorecardComparer _bowlingScorecardComparer;
        private readonly IBattingScorecardComparer _battingScorecardComparer;
        private readonly IPlayerRepository _playerRepository;
        private readonly IDataRedactor _dataRedactor;
        private readonly IStatisticsRepository _statisticsRepository;
        private readonly IOversHelper _oversHelper;
        private readonly IPlayerInMatchStatisticsBuilder _playerInMatchStatisticsBuilder;
        private readonly IMatchInningsFactory _matchInningsFactory;
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly IStoolballEntityCopier _copier;

        public SqlServerMatchRepository(IDatabaseConnectionFactory databaseConnectionFactory, IDapperWrapper dapperWrapper, IAuditRepository auditRepository, ILogger<SqlServerMatchRepository> logger, IRouteGenerator routeGenerator,
            IRedirectsRepository redirectsRepository, IHtmlSanitizer htmlSanitiser, IMatchNameBuilder matchNameBuilder, IPlayerTypeSelector playerTypeSelector,
            IBowlingScorecardComparer bowlingScorecardComparer, IBattingScorecardComparer battingScorecardComparer, IPlayerRepository playerRepository, IDataRedactor dataRedactor,
            IStatisticsRepository statisticsRepository, IOversHelper oversHelper, IPlayerInMatchStatisticsBuilder playerInMatchStatisticsBuilder, IMatchInningsFactory matchInningsFactory,
            ISeasonDataSource seasonDataSource, IStoolballEntityCopier copier)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _dapperWrapper = dapperWrapper ?? throw new ArgumentNullException(nameof(dapperWrapper));
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
            _dataRedactor = dataRedactor ?? throw new ArgumentNullException(nameof(dataRedactor));
            _statisticsRepository = statisticsRepository ?? throw new ArgumentNullException(nameof(statisticsRepository));
            _oversHelper = oversHelper ?? throw new ArgumentNullException(nameof(oversHelper));
            _playerInMatchStatisticsBuilder = playerInMatchStatisticsBuilder ?? throw new ArgumentNullException(nameof(playerInMatchStatisticsBuilder));
            _matchInningsFactory = matchInningsFactory ?? throw new ArgumentNullException(nameof(matchInningsFactory));
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _copier = copier ?? throw new ArgumentNullException(nameof(copier));
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

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var createdMatch = await CreateMatch(match, memberKey, memberName, transaction).ConfigureAwait(false);
                    transaction.Commit();
                    return createdMatch;
                }
            }
        }

        /// <summary>
        /// Creates a stoolball match
        /// </summary>
        public async Task<Match> CreateMatch(Match match, Guid memberKey, string memberName, IDbTransaction transaction)
        {
            if (match is null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            if (transaction is null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            var auditableMatch = _copier.CreateAuditableCopy(match);
            auditableMatch.MatchId = Guid.NewGuid();
            auditableMatch.UpdateMatchNameAutomatically = string.IsNullOrEmpty(auditableMatch.MatchName);
            auditableMatch.MatchNotes = _htmlSanitiser.Sanitize(auditableMatch.MatchNotes);
            auditableMatch.MemberKey = memberKey;

            await PopulateTeamNames(auditableMatch, transaction).ConfigureAwait(false);
            await UpdateMatchRoute(auditableMatch, string.Empty, transaction).ConfigureAwait(false);

            if (auditableMatch.UpdateMatchNameAutomatically)
            {
                auditableMatch.MatchName = _matchNameBuilder.BuildMatchName(auditableMatch);
            }

            auditableMatch.EnableBonusOrPenaltyRuns = true;
            if (auditableMatch.Season != null && auditableMatch.Season.SeasonId.HasValue)
            {
                auditableMatch.Season = _copier.CreateAuditableCopy(await _seasonDataSource.ReadSeasonById(auditableMatch.Season.SeasonId.Value, true).ConfigureAwait(false));
                auditableMatch.PlayersPerTeam = auditableMatch.Season.PlayersPerTeam;
                auditableMatch.LastPlayerBatsOn = auditableMatch.Season.EnableLastPlayerBatsOn;
                auditableMatch.EnableBonusOrPenaltyRuns = auditableMatch.Season.EnableBonusOrPenaltyRuns;
            }
            auditableMatch.PlayerType = _playerTypeSelector.SelectPlayerType(auditableMatch);

            await transaction.Connection.ExecuteAsync($@"INSERT INTO {Tables.Match}
						(MatchId, MatchName, UpdateMatchNameAutomatically, TournamentId, MatchLocationId, MatchType, PlayerType, PlayersPerTeam, InningsOrderIsKnown,
						 StartTime, StartTimeIsKnown, LastPlayerBatsOn, EnableBonusOrPenaltyRuns, MatchNotes, SeasonId, MatchRoute, MemberKey)
						VALUES (@MatchId, @MatchName, @UpdateMatchNameAutomatically, @TournamentId, @MatchLocationId, @MatchType, @PlayerType, @PlayersPerTeam, @InningsOrderIsKnown, 
                        @StartTime, @StartTimeIsKnown, @LastPlayerBatsOn, @EnableBonusOrPenaltyRuns, @MatchNotes, @SeasonId, @MatchRoute, @MemberKey)",
            new
            {
                auditableMatch.MatchId,
                auditableMatch.MatchName,
                auditableMatch.UpdateMatchNameAutomatically,
                auditableMatch.Tournament?.TournamentId,
                auditableMatch.MatchLocation?.MatchLocationId,
                MatchType = auditableMatch.MatchType.ToString(),
                PlayerType = auditableMatch.PlayerType.ToString(),
                auditableMatch.PlayersPerTeam,
                auditableMatch.InningsOrderIsKnown,
                StartTime = TimeZoneInfo.ConvertTimeToUtc(auditableMatch.StartTime.DateTime, TimeZoneInfo.FindSystemTimeZoneById(UkTimeZone())),
                auditableMatch.StartTimeIsKnown,
                auditableMatch.LastPlayerBatsOn,
                auditableMatch.EnableBonusOrPenaltyRuns,
                auditableMatch.MatchNotes,
                auditableMatch.Season?.SeasonId,
                auditableMatch.MatchRoute,
                auditableMatch.MemberKey
            }, transaction).ConfigureAwait(false);

            Guid? homeMatchTeamId = null;
            Guid? awayMatchTeamId = null;

            foreach (var team in auditableMatch.Teams)
            {
                Guid? matchTeamId = null;
                if (team.TeamRole == TeamRole.Home)
                {
                    homeMatchTeamId = Guid.NewGuid();
                    matchTeamId = homeMatchTeamId.Value;
                }
                else if (team.TeamRole == TeamRole.Away)
                {
                    awayMatchTeamId = Guid.NewGuid();
                    matchTeamId = awayMatchTeamId.Value;
                }
                else if (team.TeamRole == TeamRole.Training)
                {
                    matchTeamId = Guid.NewGuid();
                }

                team.MatchTeamId = matchTeamId;

                await transaction.Connection.ExecuteAsync($@"INSERT INTO {Tables.MatchTeam} 
								(MatchTeamId, MatchId, TeamId, TeamRole) VALUES (@MatchTeamId, @MatchId, @TeamId, @TeamRole)",
                    new
                    {
                        team.MatchTeamId,
                        auditableMatch.MatchId,
                        team.Team.TeamId,
                        TeamRole = team.TeamRole.ToString()
                    },
                    transaction).ConfigureAwait(false);
            }

            if (auditableMatch.MatchType != MatchType.TrainingSession)
            {
                auditableMatch.MatchInnings.Add(_matchInningsFactory.CreateMatchInnings(auditableMatch, homeMatchTeamId, awayMatchTeamId));
                auditableMatch.MatchInnings.Add(_matchInningsFactory.CreateMatchInnings(auditableMatch, awayMatchTeamId, homeMatchTeamId));

                foreach (var innings in auditableMatch.MatchInnings)
                {
                    await InsertMatchInnings(auditableMatch.MatchId.Value, innings, transaction).ConfigureAwait(false);
                }
            }

            var redacted = _copier.CreateRedactedCopy(auditableMatch);
            await _auditRepository.CreateAudit(new AuditRecord
            {
                Action = AuditAction.Create,
                MemberKey = memberKey,
                ActorName = memberName,
                EntityUri = auditableMatch.EntityUri,
                State = JsonConvert.SerializeObject(auditableMatch),
                RedactedState = JsonConvert.SerializeObject(redacted),
                AuditDate = DateTime.UtcNow
            }, transaction).ConfigureAwait(false);

            _logger.Info(LoggingTemplates.Created, redacted, memberName, memberKey, GetType(), nameof(SqlServerMatchRepository.CreateMatch));

            return auditableMatch;
        }

        private static async Task InsertMatchInnings(Guid matchId, MatchInnings innings, IDbTransaction transaction)
        {
            innings.MatchInningsId = innings.MatchInningsId ?? Guid.NewGuid();

            await transaction.Connection.ExecuteAsync($@"INSERT INTO {Tables.MatchInnings} 
							(MatchInningsId, MatchId, BattingMatchTeamId, BowlingMatchTeamId, InningsOrderInMatch)
							VALUES (@MatchInningsId, @MatchId, @BattingMatchTeamId, @BowlingMatchTeamId, @InningsOrderInMatch)",
                new
                {
                    innings.MatchInningsId,
                    MatchId = matchId,
                    innings.BattingMatchTeamId,
                    innings.BowlingMatchTeamId,
                    innings.InningsOrderInMatch
                },
                transaction).ConfigureAwait(false);

            await InsertOverSets(innings, transaction).ConfigureAwait(false);
        }

        private static async Task InsertOverSets(MatchInnings innings, IDbTransaction transaction)
        {
            for (var i = 0; i < innings.OverSets.Count; i++)
            {
                innings.OverSets[i].OverSetId = Guid.NewGuid();
                await transaction.Connection.ExecuteAsync($"INSERT INTO {Tables.OverSet} (OverSetId, MatchInningsId, OverSetNumber, Overs, BallsPerOver) VALUES (@OverSetId, @MatchInningsId, @OverSetNumber, @Overs, @BallsPerOver)",
                    new
                    {
                        innings.OverSets[i].OverSetId,
                        innings.MatchInningsId,
                        OverSetNumber = i + 1,
                        innings.OverSets[i].Overs,
                        innings.OverSets[i].BallsPerOver
                    },
                    transaction).ConfigureAwait(false);
            }
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

            var auditableMatch = _copier.CreateAuditableCopy(match);
            auditableMatch.UpdateMatchNameAutomatically = string.IsNullOrEmpty(auditableMatch.MatchName);
            auditableMatch.MatchNotes = _htmlSanitiser.Sanitize(auditableMatch.MatchNotes);

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    await PopulateTeamNames(auditableMatch, transaction).ConfigureAwait(false);
                    await UpdateMatchRoute(auditableMatch, match.MatchRoute, transaction).ConfigureAwait(false);

                    if (auditableMatch.UpdateMatchNameAutomatically)
                    {
                        auditableMatch.MatchName = _matchNameBuilder.BuildMatchName(auditableMatch);
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
                        auditableMatch.MatchName,
                        auditableMatch.UpdateMatchNameAutomatically,
                        auditableMatch.MatchLocation?.MatchLocationId,
                        StartTime = TimeZoneInfo.ConvertTimeToUtc(auditableMatch.StartTime.DateTime, TimeZoneInfo.FindSystemTimeZoneById(UkTimeZone())),
                        auditableMatch.StartTimeIsKnown,
                        auditableMatch.Season?.SeasonId,
                        auditableMatch.MatchNotes,
                        MatchResultType = auditableMatch.MatchResultType?.ToString(),
                        auditableMatch.MatchRoute,
                        auditableMatch.MatchId
                    }, transaction).ConfigureAwait(false);


                    var currentTeams = await connection.QueryAsync<MatchTeamResult>(
                            $@"SELECT MatchTeamId, TeamId, TeamRole FROM {Tables.MatchTeam} WHERE MatchId = @MatchId", new { auditableMatch.MatchId }, transaction
                        ).ConfigureAwait(false);

                    if (auditableMatch.MatchType == MatchType.TrainingSession)
                    {
                        // Team added
                        foreach (var team in auditableMatch.Teams)
                        {
                            if (!currentTeams.Any(x => x.TeamId == team.Team.TeamId))
                            {
                                team.MatchTeamId = Guid.NewGuid();
                                await connection.ExecuteAsync($@"INSERT INTO {Tables.MatchTeam} 
								(MatchTeamId, MatchId, TeamId, TeamRole) VALUES (@MatchTeamId, @MatchId, @TeamId, @TeamRole)",
                                    new
                                    {
                                        team.MatchTeamId,
                                        auditableMatch.MatchId,
                                        team.Team.TeamId,
                                        TeamRole = team.TeamRole.ToString()
                                    },
                                    transaction).ConfigureAwait(false);
                            }
                        }

                        // Team removed?
                        foreach (var team in currentTeams.Where(x => !auditableMatch.Teams.Where(t => t.Team.TeamId.HasValue).Select(t => t.Team.TeamId!.Value).Contains(x.TeamId!.Value)))
                        {
                            await transaction.Connection.ExecuteAsync($"DELETE FROM { Tables.MatchTeam } WHERE MatchTeamId = @MatchTeamId", new { team.MatchTeamId }, transaction).ConfigureAwait(false);
                        }
                    }
                    else
                    {

                        foreach (var team in auditableMatch.Teams)
                        {
                            var currentTeamInRole = currentTeams.SingleOrDefault(x => x.TeamRole == team.TeamRole);

                            // Team added
                            if (currentTeamInRole == null)
                            {
                                team.MatchTeamId = Guid.NewGuid();
                                await connection.ExecuteAsync($@"INSERT INTO {Tables.MatchTeam} 
								(MatchTeamId, MatchId, TeamId, TeamRole) VALUES (@MatchTeamId, @MatchId, @TeamId, @TeamRole)",
                                    new
                                    {
                                        team.MatchTeamId,
                                        auditableMatch.MatchId,
                                        team.Team.TeamId,
                                        TeamRole = team.TeamRole.ToString()
                                    },
                                    transaction).ConfigureAwait(false);
                            }
                            // Team changed
                            else if (currentTeamInRole.TeamId != team.Team.TeamId)
                            {
                                await connection.ExecuteAsync($"UPDATE {Tables.MatchTeam} SET TeamId = @TeamId WHERE MatchTeamId = @MatchTeamId",
                                new
                                {
                                    team.Team.TeamId,
                                    currentTeamInRole.MatchTeamId
                                },
                                transaction).ConfigureAwait(false);
                            }
                        }

                        // Team removed?
                        await RemoveTeamIfRequired(TeamRole.Home, currentTeams, auditableMatch.Teams, transaction).ConfigureAwait(false);
                        await RemoveTeamIfRequired(TeamRole.Away, currentTeams, auditableMatch.Teams, transaction).ConfigureAwait(false);

                        // Update innings with the new values for match team ids (assuming the match hasn't happened yet, 
                        // therefore the innings order is home bats first as assumed in CreateMatch)
                        await connection.ExecuteAsync($@"UPDATE { Tables.MatchInnings } SET
                                BattingMatchTeamId = (SELECT MatchTeamId FROM { Tables.MatchTeam } WHERE MatchId = @MatchId AND TeamRole = '{TeamRole.Home.ToString()}'), 
                                BowlingMatchTeamId = (SELECT MatchTeamId FROM { Tables.MatchTeam } WHERE MatchId = @MatchId AND TeamRole = '{TeamRole.Away.ToString()}')
                                WHERE MatchId = @MatchId AND (InningsOrderInMatch % 2 = 1)",
                            new
                            {
                                auditableMatch.MatchId,
                            },
                            transaction).ConfigureAwait(false);

                        await connection.ExecuteAsync($@"UPDATE {Tables.MatchInnings} SET
                                BattingMatchTeamId = (SELECT MatchTeamId FROM { Tables.MatchTeam } WHERE MatchId = @MatchId AND TeamRole = '{TeamRole.Away.ToString()}'), 
                                BowlingMatchTeamId = (SELECT MatchTeamId FROM { Tables.MatchTeam } WHERE MatchId = @MatchId AND TeamRole = '{TeamRole.Home.ToString()}')
                                WHERE MatchId = @MatchId AND (InningsOrderInMatch % 2 = 0)",
                            new
                            {
                                auditableMatch.MatchId,
                            },
                            transaction).ConfigureAwait(false);
                    }

                    if (match.MatchRoute != auditableMatch.MatchRoute)
                    {
                        await _redirectsRepository.InsertRedirect(match.MatchRoute, auditableMatch.MatchRoute, null, transaction).ConfigureAwait(false);
                    }

                    var redacted = _copier.CreateRedactedCopy(auditableMatch);
                    await _auditRepository.CreateAudit(new AuditRecord
                    {
                        Action = AuditAction.Update,
                        MemberKey = memberKey,
                        ActorName = memberName,
                        EntityUri = auditableMatch.EntityUri,
                        State = JsonConvert.SerializeObject(auditableMatch),
                        RedactedState = JsonConvert.SerializeObject(redacted),
                        AuditDate = DateTime.UtcNow
                    }, transaction).ConfigureAwait(false);

                    transaction.Commit();

                    _logger.Info(LoggingTemplates.Updated, redacted, memberName, memberKey, GetType(), nameof(SqlServerMatchRepository.UpdateMatch));
                }
            }

            return auditableMatch;
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

        public async Task<Match> UpdateMatchFormat(Match match, Guid memberKey, string memberName)
        {
            if (match is null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            if (match.MatchId is null)
            {
                throw new ArgumentException($"{nameof(match)} must have a MatchId");
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            var auditableMatch = _copier.CreateAuditableCopy(match);
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    if (match.MatchInnings[0].OverSets.FirstOrDefault()?.Overs != null)
                    {
                        await connection.ExecuteAsync($"UPDATE {Tables.OverSet} SET Overs = @Overs WHERE MatchInningsId IN @MatchInningsIds",
                            new
                            {
                                match.MatchInnings[0].OverSets[0].Overs,
                                MatchInningsIds = match.MatchInnings.Select(x => x.MatchInningsId).OfType<Guid>()
                            },
                            transaction).ConfigureAwait(false);
                    }

                    var currentMatchInnings = await connection.QueryAsync<Guid>($"SELECT MatchInningsId FROM {Tables.MatchInnings} WHERE MatchId = @MatchId", new { match.MatchId }, transaction).ConfigureAwait(false);

                    var deletedMatchInnings = currentMatchInnings.Where(x => !match.MatchInnings.Select(mi => mi.MatchInningsId).Contains(x));
                    if (deletedMatchInnings.Any())
                    {
                        await connection.ExecuteAsync($"DELETE FROM {Tables.OverSet} WHERE MatchInningsId IN @deletedMatchInnings", new { deletedMatchInnings }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"DELETE FROM {Tables.MatchInnings} WHERE MatchInningsId IN @deletedMatchInnings", new { deletedMatchInnings }, transaction).ConfigureAwait(false);
                    }

                    var addedMatchInnings = match.MatchInnings.Where(x => !x.MatchInningsId.HasValue || !currentMatchInnings.Contains(x.MatchInningsId.Value));
                    if (addedMatchInnings.Any())
                    {
                        foreach (var innings in addedMatchInnings)
                        {
                            await InsertMatchInnings(match.MatchId.Value, innings, transaction).ConfigureAwait(false);
                        }
                    }

                    var redacted = _copier.CreateRedactedCopy(auditableMatch);
                    await _auditRepository.CreateAudit(new AuditRecord
                    {
                        Action = AuditAction.Update,
                        MemberKey = memberKey,
                        ActorName = memberName,
                        EntityUri = auditableMatch.EntityUri,
                        State = JsonConvert.SerializeObject(auditableMatch),
                        RedactedState = JsonConvert.SerializeObject(redacted),
                        AuditDate = DateTime.UtcNow
                    }, transaction).ConfigureAwait(false);

                    transaction.Commit();

                    _logger.Info(LoggingTemplates.Updated, redacted, memberName, memberKey, GetType(), nameof(UpdateMatchFormat));
                }
            }

            return auditableMatch;
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

            if (match.MatchId is null)
            {
                throw new ArgumentException($"{nameof(match)} must have a MatchId");
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            var auditableMatch = _copier.CreateAuditableCopy(match);

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var beforeUpdate = await connection.QuerySingleAsync<Match>(
                        $@"SELECT MatchResultType, UpdateMatchNameAutomatically, MatchName, MatchRoute, StartTime
                            FROM {Tables.Match} 
                            WHERE MatchId = @MatchId",
                        new { auditableMatch.MatchId },
                        transaction).ConfigureAwait(false);

                    auditableMatch.StartTime = beforeUpdate.StartTime;
                    auditableMatch.MatchName = beforeUpdate.MatchName;

                    // the route might change if teams were missing and are only now being added
                    await UpdateMatchRoute(auditableMatch, beforeUpdate.MatchRoute, transaction).ConfigureAwait(false);

                    if (!auditableMatch.MatchResultType.HasValue && beforeUpdate.MatchResultType.HasValue &&
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
                            auditableMatch.MatchLocation?.MatchLocationId,
                            auditableMatch.InningsOrderIsKnown,
                            auditableMatch.MatchRoute,
                            auditableMatch.MatchId
                        }, transaction).ConfigureAwait(false);
                    }
                    else
                    {
                        // safe to update result type and name
                        if (auditableMatch.UpdateMatchNameAutomatically)
                        {
                            auditableMatch.MatchName = _matchNameBuilder.BuildMatchName(auditableMatch);
                        }
                        else
                        {
                            auditableMatch.MatchName = beforeUpdate.MatchName;
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
                            auditableMatch.MatchName,
                            auditableMatch.MatchLocation?.MatchLocationId,
                            auditableMatch.InningsOrderIsKnown,
                            MatchResultType = auditableMatch.MatchResultType?.ToString(),
                            auditableMatch.MatchRoute,
                            auditableMatch.MatchId
                        }, transaction).ConfigureAwait(false);
                    }

                    foreach (var team in auditableMatch.Teams)
                    {
                        if (team.MatchTeamId.HasValue)
                        {
                            await connection.ExecuteAsync($"UPDATE {Tables.MatchTeam} SET WonToss = @WonToss WHERE MatchTeamId = @MatchTeamId",
                            new
                            {
                                team.WonToss,
                                team.MatchTeamId
                            },
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
                                     auditableMatch.MatchId,
                                     team.Team.TeamId,
                                     team.TeamRole,
                                     team.WonToss
                                 },
                                 transaction).ConfigureAwait(false);

                            // You cannot set the order of innings in a match until both teams are fixed, so this is the last point where 
                            // we know that, in the database, match.InningsOrderIsKnown is false and the assumption is that the home team 
                            // batted first. Update the MatchInnings accordingly so that the MatchTeamIds are in there, knowing that the 
                            // innings order may be changed a few lines later.
                            var oddInnings = auditableMatch.MatchInnings.Where(x => x.InningsOrderInMatch % 2 == 1);
                            var evenInnings = auditableMatch.MatchInnings.Where(x => x.InningsOrderInMatch % 2 == 0);
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

                    if (auditableMatch.InningsOrderIsKnown)
                    {
                        // All teams and innings should now have match team ids to work with
                        var battedFirst = auditableMatch.Teams.Single(x => x.BattedFirst == true).MatchTeamId;
                        var shouldBeOddInnings = auditableMatch.MatchInnings.Where(x => x.BattingMatchTeamId == battedFirst);
                        var shouldBeEvenInnings = auditableMatch.MatchInnings.Where(x => x.BattingMatchTeamId != battedFirst);
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

                        foreach (var innings in auditableMatch.MatchInnings)
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

                    if (beforeUpdate.MatchRoute != auditableMatch.MatchRoute)
                    {
                        await _redirectsRepository.InsertRedirect(beforeUpdate.MatchRoute, auditableMatch.MatchRoute, null, transaction).ConfigureAwait(false);
                    }

                    var playerStatistics = _playerInMatchStatisticsBuilder.BuildStatisticsForMatch(auditableMatch);
                    await _statisticsRepository.DeletePlayerStatistics(auditableMatch.MatchId!.Value, transaction).ConfigureAwait(false);
                    await _statisticsRepository.UpdatePlayerStatistics(playerStatistics, transaction).ConfigureAwait(false);

                    var redacted = _copier.CreateRedactedCopy(auditableMatch);
                    await _auditRepository.CreateAudit(new AuditRecord
                    {
                        Action = AuditAction.Update,
                        MemberKey = memberKey,
                        ActorName = memberName,
                        EntityUri = auditableMatch.EntityUri,
                        State = JsonConvert.SerializeObject(auditableMatch),
                        RedactedState = JsonConvert.SerializeObject(redacted),
                        AuditDate = DateTime.UtcNow
                    }, transaction).ConfigureAwait(false);

                    transaction.Commit();

                    _logger.Info(LoggingTemplates.Updated, redacted, memberName, memberKey, GetType(), nameof(SqlServerMatchRepository.UpdateStartOfPlay));
                }

            }

            return auditableMatch;
        }

        /// <summary>
        /// Updates the batting scorecard for a single innings of a match
        /// </summary>
        public async Task<MatchInnings> UpdateBattingScorecard(Match match, Guid matchInningsId, Guid memberKey, string memberName)
        {
            if (match is null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            if (match.MatchId is null)
            {
                throw new ArgumentException($"{nameof(match)} must have a MatchId");
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            var auditableMatch = _copier.CreateAuditableCopy(match);
            var auditableInnings = auditableMatch.MatchInnings.SingleOrDefault(x => x.MatchInningsId == matchInningsId);

            if (auditableInnings is null) { throw new ArgumentException($"MatchInningsId {matchInningsId} did not match an innings of match {match.MatchId}", nameof(matchInningsId)); }
            if (auditableInnings?.BattingTeam?.Team?.TeamId is null) { throw new ArgumentException($"{nameof(match)} must have a {nameof(Team.TeamId)} for the batting team in innings {matchInningsId}", nameof(match)); }
            if (auditableInnings?.BowlingTeam?.Team?.TeamId is null) { throw new ArgumentException($"{nameof(match)} must have a {nameof(Team.TeamId)} for the bowling team in innings {matchInningsId}", nameof(match)); }

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    // Select existing innings and work out which ones have changed.
                    var matchInningsBefore = await _dapperWrapper.QuerySingleAsync<(int? Byes, int? Wides, int? NoBalls, int? BonusOrPenaltyRuns, int? Runs, int? Wickets)>(
                        $"SELECT Byes, Wides, NoBalls, BonusOrPenaltyRuns, Runs, Wickets FROM {Tables.MatchInnings} WHERE MatchInningsId = @MatchInningsId",
                        new { auditableInnings.MatchInningsId }, transaction).ConfigureAwait(false);

                    var matchInningsTotalsHaveChanged = matchInningsBefore.Byes != auditableInnings.Byes ||
                                                        matchInningsBefore.Wides != auditableInnings.Wides ||
                                                        matchInningsBefore.NoBalls != auditableInnings.NoBalls ||
                                                        matchInningsBefore.BonusOrPenaltyRuns != auditableInnings.BonusOrPenaltyRuns ||
                                                        matchInningsBefore.Runs != auditableInnings.Runs ||
                                                        matchInningsBefore.Wickets != auditableInnings.Wickets;

                    var playerInningsBefore = await _dapperWrapper.QueryAsync<PlayerInnings, PlayerIdentity, PlayerIdentity, PlayerIdentity, PlayerInnings>(
                        $@"SELECT i.PlayerInningsId, i.BattingPosition, i.DismissalType, i.RunsScored, i.BallsFaced,
                               bat.PlayerIdentityName,
                               field.PlayerIdentityName,
                               bowl.PlayerIdentityName
                               FROM {Tables.PlayerInnings} i 
                               INNER JOIN {Tables.PlayerIdentity} bat ON i.BatterPlayerIdentityId = bat.PlayerIdentityId
                               LEFT JOIN {Tables.PlayerIdentity} field ON i.DismissedByPlayerIdentityId = field.PlayerIdentityId
                               LEFT JOIN {Tables.PlayerIdentity} bowl ON i.BowlerPlayerIdentityId = bowl.PlayerIdentityId
                               WHERE i.MatchInningsId = @MatchInningsId",
                        (playerInnings, batter, fielder, bowler) =>
                        {
                            playerInnings.Batter = batter;
                            playerInnings.DismissedBy = fielder;
                            playerInnings.Bowler = bowler;
                            return playerInnings;
                        },
                           new { auditableInnings.MatchInningsId },
                           transaction,
                           splitOn: "PlayerIdentityName, PlayerIdentityName, PlayerIdentityName").ConfigureAwait(false);

                    // Ensure every player innings submitted has a batting position and player identities have their ids
                    for (var i = 0; i < auditableInnings.PlayerInnings.Count; i++)
                    {
                        var playerInnings = auditableInnings.PlayerInnings[i];

                        playerInnings.BattingPosition = i + 1;

                        if (playerInnings.Batter != null && !string.IsNullOrWhiteSpace(playerInnings.Batter.PlayerIdentityName))
                        {
                            playerInnings.Batter.Team = auditableInnings.BattingTeam.Team;
                            playerInnings.Batter = await _playerRepository.CreateOrMatchPlayerIdentity(playerInnings.Batter, memberKey, memberName, transaction).ConfigureAwait(false);
                        }

                        if (playerInnings.DismissedBy != null)
                        {
                            playerInnings.DismissedBy.Team = auditableInnings.BowlingTeam.Team;
                            playerInnings.DismissedBy = await _playerRepository.CreateOrMatchPlayerIdentity(playerInnings.DismissedBy, memberKey, memberName, transaction).ConfigureAwait(false);
                        }

                        if (playerInnings.Bowler != null)
                        {
                            playerInnings.Bowler.Team = auditableInnings.BowlingTeam.Team;
                            playerInnings.Bowler = await _playerRepository.CreateOrMatchPlayerIdentity(playerInnings.Bowler, memberKey, memberName, transaction).ConfigureAwait(false);
                        }
                    }

                    // Batting scorecard can also add wicket-taking bowlers, so make sure they have ids too
                    foreach (var figures in auditableInnings.BowlingFigures)
                    {
                        figures.Bowler.Team = auditableInnings.BowlingTeam.Team;
                        figures.Bowler = await _playerRepository.CreateOrMatchPlayerIdentity(figures.Bowler, memberKey, memberName, transaction).ConfigureAwait(false);
                    }

                    var comparison = _battingScorecardComparer.CompareScorecards(playerInningsBefore, auditableInnings.PlayerInnings);

                    // Now got lists of:
                    // - unchanged innings 
                    // - new innings
                    // - changed innings
                    // - deleted innings
                    // - affected players from the new/changed/deleted lists

                    var playerInningsHaveChanged = comparison.PlayerInningsAdded.Any() || comparison.PlayerInningsChanged.Any() || comparison.PlayerInningsRemoved.Any();
                    if (playerInningsHaveChanged || matchInningsTotalsHaveChanged)
                    {
                        await _statisticsRepository.DeletePlayerStatistics(auditableMatch.MatchId!.Value, transaction).ConfigureAwait(false);
                    }

                    if (playerInningsHaveChanged)
                    {
                    foreach (var playerInnings in comparison.PlayerInningsAdded)
                    {
                        playerInnings.PlayerInningsId = Guid.NewGuid();
                            await _dapperWrapper.ExecuteAsync($@"INSERT INTO {Tables.PlayerInnings} 
                                (PlayerInningsId, BattingPosition, MatchInningsId, BatterPlayerIdentityId, DismissalType, DismissedByPlayerIdentityId, BowlerPlayerIdentityId, RunsScored, BallsFaced) 
                                VALUES 
                                (@PlayerInningsId, @BattingPosition, @MatchInningsId, @BatterPlayerIdentityId, @DismissalType, @DismissedByPlayerIdentityId, @BowlerPlayerIdentityId, @RunsScored, @BallsFaced)",
                            new
                            {
                                playerInnings.PlayerInningsId,
                                playerInnings.BattingPosition,
                                auditableInnings.MatchInningsId,
                                BatterPlayerIdentityId = playerInnings.Batter.PlayerIdentityId,
                                DismissalType = playerInnings.DismissalType?.ToString(),
                                DismissedByPlayerIdentityId = playerInnings.DismissedBy?.PlayerIdentityId,
                                BowlerPlayerIdentityId = playerInnings.Bowler?.PlayerIdentityId,
                                playerInnings.RunsScored,
                                playerInnings.BallsFaced
                            }, transaction).ConfigureAwait(false);

                    }

                    foreach (var (before, after) in comparison.PlayerInningsChanged)
                    {
                        after.PlayerInningsId = before.PlayerInningsId;
                            await _dapperWrapper.ExecuteAsync($@"UPDATE {Tables.PlayerInnings} SET 
                                BattingPosition = @BattingPosition,
                                BatterPlayerIdentityId = @BatterPlayerIdentityId,
                                DismissalType = @DismissalType,
                                DismissedByPlayerIdentityId = @DismissedByPlayerIdentityId,
                                BowlerPlayerIdentityId = @BowlerPlayerIdentityId,
                                RunsScored = @RunsScored,
                                BallsFaced = @BallsFaced
                                WHERE PlayerInningsId = @PlayerInningsId",
                            new
                            {
                                after.BattingPosition,
                                BatterPlayerIdentityId = after.Batter.PlayerIdentityId,
                                DismissalType = after.DismissalType?.ToString(),
                                DismissedByPlayerIdentityId = after.DismissedBy?.PlayerIdentityId,
                                BowlerPlayerIdentityId = after.Bowler?.PlayerIdentityId,
                                after.RunsScored,
                                after.BallsFaced,
                                after.PlayerInningsId
                            }, transaction).ConfigureAwait(false);
                    }

                        // Deleting removed player innings only works because we already deleted them from the statistics table
                        await _dapperWrapper.ExecuteAsync($"DELETE FROM {Tables.PlayerInnings} WHERE PlayerInningsId IN @PlayerInningsIds", new { PlayerInningsIds = comparison.PlayerInningsRemoved.Select(x => x.PlayerInningsId) }, transaction).ConfigureAwait(false);
                    }

                    // Update the extras and final score
                    if (matchInningsTotalsHaveChanged)
                    {
                        await _dapperWrapper.ExecuteAsync(
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
                            auditableInnings.Byes,
                            auditableInnings.Wides,
                            auditableInnings.NoBalls,
                            auditableInnings.BonusOrPenaltyRuns,
                            auditableInnings.Runs,
                            auditableInnings.Wickets,
                            auditableInnings.MatchInningsId
                        },
                        transaction).ConfigureAwait(false);
                    }

                    // Update the number of players per team if it has grown
                    await _dapperWrapper.ExecuteAsync(
                         $@"UPDATE {Tables.Match} SET 
                                PlayersPerTeam = @PlayersPerTeam 
                                WHERE MatchId = (SELECT MatchId FROM {Tables.MatchInnings} WHERE MatchInningsId = @MatchInningsId)
                                AND @PlayersPerTeam > PlayersPerTeam",
                        new
                        {
                            PlayersPerTeam = auditableInnings.PlayerInnings.Count,
                            auditableInnings.MatchInningsId
                        },
                        transaction).ConfigureAwait(false);


                    if (playerInningsHaveChanged)
                    {
                        var bowlingFiguresHaveChanged = comparison.PlayerInningsAdded.Any(x => x.Bowler != null) ||
                            comparison.PlayerInningsChanged.Any(x => x.Item1.Bowler?.PlayerIdentityId != x.Item2.Bowler?.PlayerIdentityId) ||
                            comparison.PlayerInningsRemoved.Any(x => x.Bowler != null);
                        if (bowlingFiguresHaveChanged)
                        {
                    await _statisticsRepository.DeleteBowlingFigures(auditableInnings.MatchInningsId!.Value, transaction).ConfigureAwait(false);
                    await _statisticsRepository.UpdateBowlingFigures(auditableInnings, memberKey, memberName, transaction).ConfigureAwait(false);
                        }
                    }

                    if (playerInningsHaveChanged || matchInningsTotalsHaveChanged)
                    {
                        var playerStatistics = _playerInMatchStatisticsBuilder.BuildStatisticsForMatch(auditableMatch);
                    await _statisticsRepository.UpdatePlayerStatistics(playerStatistics, transaction).ConfigureAwait(false);
                    }

                    if (playerInningsHaveChanged || matchInningsTotalsHaveChanged)
                    {
                    var serialisedInnings = JsonConvert.SerializeObject(auditableInnings);
                    await _auditRepository.CreateAudit(new AuditRecord
                    {
                        Action = AuditAction.Update,
                        MemberKey = memberKey,
                        ActorName = memberName,
                        EntityUri = auditableInnings.EntityUri,
                        State = serialisedInnings,
                        RedactedState = serialisedInnings,
                        AuditDate = DateTime.UtcNow
                    }, transaction).ConfigureAwait(false);

                    _logger.Info(LoggingTemplates.Updated, auditableInnings, memberName, memberKey, GetType(), nameof(UpdateBattingScorecard));
                }

                    transaction.Commit();
            }
            }

            return auditableInnings;
        }

        /// <summary>
        /// Updates the bowling scorecard for a single innings of a match
        /// </summary>
        public async Task<MatchInnings> UpdateBowlingScorecard(Match match, Guid matchInningsId, Guid memberKey, string memberName)
        {
            if (match is null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            if (match.MatchId is null)
            {
                throw new ArgumentException($"{nameof(match)} must have a MatchId");
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            var auditableMatch = _copier.CreateAuditableCopy(match);
            var auditableInnings = auditableMatch.MatchInnings.SingleOrDefault(x => x.MatchInningsId == matchInningsId);

            if (auditableInnings is null) { throw new ArgumentException($"MatchInningsId {matchInningsId} did not match an innings of match {match.MatchId}", nameof(matchInningsId)); }
            if (auditableInnings?.BowlingTeam?.Team?.TeamId is null) { throw new ArgumentException($"{nameof(match)} must have a {nameof(Team.TeamId)} for the bowling team in innings {matchInningsId}", nameof(match)); }

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    // Select existing overs and work out which ones have changed.
                    var oversBefore = await _dapperWrapper.QueryAsync<Over, OverSet, PlayerIdentity, Over>(
                        $@"SELECT o.OverId, o.OverNumber, o.BallsBowled, o.NoBalls, o.Wides, o.RunsConceded,
                               o.OverSetId,
                               p.PlayerIdentityId, p.PlayerIdentityName
                               FROM {Tables.Over} o INNER JOIN {Tables.PlayerIdentity} p ON o.BowlerPlayerIdentityId = p.PlayerIdentityId
                               WHERE o.MatchInningsId = @MatchInningsId",
                        (over, overSet, playerIdentity) =>
                        {
                            over.OverSet = overSet;
                            over.Bowler = playerIdentity;
                            return over;
                        },
                           new { auditableInnings.MatchInningsId },
                           transaction,
                           splitOn: "OverSetId, PlayerIdentityId").ConfigureAwait(false);

                    // Ensure every over is numbered, and every player identity has its id
                    for (var i = 0; i < auditableInnings.OversBowled.Count; i++)
                    {
                        var over = auditableInnings.OversBowled[i];

                        over.OverNumber = i + 1;

                        over.OverId = Guid.NewGuid();
                        over.Bowler.Team = auditableInnings.BowlingTeam.Team;
                        over.Bowler = await _playerRepository.CreateOrMatchPlayerIdentity(over.Bowler, memberKey, memberName, transaction).ConfigureAwait(false);
                    }

                    foreach (var figures in auditableInnings.BowlingFigures)
                    {
                        if (!figures.Bowler.PlayerIdentityId.HasValue)
                        {
                            figures.Bowler = auditableInnings.OversBowled.First(o => o.Bowler.ComparableName() == figures.Bowler.ComparableName()).Bowler;
                        }
                    }

                    var comparison = _bowlingScorecardComparer.CompareScorecards(oversBefore, auditableInnings.OversBowled);

                    // Now got lists of:
                    // - unchanged overs 
                    // - new overs
                    // - changed overs
                    // - deleted overs
                    // - affected players from the new/changed/deleted lists

                    if (comparison.OversAdded.Any() || comparison.OversChanged.Any() || comparison.OversRemoved.Any())
                    {

                        var previousOverSetIds = (await _dapperWrapper.QueryAsync<Guid>($"SELECT OverSetId FROM {Tables.OverSet} WHERE MatchInningsId = @MatchInningsId", new { auditableInnings.MatchInningsId }, transaction).ConfigureAwait(false)).ToList();
                    await InsertOverSets(auditableInnings, transaction).ConfigureAwait(false);

                    foreach (var over in comparison.OversAdded)
                    {
                            await _dapperWrapper.ExecuteAsync($@"INSERT INTO {Tables.Over} 
                                (OverId, OverNumber, MatchInningsId, BowlerPlayerIdentityId, OverSetId, BallsBowled, NoBalls, Wides, RunsConceded) 
                                VALUES 
                                (@OverId, @OverNumber, @MatchInningsId, @BowlerPlayerIdentityId, @OverSetId, @BallsBowled, @NoBalls, @Wides, @RunsConceded)",
                            new
                            {
                                over.OverId,
                                over.OverNumber,
                                auditableInnings.MatchInningsId,
                                BowlerPlayerIdentityId = over.Bowler.PlayerIdentityId,
                                _oversHelper.OverSetForOver(auditableInnings.OverSets, over.OverNumber)?.OverSetId,
                                over.BallsBowled,
                                over.NoBalls,
                                over.Wides,
                                over.RunsConceded,
                            }, transaction).ConfigureAwait(false);

                    }

                    foreach (var (before, after) in comparison.OversChanged)
                    {
                        after.OverId = before.OverId;
                            await _dapperWrapper.ExecuteAsync($@"UPDATE {Tables.Over} SET 
                                OverNumber = @OverNumber,
                                BowlerPlayerIdentityId = @BowlerPlayerIdentityId,
                                OverSetId = @OverSetId,
                                BallsBowled = @BallsBowled,
                                NoBalls = @NoBalls,
                                Wides = @Wides,
                                RunsConceded = @RunsConceded
                                WHERE OverId = @OverId",
                            new
                            {
                                after.OverNumber,
                                BowlerPlayerIdentityId = after.Bowler.PlayerIdentityId,
                                _oversHelper.OverSetForOver(auditableInnings.OverSets, after.OverNumber)?.OverSetId,
                                after.BallsBowled,
                                after.NoBalls,
                                after.Wides,
                                after.RunsConceded,
                                after.OverId
                            }, transaction).ConfigureAwait(false);

                    }

                        await _dapperWrapper.ExecuteAsync($"DELETE FROM {Tables.Over} WHERE OverId IN @OverIds", new { OverIds = comparison.OversRemoved.Select(x => x.OverId) }, transaction).ConfigureAwait(false);

                    // What about unchanged overs? They may have an OverSetId but we've just recreated the OverSets, so it needs to be updated
                    foreach (var over in comparison.OversUnchanged)
                    {
                            await _dapperWrapper.ExecuteAsync($"UPDATE {Tables.Over} SET OverSetId = @OverSetId WHERE OverId = @OverId", new { _oversHelper.OverSetForOver(auditableInnings.OverSets, over.OverNumber)?.OverSetId, over.OverId }, transaction).ConfigureAwait(false);
                    }

                    // Now the previous over sets can be removed, because the references from Tables.Over should be gone
                    if (previousOverSetIds.Count > 0)
                    {
                            await _dapperWrapper.ExecuteAsync($"DELETE FROM {Tables.OverSet} WHERE OverSetId IN @previousOverSetIds", new { previousOverSetIds }, transaction).ConfigureAwait(false);
                    }

                    var playerStatistics = _playerInMatchStatisticsBuilder.BuildStatisticsForMatch(auditableMatch);

                    await _statisticsRepository.DeletePlayerStatistics(auditableMatch.MatchId!.Value, transaction).ConfigureAwait(false);
                    await _statisticsRepository.DeleteBowlingFigures(auditableInnings.MatchInningsId!.Value, transaction).ConfigureAwait(false);
                    await _statisticsRepository.UpdateBowlingFigures(auditableInnings, memberKey, memberName, transaction).ConfigureAwait(false);
                    await _statisticsRepository.UpdatePlayerStatistics(playerStatistics, transaction).ConfigureAwait(false);

                    var serialisedInnings = JsonConvert.SerializeObject(auditableInnings);
                    await _auditRepository.CreateAudit(new AuditRecord
                    {
                        Action = AuditAction.Update,
                        MemberKey = memberKey,
                        ActorName = memberName,
                        EntityUri = auditableInnings.EntityUri,
                        State = serialisedInnings,
                        RedactedState = serialisedInnings,
                        AuditDate = DateTime.UtcNow
                    }, transaction).ConfigureAwait(false);

                        _logger.Info(LoggingTemplates.Updated, auditableInnings, memberName, memberKey, GetType(), nameof(SqlServerMatchRepository.UpdateBowlingScorecard));
                    }

                    transaction.Commit();
                }

                }

            return auditableInnings;
            }

            return auditableInnings;
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

            if (match.MatchId is null)
            {
                throw new ArgumentException($"{nameof(match)} must have a MatchId");
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            foreach (var award in match.Awards)
            {
                if (award.Award is null)
                {
                    throw new ArgumentException($"{nameof(award.Award)} cannot be null in a {typeof(MatchAward)}");
                }

                if (string.IsNullOrWhiteSpace(award.Award.AwardName))
                {
                    throw new ArgumentException($"{nameof(award.Award.AwardName)} cannot be null or empty in a {typeof(MatchAward)}");
                }

                if (award.PlayerIdentity is null)
                {
                    throw new ArgumentException($"{nameof(award.PlayerIdentity)} cannot be null in a {typeof(MatchAward)}");
                }

                if (award.PlayerIdentity.Team is null)
                {
                    throw new ArgumentException($"{nameof(award.PlayerIdentity.Team)} cannot be null in a {typeof(MatchAward)}");
                }

                if (!award.PlayerIdentity.Team.TeamId.HasValue)
                {
                    throw new ArgumentException($"{nameof(award.PlayerIdentity.Team.TeamId)} cannot be null in a {typeof(MatchAward)}");
                }
            }

            var auditableMatch = _copier.CreateAuditableCopy(match);

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var beforeUpdate = await _dapperWrapper.QuerySingleOrDefaultAsync<Match>(
                        $@"SELECT UpdateMatchNameAutomatically, MatchName
                            FROM {Tables.Match} 
                            WHERE MatchId = @MatchId",
                        new { auditableMatch.MatchId },
                        transaction).ConfigureAwait(false);

                    if (beforeUpdate is null) { throw new MatchNotFoundException(auditableMatch.MatchId!.Value); }

                    if (auditableMatch.UpdateMatchNameAutomatically)
                    {
                        auditableMatch.MatchName = _matchNameBuilder.BuildMatchName(auditableMatch);
                    }
                    else
                    {
                        auditableMatch.MatchName = beforeUpdate.MatchName;
                    }

                    await _dapperWrapper.ExecuteAsync($@"UPDATE {Tables.Match} SET
                            MatchName = @MatchName,                                
                            MatchResultType = @MatchResultType
                            WHERE MatchId = @MatchId",
                        new
                        {
                            auditableMatch.MatchName,
                            MatchResultType = auditableMatch.MatchResultType?.ToString(),
                            auditableMatch.MatchId
                        },
                        transaction).ConfigureAwait(false);


                    var awardsBefore = await _dapperWrapper.QueryAsync<(Guid playerIdentityId, string playerIdentityName, Guid teamId)>(
                            $@"SELECT pi.PlayerIdentityId, pi.PlayerIdentityName, pi.TeamId 
                                FROM {Tables.AwardedTo} a INNER JOIN {Tables.PlayerIdentity} pi ON a.PlayerIdentityId = pi.PlayerIdentityId 
                                WHERE a.MatchId = @MatchId",
                            new { auditableMatch.MatchId },
                            transaction).ConfigureAwait(false);

                    var playerIdentitiesWithAwardsBefore = awardsBefore.Select(x => x.playerIdentityId).ToList();
                    var playerIdentitiesAffectedByAwards = new List<(Guid playerIdentityId, string playerIdentityName, Guid teamId)>();

                    await _dapperWrapper.ExecuteAsync($"DELETE FROM {Tables.AwardedTo} WHERE MatchId = @MatchId", new { auditableMatch.MatchId }, transaction).ConfigureAwait(false);
                        foreach (var award in auditableMatch.Awards)
                        {
                        var awardId = await _dapperWrapper.QuerySingleOrDefaultAsync<Guid?>($"SELECT AwardId FROM {Tables.Award} WHERE AwardName = @AwardName", award.Award, transaction).ConfigureAwait(false);
                        if (!awardId.HasValue) { throw new AwardNotFoundException(award.Award!.AwardName!); }

                            if (!award.AwardedToId.HasValue)
                            {
                                award.AwardedToId = Guid.NewGuid();
                            }

                        if (!award.PlayerIdentity!.PlayerIdentityId.HasValue)
                            {
                                award.PlayerIdentity = await _playerRepository.CreateOrMatchPlayerIdentity(award.PlayerIdentity, memberKey, memberName, transaction).ConfigureAwait(false);
                            }

                        await _dapperWrapper.ExecuteAsync($@"INSERT INTO {Tables.AwardedTo} 
                                (AwardedToId, MatchId, AwardId, PlayerIdentityId, Reason)
                                VALUES (@AwardedToId, @MatchId, @awardId, @PlayerIdentityId, @Reason)",
                                    new
                                    {
                                        award.AwardedToId,
                                        auditableMatch.MatchId,
                                    awardId,
                                        award.PlayerIdentity.PlayerIdentityId,
                                        award.Reason
                                    },
                                    transaction).ConfigureAwait(false);

                            // If this is a new award, add to affected player identities
                            if (!playerIdentitiesWithAwardsBefore.Contains(award.PlayerIdentity.PlayerIdentityId!.Value))
                            {
                                playerIdentitiesAffectedByAwards.Add((award.PlayerIdentity.PlayerIdentityId.Value, award.PlayerIdentity.PlayerIdentityName, award.PlayerIdentity.Team.TeamId!.Value));
                            }
                        }

                    // If awards removed, add those player identities to affected identities
                    var playerIdentitiesRemoved = awardsBefore.Where(x => !auditableMatch.Awards.Select(award => award.PlayerIdentity.PlayerIdentityId).Contains(x.playerIdentityId));
                    playerIdentitiesAffectedByAwards.AddRange(playerIdentitiesRemoved);

                    var playerStatistics = _playerInMatchStatisticsBuilder.BuildStatisticsForMatch(auditableMatch);

                    await _statisticsRepository.DeletePlayerStatistics(auditableMatch.MatchId!.Value, transaction).ConfigureAwait(false);
                    await _statisticsRepository.UpdatePlayerStatistics(playerStatistics, transaction).ConfigureAwait(false);

                    var redacted = _copier.CreateRedactedCopy(auditableMatch);
                    await _auditRepository.CreateAudit(new AuditRecord
                    {
                        Action = AuditAction.Update,
                        MemberKey = memberKey,
                        ActorName = memberName,
                        EntityUri = auditableMatch.EntityUri,
                        State = JsonConvert.SerializeObject(auditableMatch),
                        RedactedState = JsonConvert.SerializeObject(redacted),
                        AuditDate = DateTime.UtcNow
                    }, transaction).ConfigureAwait(false);

                    transaction.Commit();

                    _logger.Info(LoggingTemplates.Updated, redacted, memberName, memberKey, GetType(), nameof(SqlServerMatchRepository.UpdateCloseOfPlay));
                }
            }

            return auditableMatch;
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
                baseRoute = string.Join(" ", match.Teams.OrderBy(x => x.TeamRole).Select(x => x.Team.TeamName).Take(2));
            }
            else if (!string.IsNullOrEmpty(match.MatchName))
            {
                baseRoute = match.MatchName;
            }
            else
            {
                baseRoute = "to-be-confirmed";
            }


            var generatedRoute = _routeGenerator.GenerateRoute("/matches", baseRoute + " " + match.StartTime.Date.ToString("dMMMyyyy", CultureInfo.CurrentCulture), NoiseWords.MatchRoute);
            if (string.IsNullOrEmpty(routeBeforeUpdate) || !_routeGenerator.IsMatchingRoute(routeBeforeUpdate, generatedRoute))
            {
                match.MatchRoute = generatedRoute;
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
                                                                                    FROM {Tables.Team} AS t INNER JOIN {Tables.TeamVersion} tn ON t.TeamId = tn.TeamId
                                                                                    WHERE t.TeamId IN @TeamIds
                                                                                    AND tn.TeamVersionId = (SELECT TOP 1 TeamVersionId FROM {Tables.TeamVersion} WHERE TeamId = t.TeamId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)",
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
                    await DeleteMatch(match, memberKey, memberName, transaction).ConfigureAwait(false);
                    transaction.Commit();
                }
            }
        }

        /// <summary>
        /// Deletes a stoolball match
        /// </summary>
        public async Task DeleteMatch(Match match, Guid memberKey, string memberName, IDbTransaction transaction)
        {
            if (match is null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            if (transaction is null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            var auditableMatch = _copier.CreateAuditableCopy(match);

            await transaction.Connection.ExecuteAsync($"DELETE FROM {Tables.PlayerInMatchStatistics} WHERE MatchId = @MatchId", new { auditableMatch.MatchId }, transaction).ConfigureAwait(false);
            await transaction.Connection.ExecuteAsync($"DELETE FROM {Tables.BowlingFigures} WHERE MatchInningsId IN (SELECT MatchInningsId FROM {Tables.MatchInnings} WHERE MatchId = @MatchId)", new { auditableMatch.MatchId }, transaction).ConfigureAwait(false);
            await transaction.Connection.ExecuteAsync($"DELETE FROM {Tables.Over} WHERE MatchInningsId IN (SELECT MatchInningsId FROM {Tables.MatchInnings} WHERE MatchId = @MatchId)", new { auditableMatch.MatchId }, transaction).ConfigureAwait(false);
            await transaction.Connection.ExecuteAsync($"DELETE FROM {Tables.PlayerInnings} WHERE MatchInningsId IN (SELECT MatchInningsId FROM {Tables.MatchInnings} WHERE MatchId = @MatchId)", new { auditableMatch.MatchId }, transaction).ConfigureAwait(false);
            await transaction.Connection.ExecuteAsync($"DELETE FROM {Tables.OverSet} WHERE MatchInningsId IN (SELECT MatchInningsId FROM {Tables.MatchInnings} WHERE MatchId = @MatchId)", new { auditableMatch.MatchId }, transaction).ConfigureAwait(false);
            await transaction.Connection.ExecuteAsync($"DELETE FROM {Tables.MatchInnings} WHERE MatchId = @MatchId", new { auditableMatch.MatchId }, transaction).ConfigureAwait(false);
            await transaction.Connection.ExecuteAsync($"DELETE FROM {Tables.MatchTeam} WHERE MatchId = @MatchId", new { auditableMatch.MatchId }, transaction).ConfigureAwait(false);
            await transaction.Connection.ExecuteAsync($"DELETE FROM {Tables.Comment} WHERE MatchId = @MatchId", new { auditableMatch.MatchId }, transaction).ConfigureAwait(false);
            await transaction.Connection.ExecuteAsync($"DELETE FROM {Tables.AwardedTo} WHERE MatchId = @MatchId", new { auditableMatch.MatchId }, transaction).ConfigureAwait(false);
            await transaction.Connection.ExecuteAsync($"DELETE FROM {Tables.Match} WHERE MatchId = @MatchId", new { auditableMatch.MatchId }, transaction).ConfigureAwait(false);

            await _redirectsRepository.DeleteRedirectsByDestinationPrefix(auditableMatch.MatchRoute, transaction).ConfigureAwait(false);

            var redacted = _copier.CreateRedactedCopy(auditableMatch);
            await _auditRepository.CreateAudit(new AuditRecord
            {
                Action = AuditAction.Delete,
                MemberKey = memberKey,
                ActorName = memberName,
                EntityUri = auditableMatch.EntityUri,
                State = JsonConvert.SerializeObject(auditableMatch),
                RedactedState = JsonConvert.SerializeObject(redacted),
                AuditDate = DateTime.UtcNow
            }, transaction).ConfigureAwait(false);

            _logger.Info(LoggingTemplates.Deleted, redacted, memberName, memberKey, GetType(), nameof(SqlServerMatchRepository.DeleteMatch));
        }
    }
}