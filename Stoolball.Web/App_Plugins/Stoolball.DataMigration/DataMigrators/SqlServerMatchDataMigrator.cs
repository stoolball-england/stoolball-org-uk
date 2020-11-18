using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Competitions;
using Stoolball.Data.SqlServer;
using Stoolball.Logging;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Routing;
using Stoolball.Security;
using Stoolball.Teams;
using static Stoolball.Data.SqlServer.Constants;
using Tables = Stoolball.Data.SqlServer.Constants.Tables;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class SqlServerMatchDataMigrator : IMatchDataMigrator
    {
        private readonly IRedirectsRepository _redirectsRepository;
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IAuditHistoryBuilder _auditHistoryBuilder;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger _logger;
        private readonly IRouteGenerator _routeGenerator;
        private readonly ISeasonEstimator _seasonEstimator;
        private readonly IDataRedactor _dataRedactor;

        public SqlServerMatchDataMigrator(IRedirectsRepository redirectsRepository, IDatabaseConnectionFactory databaseConnectionFactory, IAuditHistoryBuilder auditHistoryBuilder,
            IAuditRepository auditRepository, ILogger logger, IRouteGenerator routeGenerator, ISeasonEstimator seasonEstimator, IDataRedactor dataRedactor)
        {
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _auditHistoryBuilder = auditHistoryBuilder ?? throw new ArgumentNullException(nameof(auditHistoryBuilder));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
            _seasonEstimator = seasonEstimator ?? throw new ArgumentNullException(nameof(seasonEstimator));
            _dataRedactor = dataRedactor ?? throw new ArgumentNullException(nameof(dataRedactor));
        }

        /// <summary>
        /// Clear down all the match data ready for a fresh import
        /// </summary>
        /// <returns></returns>
        public async Task DeleteMatches()
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    await connection.ExecuteAsync($"DELETE FROM {Tables.MatchComment}", null, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.MatchInnings}", null, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.MatchTeam}", null, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.Match}", null, transaction).ConfigureAwait(false);

                    await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/matches/", transaction).ConfigureAwait(false);

                    transaction.Commit();
                }
            }
        }

        /// <summary>
        /// Save the supplied match to the database with its existing <see cref="Match.MatchId"/>
        /// </summary>
        public async Task<Match> MigrateMatch(MigratedMatch match)
        {
            if (match is null)
            {
                throw new System.ArgumentNullException(nameof(match));
            }

            var migratedMatch = CreateAuditableCopy(match);
            migratedMatch.MatchId = Guid.NewGuid();

            var seasonDates = _seasonEstimator.EstimateSeasonDates(migratedMatch.StartTime);
            migratedMatch.LastPlayerBatsOn = seasonDates.fromDate.Year != seasonDates.untilDate.Year;

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    string baseRoute = string.Empty;
                    if (migratedMatch.MigratedTeams.Count > 0)
                    {
                        var teamsWithNames = await connection.QueryAsync<MigratedTeamInMatch, Team, MigratedTeamInMatch>(
                                                                         $@"SELECT t.MigratedTeamId, t.TeamId, tn.TeamName FROM {Tables.Team} t
                                                                             INNER JOIN {Tables.TeamName} AS tn ON t.TeamId = tn.TeamId AND tn.UntilDate IS NULL 
                                                                             WHERE t.MigratedTeamId IN @MigratedTeamIds",
                                                                         (migratedTeam, team) =>
                                                                         {
                                                                             migratedTeam.Team = team;
                                                                             return migratedTeam;
                                                                         },
                                                                         new { MigratedTeamIds = match.MigratedTeams.Select(x => x.MigratedTeamId).ToList() },
                                                                         transaction,
                                                                         splitOn: "TeamId"
                                                                        ).ConfigureAwait(false);

                        foreach (var teamInMatch in migratedMatch.MigratedTeams)
                        {
                            teamInMatch.Team = teamsWithNames.Where(x => x.MigratedTeamId == teamInMatch.MigratedTeamId).Select(x => x.Team).Single();
                        }

                        baseRoute = string.Join(" ", migratedMatch.MigratedTeams.OrderBy(x => x.TeamRole).Select(x => x.Team.TeamName));
                    }
                    else
                    {
                        baseRoute = "unconfirmed";
                    }

                    migratedMatch.MatchRoute = _routeGenerator.GenerateRoute("/matches", baseRoute + " " + migratedMatch.StartTime.Date.ToString("dMMMyyyy", CultureInfo.CurrentCulture), NoiseWords.MatchRoute);
                    int count;
                    do
                    {
                        count = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Match} WHERE MatchRoute = @MatchRoute", new { migratedMatch.MatchRoute }, transaction).ConfigureAwait(false);
                        if (count > 0)
                        {
                            migratedMatch.MatchRoute = _routeGenerator.IncrementRoute(migratedMatch.MatchRoute);
                        }
                    }
                    while (count > 0);

                    _auditHistoryBuilder.BuildInitialAuditHistory(match, migratedMatch, nameof(SqlServerMatchDataMigrator), CreateRedactedCopy);
                    migratedMatch.MemberKey = migratedMatch.History.Count > 0 ? migratedMatch.History[0].MemberKey : null;

                    if (migratedMatch.MigratedMatchLocationId.HasValue)
                    {
                        migratedMatch.MatchLocation = new MatchLocation
                        {
                            MatchLocationId = await connection.ExecuteScalarAsync<Guid>($"SELECT MatchLocationId FROM {Tables.MatchLocation} WHERE MigratedMatchLocationId = @MigratedMatchLocationId", new { migratedMatch.MigratedMatchLocationId }, transaction).ConfigureAwait(false)
                        };
                    }

                    if (migratedMatch.MigratedTournamentId.HasValue)
                    {
                        migratedMatch.Tournament = new Tournament
                        {
                            TournamentId = await connection.ExecuteScalarAsync<Guid>($"SELECT TournamentId FROM {Tables.Tournament} WHERE MigratedTournamentId = @MigratedTournamentId", new { migratedMatch.MigratedTournamentId }, transaction).ConfigureAwait(false)
                        };
                    }

                    if (migratedMatch.MigratedSeasonIds.Count > 0)
                    {
                        migratedMatch.Season = new Season
                        {
                            SeasonId = await connection.ExecuteScalarAsync<Guid>($"SELECT SeasonId FROM {Tables.Season} WHERE MigratedSeasonId = @MigratedSeasonId", new { MigratedSeasonId = migratedMatch.MigratedSeasonIds.First() }, transaction).ConfigureAwait(false)
                        };
                    }

                    await connection.ExecuteAsync($@"INSERT INTO {Tables.Match}
						(MatchId, MigratedMatchId, MatchName, UpdateMatchNameAutomatically, MatchLocationId, MatchType, PlayerType, PlayersPerTeam, InningsOrderIsKnown,
						 LastPlayerBatsOn, EnableBonusOrPenaltyRuns, TournamentId, OrderInTournament, StartTime, StartTimeIsKnown, MatchResultType, MatchNotes, SeasonId, MatchRoute, MemberKey)
						VALUES (@MatchId, @MigratedMatchId, @MatchName, @UpdateMatchNameAutomatically, @MatchLocationId, @MatchType, @PlayerType, @PlayersPerTeam, @InningsOrderIsKnown, 
                        @LastPlayerBatsOn, @EnableBonusOrPenaltyRuns, @TournamentId, @OrderInTournament, @StartTime, @StartTimeIsKnown, @MatchResultType, @MatchNotes, @SeasonId, @MatchRoute, @MemberKey)",
                    new
                    {
                        migratedMatch.MatchId,
                        migratedMatch.MigratedMatchId,
                        migratedMatch.MatchName,
                        migratedMatch.UpdateMatchNameAutomatically,
                        migratedMatch.MatchLocation?.MatchLocationId,
                        MatchType = migratedMatch.MatchType.ToString(),
                        PlayerType = migratedMatch.PlayerType.ToString(),
                        migratedMatch.PlayersPerTeam,
                        migratedMatch.InningsOrderIsKnown,
                        migratedMatch.LastPlayerBatsOn,
                        migratedMatch.EnableBonusOrPenaltyRuns,
                        migratedMatch.Tournament?.TournamentId,
                        migratedMatch.OrderInTournament,
                        migratedMatch.StartTime,
                        migratedMatch.StartTimeIsKnown,
                        MatchResultType = migratedMatch.MatchResultType?.ToString(),
                        migratedMatch.MatchNotes,
                        migratedMatch.Season?.SeasonId,
                        migratedMatch.MatchRoute,
                        migratedMatch.MemberKey
                    }, transaction).ConfigureAwait(false);

                    Guid? homeMatchTeamId = null;
                    Guid? awayMatchTeamId = null;

                    foreach (var team in migratedMatch.MigratedTeams)
                    {
                        if (team.TeamRole == TeamRole.Home)
                        {
                            homeMatchTeamId = Guid.NewGuid();
                            team.MatchTeamId = homeMatchTeamId.Value;
                        }
                        else
                        {
                            awayMatchTeamId = Guid.NewGuid();
                            team.MatchTeamId = awayMatchTeamId.Value;
                        }

                        await connection.ExecuteAsync($@"INSERT INTO {Tables.MatchTeam} 
								(MatchTeamId, MigratedMatchTeamId, MatchId, TeamId, TeamRole, WonToss) VALUES (@MatchTeamId, @MigratedMatchTeamId, @MatchId, @TeamId, @TeamRole, @WonToss)",
                            new
                            {
                                team.MatchTeamId,
                                team.MigratedMatchTeamId,
                                migratedMatch.MatchId,
                                team.Team.TeamId,
                                TeamRole = team.TeamRole.ToString(),
                                team.WonToss
                            },
                            transaction).ConfigureAwait(false);
                    }

                    for (var i = 0; i < migratedMatch.MigratedMatchInnings.Count; i++)
                    {
                        var innings = migratedMatch.MigratedMatchInnings[i];
                        innings.MatchInningsId = Guid.NewGuid();
                        innings.BattingMatchTeamId = (i == 0) ? homeMatchTeamId : awayMatchTeamId;
                        innings.BowlingMatchTeamId = (i == 0) ? awayMatchTeamId : homeMatchTeamId;

                        await connection.ExecuteAsync($@"INSERT INTO {Tables.MatchInnings} 
								(MatchInningsId, MatchId, BattingMatchTeamId, BowlingMatchTeamId, InningsOrderInMatch, Overs, Byes, Wides, NoBalls, BonusOrPenaltyRuns, Runs, Wickets)
								VALUES (@MatchInningsId, @MatchId, @BattingMatchTeamId, @BowlingMatchTeamId, @InningsOrderInMatch, @Overs, @Byes, @Wides, @NoBalls, @BonusOrPenaltyRuns, @Runs, @Wickets)",
                            new
                            {
                                innings.MatchInningsId,
                                migratedMatch.MatchId,
                                innings.BattingMatchTeamId,
                                innings.BowlingMatchTeamId,
                                innings.InningsOrderInMatch,
                                innings.Overs,
                                innings.Byes,
                                innings.Wides,
                                innings.NoBalls,
                                innings.BonusOrPenaltyRuns,
                                innings.Runs,
                                innings.Wickets
                            },
                            transaction).ConfigureAwait(false);
                    }

                    await _redirectsRepository.InsertRedirect(match.MatchRoute, migratedMatch.MatchRoute, string.Empty, transaction).ConfigureAwait(false);
                    await _redirectsRepository.InsertRedirect(match.MatchRoute, migratedMatch.MatchRoute, "/statistics", transaction).ConfigureAwait(false);
                    await _redirectsRepository.InsertRedirect(match.MatchRoute, migratedMatch.MatchRoute, "/calendar.ics", transaction).ConfigureAwait(false);

                    foreach (var audit in migratedMatch.History)
                    {
                        await _auditRepository.CreateAudit(audit, transaction).ConfigureAwait(false);
                    }

                    transaction.Commit();

                    _logger.Info(GetType(), LoggingTemplates.Migrated, CreateRedactedCopy(migratedMatch), GetType(), nameof(MigrateMatch));
                }
            }

            return migratedMatch;
        }

        private MigratedMatch CreateRedactedCopy(MigratedMatch match)
        {
            var redacted = CreateAuditableCopy(match);
            redacted.MatchNotes = _dataRedactor.RedactPersonalData(match.MatchNotes);
            redacted.History.Clear();
            return redacted;
        }

        private static MigratedMatch CreateAuditableCopy(MigratedMatch match)
        {
            return new MigratedMatch
            {
                MatchId = match.MatchId,
                MigratedMatchId = match.MigratedMatchId,
                MatchName = match.MatchName?.Trim(),
                UpdateMatchNameAutomatically = match.UpdateMatchNameAutomatically,
                MigratedMatchLocationId = match.MigratedMatchLocationId,
                MatchType = match.MatchType,
                PlayerType = match.PlayerType,
                PlayersPerTeam = match.PlayersPerTeam,
                EnableBonusOrPenaltyRuns = true,
                MigratedMatchInnings = match.MigratedMatchInnings,
                InningsOrderIsKnown = match.InningsOrderIsKnown,
                MigratedTournamentId = match.MigratedTournamentId,
                OrderInTournament = match.OrderInTournament,
                StartTime = match.StartTime,
                StartTimeIsKnown = match.StartTimeIsKnown,
                MigratedTeams = match.MigratedTeams,
                MigratedSeasonIds = match.MigratedSeasonIds,
                MatchResultType = match.MatchResultType,
                MatchNotes = match.MatchNotes
            };
        }
    }
}
