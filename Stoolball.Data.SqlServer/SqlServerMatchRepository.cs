﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Ganss.XSS;
using Newtonsoft.Json;
using Stoolball.Competitions;
using Stoolball.Logging;
using Stoolball.Matches;
using Stoolball.MatchLocations;
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
        private readonly IDataRedactor _dataRedactor;
        private readonly IStatisticsRepository _statisticsRepository;
        private readonly IOversHelper _oversHelper;
        private readonly IPlayerInMatchStatisticsBuilder _playerInMatchStatisticsBuilder;

        public SqlServerMatchRepository(IDatabaseConnectionFactory databaseConnectionFactory, IAuditRepository auditRepository, ILogger logger, IRouteGenerator routeGenerator,
            IRedirectsRepository redirectsRepository, IHtmlSanitizer htmlSanitiser, IMatchNameBuilder matchNameBuilder, IPlayerTypeSelector playerTypeSelector,
            IBowlingScorecardComparer bowlingScorecardComparer, IBattingScorecardComparer battingScorecardComparer, IPlayerRepository playerRepository, IDataRedactor dataRedactor,
            IStatisticsRepository statisticsRepository, IOversHelper oversHelper, IPlayerInMatchStatisticsBuilder playerInMatchStatisticsBuilder)
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
            _dataRedactor = dataRedactor ?? throw new ArgumentNullException(nameof(dataRedactor));
            _statisticsRepository = statisticsRepository ?? throw new ArgumentNullException(nameof(statisticsRepository));
            _oversHelper = oversHelper ?? throw new ArgumentNullException(nameof(oversHelper));
            _playerInMatchStatisticsBuilder = playerInMatchStatisticsBuilder ?? throw new ArgumentNullException(nameof(playerInMatchStatisticsBuilder));
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

        private static Match CreateAuditableCopy(Match match)
        {
            return new Match
            {
                MatchId = match.MatchId,
                MatchName = match.MatchName,
                UpdateMatchNameAutomatically = match.UpdateMatchNameAutomatically,
                MatchType = match.MatchType,
                PlayerType = match.PlayerType,
                Tournament = match.Tournament != null ? new Tournament { TournamentId = match.Tournament.TournamentId } : null,
                MatchResultType = match.MatchResultType,
                Teams = match.Teams.Select(x => CreateAuditableCopy(x)).ToList(),
                MatchLocation = match.MatchLocation != null ? new MatchLocation { MatchLocationId = match.MatchLocation.MatchLocationId } : null,
                StartTime = match.StartTime,
                StartTimeIsKnown = match.StartTimeIsKnown,
                Season = match.Season != null ? new Season { SeasonId = match.Season.SeasonId } : null,
                PlayersPerTeam = match.PlayersPerTeam,
                InningsOrderIsKnown = match.InningsOrderIsKnown,
                LastPlayerBatsOn = match.LastPlayerBatsOn,
                EnableBonusOrPenaltyRuns = match.EnableBonusOrPenaltyRuns,
                MatchInnings = match.MatchInnings.Select(x => CreateAuditableCopy(x)).ToList(),
                Awards = match.Awards.Select(x => CreateAuditableCopy(x)).ToList(),
                MatchNotes = match.MatchNotes,
                MatchRoute = match.MatchRoute
            };
        }

        private static TeamInMatch CreateAuditableCopy(TeamInMatch teamInMatch)
        {
            return new TeamInMatch
            {
                MatchTeamId = teamInMatch.MatchTeamId,
                Team = new Team
                {
                    TeamId = teamInMatch.Team?.TeamId,
                    TeamName = teamInMatch.Team?.TeamName,
                    TeamRoute = teamInMatch.Team?.TeamRoute
                },
                TeamRole = teamInMatch.TeamRole,
                WonToss = teamInMatch.WonToss,
                BattedFirst = teamInMatch.BattedFirst
            };
        }

        private Match CreateRedactedCopy(Match match)
        {
            var redacted = CreateAuditableCopy(match);
            redacted.MatchNotes = _dataRedactor.RedactPersonalData(match.MatchNotes);
            return redacted;
        }

        private static MatchInnings CreateAuditableCopy(MatchInnings innings)
        {
            return new MatchInnings
            {
                MatchInningsId = innings.MatchInningsId,
                InningsOrderInMatch = innings.InningsOrderInMatch,
                BattingMatchTeamId = innings.BattingMatchTeamId,
                BowlingMatchTeamId = innings.BowlingMatchTeamId,
                PlayerInnings = innings.PlayerInnings.Select(x => new PlayerInnings
                {
                    PlayerInningsId = x.PlayerInningsId,
                    Batter = CreateAuditableCopy(x.Batter),
                    DismissedBy = x.DismissedBy != null ? CreateAuditableCopy(x.DismissedBy) : null,
                    Bowler = x.Bowler != null ? CreateAuditableCopy(x.Bowler) : null,
                    DismissalType = x.DismissalType,
                    RunsScored = x.RunsScored,
                    BallsFaced = x.BallsFaced
                }).ToList(),
                OversBowled = innings.OversBowled.Select(x => new Over
                {
                    OverId = x.OverId,
                    OverNumber = x.OverNumber,
                    Bowler = CreateAuditableCopy(x.Bowler),
                    BallsBowled = x.BallsBowled,
                    NoBalls = x.NoBalls,
                    Wides = x.Wides,
                    RunsConceded = x.RunsConceded
                }).ToList(),
                BowlingFigures = innings.BowlingFigures.Select(x => new BowlingFigures
                {
                    BowlingFiguresId = x.BowlingFiguresId,
                    Bowler = CreateAuditableCopy(x.Bowler),
                    Overs = x.Overs,
                    Maidens = x.Maidens,
                    RunsConceded = x.RunsConceded,
                    Wickets = x.Wickets
                }).ToList(),
                BattingTeam = innings.BattingTeam != null ? CreateAuditableCopy(innings.BattingTeam) : null,
                BowlingTeam = innings.BowlingTeam != null ? CreateAuditableCopy(innings.BowlingTeam) : null,
                OverSets = innings.OverSets,
                Byes = innings.Byes,
                NoBalls = innings.NoBalls,
                Wides = innings.Wides,
                BonusOrPenaltyRuns = innings.BonusOrPenaltyRuns,
                Runs = innings.Runs,
                Wickets = innings.Wickets
            };
        }

        private static MatchAward CreateAuditableCopy(MatchAward award)
        {
            return new MatchAward
            {
                AwardedToId = award.AwardedToId,
                Award = award.Award,
                PlayerIdentity = CreateAuditableCopy(award.PlayerIdentity),
                Reason = award.Reason
            };
        }

        private static PlayerIdentity CreateAuditableCopy(PlayerIdentity playerIdentity)
        {
            var copy = new PlayerIdentity
            {
                PlayerIdentityId = playerIdentity.PlayerIdentityId,
                PlayerIdentityName = playerIdentity.PlayerIdentityName,
                Team = new Team
                {
                    TeamId = playerIdentity.Team?.TeamId
                }
            };
            if (playerIdentity.Player != null)
            {
                copy.Player = new Player
                {
                    PlayerId = playerIdentity.Player.PlayerId,
                    PlayerRoute = playerIdentity.Player.PlayerRoute
                };
            }
            return copy;
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

            var auditableMatch = CreateAuditableCopy(match);
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
            if (auditableMatch.Season != null)
            {
                var unprocessedSeasons = (await transaction.Connection.QueryAsync<Season, Competition, OverSet, Season>(
                     $@"SELECT s.SeasonId, s.PlayersPerTeam, s.EnableLastPlayerBatsOn, s.EnableBonusOrPenaltyRuns, 
                                    co.PlayerType,
                                    os.Overs, os.BallsPerOver
                                    FROM {Tables.Season} AS s INNER JOIN {Tables.Competition} co ON s.CompetitionId = co.CompetitionId 
                                    LEFT JOIN {Tables.OverSet} os ON s.SeasonId = os.SeasonId
                                    WHERE s.SeasonId = @SeasonId
                                    ORDER BY os.OverSetNumber",
                        (season, competition, overSet) =>
                        {
                            season.Competition = competition;
                            if (overSet != null) { season.DefaultOverSets.Add(overSet); }
                            return season;
                        },
                        new { auditableMatch.Season.SeasonId },
                        transaction,
                        splitOn: "PlayerType, Overs"
                    ).ConfigureAwait(false));
                auditableMatch.Season = unprocessedSeasons.First();
                auditableMatch.Season.DefaultOverSets = unprocessedSeasons.Select(season => season.DefaultOverSets.FirstOrDefault()).OfType<OverSet>().ToList();
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
                StartTime = TimeZoneInfo.ConvertTimeToUtc(auditableMatch.StartTime.DateTime, TimeZoneInfo.FindSystemTimeZoneById(UkTimeZone)),
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
                Guid matchTeamId;
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
                var defaultOverSets = new List<OverSet> { new OverSet { Overs = 12, BallsPerOver = 8 } }; // default if none provided

                auditableMatch.MatchInnings.Add(new MatchInnings
                {
                    MatchInningsId = Guid.NewGuid(),
                    BattingMatchTeamId = homeMatchTeamId,
                    BowlingMatchTeamId = awayMatchTeamId,
                    InningsOrderInMatch = 1,
                    OverSets = auditableMatch.Tournament?.DefaultOverSets ?? auditableMatch.Season?.DefaultOverSets ?? defaultOverSets
                });

                auditableMatch.MatchInnings.Add(new MatchInnings
                {
                    MatchInningsId = Guid.NewGuid(),
                    BattingMatchTeamId = awayMatchTeamId,
                    BowlingMatchTeamId = homeMatchTeamId,
                    InningsOrderInMatch = 2,
                    OverSets = auditableMatch.Tournament?.DefaultOverSets ?? auditableMatch.Season?.DefaultOverSets ?? defaultOverSets
                });

                foreach (var innings in auditableMatch.MatchInnings)
                {
                    await transaction.Connection.ExecuteAsync($@"INSERT INTO {Tables.MatchInnings} 
							(MatchInningsId, MatchId, BattingMatchTeamId, BowlingMatchTeamId, InningsOrderInMatch)
							VALUES (@MatchInningsId, @MatchId, @BattingMatchTeamId, @BowlingMatchTeamId, @InningsOrderInMatch)",
                        new
                        {
                            innings.MatchInningsId,
                            auditableMatch.MatchId,
                            innings.BattingMatchTeamId,
                            innings.BowlingMatchTeamId,
                            innings.InningsOrderInMatch
                        },
                        transaction).ConfigureAwait(false);

                    await InsertOverSets(innings, transaction).ConfigureAwait(false);
                }
            }

            var redacted = CreateRedactedCopy(auditableMatch);
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

            _logger.Info(GetType(), LoggingTemplates.Created, redacted, memberName, memberKey, GetType(), nameof(SqlServerMatchRepository.CreateMatch));

            return auditableMatch;
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

            var auditableMatch = CreateAuditableCopy(match);
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
                        StartTime = TimeZoneInfo.ConvertTimeToUtc(auditableMatch.StartTime.DateTime, TimeZoneInfo.FindSystemTimeZoneById(UkTimeZone)),
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
                        foreach (var team in currentTeams.Where(x => !auditableMatch.Teams.Select(t => t.Team.TeamId.Value).Contains(x.TeamId.Value)))
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
                                WHERE MatchId = @MatchId AND InningsOrderInMatch = 1",
                            new
                            {
                                auditableMatch.MatchId,
                            },
                            transaction).ConfigureAwait(false);

                        await connection.ExecuteAsync($@"UPDATE {Tables.MatchInnings} SET
                                BattingMatchTeamId = (SELECT MatchTeamId FROM { Tables.MatchTeam } WHERE MatchId = @MatchId AND TeamRole = '{TeamRole.Away.ToString()}'), 
                                BowlingMatchTeamId = (SELECT MatchTeamId FROM { Tables.MatchTeam } WHERE MatchId = @MatchId AND TeamRole = '{TeamRole.Home.ToString()}')
                                WHERE MatchId = @MatchId AND InningsOrderInMatch = 2",
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

                    var redacted = CreateRedactedCopy(auditableMatch);
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

                    _logger.Info(GetType(), LoggingTemplates.Updated, redacted, memberName, memberKey, GetType(), nameof(SqlServerMatchRepository.UpdateMatch));
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

            var auditableMatch = CreateAuditableCopy(match);

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
                    await _statisticsRepository.DeletePlayerStatistics(auditableMatch.MatchId.Value, transaction).ConfigureAwait(false);
                    await _statisticsRepository.UpdatePlayerStatistics(playerStatistics, transaction).ConfigureAwait(false);

                    var redacted = CreateRedactedCopy(auditableMatch);
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

                    _logger.Info(GetType(), LoggingTemplates.Updated, redacted, memberName, memberKey, GetType(), nameof(SqlServerMatchRepository.UpdateStartOfPlay));
                }

            }

            return auditableMatch;
        }

        /// <summary>
        /// Updates the battings scorecard for a single innings of a match
        /// </summary>
        public async Task<MatchInnings> UpdateBattingScorecard(Match match, Guid matchInningsId, Guid memberKey, string memberName)
        {
            if (match is null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            if (memberName is null)
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            var auditableMatch = CreateAuditableCopy(match);
            var auditableInnings = auditableMatch.MatchInnings.Single(x => x.MatchInningsId == matchInningsId);

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

                    var comparison = _battingScorecardComparer.CompareScorecards(inningsBefore, auditableInnings.PlayerInnings);

                    // Now got lists of:
                    // - unchanged innings 
                    // - new innings
                    // - changed innings
                    // - deleted innings
                    // - affected players from the new/changed/deleted lists

                    foreach (var playerInnings in comparison.PlayerInningsAdded)
                    {
                        playerInnings.PlayerInningsId = Guid.NewGuid();
                        await connection.ExecuteAsync($@"INSERT INTO {Tables.PlayerInnings} 
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
                        await connection.ExecuteAsync($@"UPDATE {Tables.PlayerInnings} SET 
                                BatterPlayerIdentityId = @BatterPlayerIdentityId,
                                DismissalType = @DismissalType,
                                DismissedByPlayerIdentityId = @DismissedByPlayerIdentityId,
                                BowlerPlayerIdentityId = @BowlerPlayerIdentityId,
                                RunsScored = @RunsScored,
                                BallsFaced = @BallsFaced
                                WHERE PlayerInningsId = @PlayerInningsId",
                            new
                            {
                                BatterPlayerIdentityId = after.Batter.PlayerIdentityId,
                                DismissalType = after.DismissalType?.ToString(),
                                DismissedByPlayerIdentityId = after.DismissedBy?.PlayerIdentityId,
                                BowlerPlayerIdentityId = after.Bowler?.PlayerIdentityId,
                                after.RunsScored,
                                after.BallsFaced,
                                after.PlayerInningsId
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
                            auditableInnings.Byes,
                            auditableInnings.Wides,
                            auditableInnings.NoBalls,
                            auditableInnings.BonusOrPenaltyRuns,
                            auditableInnings.Runs,
                            auditableInnings.Wickets,
                            auditableInnings.MatchInningsId
                        },
                        transaction).ConfigureAwait(false);

                    // Update the number of players per team
                    await connection.ExecuteAsync(
                         $@"UPDATE {Tables.Match} SET 
                                PlayersPerTeam = @PlayersPerTeam 
                                WHERE MatchId = (SELECT MatchId FROM {Tables.MatchInnings} WHERE MatchInningsId = @MatchInningsId)",
                        new
                        {
                            PlayersPerTeam = auditableInnings.PlayerInnings.Count,
                            auditableInnings.MatchInningsId
                        },
                        transaction).ConfigureAwait(false);

                    var playerStatistics = _playerInMatchStatisticsBuilder.BuildStatisticsForMatch(auditableMatch);

                    await _statisticsRepository.DeletePlayerStatistics(auditableMatch.MatchId.Value, transaction).ConfigureAwait(false);
                    await _statisticsRepository.DeleteBowlingFigures(auditableInnings.MatchInningsId.Value, transaction).ConfigureAwait(false);
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

                    transaction.Commit();

                    _logger.Info(GetType(), LoggingTemplates.Updated, auditableInnings, memberName, memberKey, GetType(), nameof(UpdateBattingScorecard));
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

            if (memberName is null)
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            var auditableMatch = CreateAuditableCopy(match);
            var auditableInnings = auditableMatch.MatchInnings.Single(x => x.MatchInningsId == matchInningsId);

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    // Select existing overs and work out which ones have changed.
                    var oversBefore = await connection.QueryAsync<Over, OverSet, PlayerIdentity, Over>(
                        $@"SELECT o.OverId, o.OverNumber, o.BallsBowled, o.NoBalls, o.Wides, o.RunsConceded,
                               o.OverSetId,
                               p.PlayerIdentityName
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
                           splitOn: "OverSetId, PlayerIdentityName").ConfigureAwait(false);

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

                    var previousOverSetIds = (await connection.QueryAsync<Guid>($"SELECT OverSetId FROM {Tables.OverSet} WHERE MatchInningsId = @MatchInningsId", new { auditableInnings.MatchInningsId }, transaction).ConfigureAwait(false)).ToList();
                    await InsertOverSets(auditableInnings, transaction).ConfigureAwait(false);

                    foreach (var over in comparison.OversAdded)
                    {
                        await connection.ExecuteAsync($@"INSERT INTO {Tables.Over} 
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
                        await connection.ExecuteAsync($@"UPDATE {Tables.Over} SET 
                                BowlerPlayerIdentityId = @BowlerPlayerIdentityId,
                                OverSetId = @OverSetId,
                                BallsBowled = @BallsBowled,
                                NoBalls = @NoBalls,
                                Wides = @Wides,
                                RunsConceded = @RunsConceded
                                WHERE OverId = @OverId",
                            new
                            {
                                BowlerPlayerIdentityId = after.Bowler.PlayerIdentityId,
                                _oversHelper.OverSetForOver(auditableInnings.OverSets, after.OverNumber)?.OverSetId,
                                after.BallsBowled,
                                after.NoBalls,
                                after.Wides,
                                after.RunsConceded,
                                after.OverId
                            }, transaction).ConfigureAwait(false);

                    }

                    await connection.ExecuteAsync($"DELETE FROM {Tables.Over} WHERE OverId IN @OverIds", new { OverIds = comparison.OversRemoved.Select(x => x.OverId) }, transaction).ConfigureAwait(false);

                    // What about unchanged overs? They may have an OverSetId but we've just recreated the OverSets, so it needs to be updated
                    foreach (var over in comparison.OversUnchanged)
                    {
                        await connection.ExecuteAsync($"UPDATE {Tables.Over} SET OverSetId = @OverSetId WHERE OverId = @OverId", new { _oversHelper.OverSetForOver(auditableInnings.OverSets, over.OverNumber)?.OverSetId, over.OverId }, transaction).ConfigureAwait(false);
                    }

                    // Now the previous over sets can be removed, because the references from Tables.Over should be gone
                    if (previousOverSetIds.Count > 0)
                    {
                        await connection.ExecuteAsync($"DELETE FROM {Tables.OverSet} WHERE OverSetId IN @previousOverSetIds", new { previousOverSetIds }, transaction).ConfigureAwait(false);
                    }

                    var playerStatistics = _playerInMatchStatisticsBuilder.BuildStatisticsForMatch(auditableMatch);

                    await _statisticsRepository.DeletePlayerStatistics(auditableMatch.MatchId.Value, transaction).ConfigureAwait(false);
                    await _statisticsRepository.DeleteBowlingFigures(auditableInnings.MatchInningsId.Value, transaction).ConfigureAwait(false);
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

                    transaction.Commit();

                    _logger.Info(GetType(), LoggingTemplates.Updated, auditableInnings, memberName, memberKey, GetType(), nameof(SqlServerMatchRepository.UpdateBowlingScorecard));
                }

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

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            var auditableMatch = CreateAuditableCopy(match);

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var beforeUpdate = await connection.QuerySingleAsync<Match>(
                        $@"SELECT UpdateMatchNameAutomatically, MatchName
                            FROM {Tables.Match} 
                            WHERE MatchId = @MatchId",
                        new { auditableMatch.MatchId },
                        transaction).ConfigureAwait(false);

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
                            MatchResultType = @MatchResultType
                            WHERE MatchId = @MatchId",
                        new
                        {
                            auditableMatch.MatchName,
                            MatchResultType = auditableMatch.MatchResultType?.ToString(),
                            auditableMatch.MatchId
                        },
                        transaction).ConfigureAwait(false);

                    await connection.ExecuteAsync($"DELETE FROM {Tables.AwardedTo} WHERE MatchId = @MatchId", new { auditableMatch.MatchId }, transaction).ConfigureAwait(false);
                    var awardId = await connection.QuerySingleOrDefaultAsync<Guid?>($"SELECT AwardId FROM {Tables.Award} WHERE AwardName = 'Player of the match'", null, transaction).ConfigureAwait(false);
                    if (awardId.HasValue)
                    {
                        foreach (var award in auditableMatch.Awards)
                        {
                            if (!award.AwardedToId.HasValue)
                            {
                                award.AwardedToId = Guid.NewGuid();
                            }

                            if (award.PlayerIdentity == null)
                            {
                                throw new ArgumentException($"{nameof(award.PlayerIdentity)} cannot be null in a {typeof(MatchAward)}");
                            }

                            if (award.PlayerIdentity.Team == null)
                            {
                                throw new ArgumentException($"{nameof(award.PlayerIdentity.Team)} cannot be null in a {typeof(MatchAward)}");
                            }

                            if (!award.PlayerIdentity.Team.TeamId.HasValue)
                            {
                                throw new ArgumentException($"{nameof(award.PlayerIdentity.Team.TeamId)} cannot be null in a {typeof(MatchAward)}");
                            }

                            if (!award.PlayerIdentity.PlayerIdentityId.HasValue)
                            {
                                award.PlayerIdentity = await _playerRepository.CreateOrMatchPlayerIdentity(award.PlayerIdentity, memberKey, memberName, transaction).ConfigureAwait(false);
                            }

                            await connection.ExecuteAsync($@"INSERT INTO {Tables.AwardedTo} 
                                (AwardedToId, MatchId, AwardId, PlayerIdentityId, Reason)
                                VALUES (@AwardedToId, @MatchId, '{awardId}', @PlayerIdentityId, @Reason)",
                                    new
                                    {
                                        award.AwardedToId,
                                        auditableMatch.MatchId,
                                        award.PlayerIdentity.PlayerIdentityId,
                                        award.Reason
                                    },
                                    transaction).ConfigureAwait(false);
                        }
                    }

                    var playerStatistics = _playerInMatchStatisticsBuilder.BuildStatisticsForMatch(auditableMatch);

                    await _statisticsRepository.DeletePlayerStatistics(auditableMatch.MatchId.Value, transaction).ConfigureAwait(false);
                    await _statisticsRepository.UpdatePlayerStatistics(playerStatistics, transaction).ConfigureAwait(false);

                    var redacted = CreateRedactedCopy(auditableMatch);
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

                    _logger.Info(GetType(), LoggingTemplates.Updated, redacted, memberName, memberKey, GetType(), nameof(SqlServerMatchRepository.UpdateCloseOfPlay));
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

            var auditableMatch = CreateAuditableCopy(match);

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

            var redacted = CreateRedactedCopy(auditableMatch);
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

            _logger.Info(GetType(), LoggingTemplates.Deleted, redacted, memberName, memberKey, GetType(), nameof(SqlServerMatchRepository.DeleteMatch));
        }
    }
}