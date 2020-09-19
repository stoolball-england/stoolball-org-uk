using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Routing;
using Stoolball.Teams;
using Stoolball.Umbraco.Data;
using Stoolball.Umbraco.Data.Audit;
using Stoolball.Umbraco.Data.Redirects;
using Umbraco.Core.Logging;
using static Stoolball.Umbraco.Data.Constants;
using Tables = Stoolball.Umbraco.Data.Constants.Tables;

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

        public SqlServerMatchDataMigrator(IRedirectsRepository redirectsRepository, IDatabaseConnectionFactory databaseConnectionFactory, IAuditHistoryBuilder auditHistoryBuilder,
            IAuditRepository auditRepository, ILogger logger, IRouteGenerator routeGenerator, ISeasonEstimator seasonEstimator)
        {
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _auditHistoryBuilder = auditHistoryBuilder ?? throw new ArgumentNullException(nameof(auditHistoryBuilder));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
            _seasonEstimator = seasonEstimator ?? throw new ArgumentNullException(nameof(seasonEstimator));
        }

        /// <summary>
        /// Clear down all the match data ready for a fresh import
        /// </summary>
        /// <returns></returns>
        public async Task DeleteMatches()
        {
            try
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
                        transaction.Commit();
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error<SqlServerMatchDataMigrator>(e);
                throw;
            }

            await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/matches/").ConfigureAwait(false);
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

            var migratedMatch = new MigratedMatch
            {
                MatchId = Guid.NewGuid(),
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

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();

                string baseRoute = string.Empty;
                if (migratedMatch.MigratedTeams.Count > 0)
                {
                    var teamNames = await connection.QueryAsync<string>($@"SELECT tn.TeamName FROM {Tables.Team} t
                                                                        INNER JOIN {Tables.TeamName} AS tn ON t.TeamId = tn.TeamId AND tn.UntilDate IS NULL 
                                                                        WHERE t.MigratedTeamId IN @MigratedTeamIds",
                                                                        new { MigratedTeamIds = match.MigratedTeams.Select(x => x.MigratedTeamId).ToList() }
                                                                    ).ConfigureAwait(false);
                    baseRoute = string.Join(" ", teamNames);
                }
                else
                {
                    baseRoute = "unconfirmed";
                }

                migratedMatch.MatchRoute = _routeGenerator.GenerateRoute("/matches", baseRoute + " " + migratedMatch.StartTime.Date.ToString("dMMMyyyy", CultureInfo.CurrentCulture), NoiseWords.MatchRoute);
                int count;
                do
                {
                    count = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Match} WHERE MatchRoute = @MatchRoute", new { migratedMatch.MatchRoute }).ConfigureAwait(false);
                    if (count > 0)
                    {
                        migratedMatch.MatchRoute = _routeGenerator.IncrementRoute(migratedMatch.MatchRoute);
                    }
                }
                while (count > 0);
            }

            _auditHistoryBuilder.BuildInitialAuditHistory(match, migratedMatch, nameof(SqlServerMatchDataMigrator));
            try
            {
                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {

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

                        var seasonDates = _seasonEstimator.EstimateSeasonDates(migratedMatch.StartTime);
                        var lastPlayerBatsOn = seasonDates.fromDate.Year != seasonDates.untilDate.Year;

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
                            LastPlayerBatsOn = lastPlayerBatsOn,
                            migratedMatch.EnableBonusOrPenaltyRuns,
                            migratedMatch.Tournament?.TournamentId,
                            migratedMatch.OrderInTournament,
                            migratedMatch.StartTime,
                            migratedMatch.StartTimeIsKnown,
                            MatchResultType = migratedMatch.MatchResultType?.ToString(),
                            migratedMatch.MatchNotes,
                            migratedMatch.Season?.SeasonId,
                            migratedMatch.MatchRoute,
                            MemberKey = migratedMatch.History.Count > 0 ? migratedMatch.History[0].MemberKey : null
                        }, transaction).ConfigureAwait(false);

                        Guid? homeMatchTeamId = null;
                        Guid? awayMatchTeamId = null;

                        foreach (var team in migratedMatch.MigratedTeams)
                        {
                            team.Team = new Team
                            {
                                TeamId = await connection.ExecuteScalarAsync<Guid>($"SELECT TeamId FROM {Tables.Team} WHERE MigratedTeamId = @MigratedTeamId", new { team.MigratedTeamId }, transaction).ConfigureAwait(false)
                            };

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
								(MatchTeamId, MigratedMatchTeamId, MatchId, TeamId, TeamRole, WonToss) VALUES (@MatchTeamId, @MigratedMatchTeamId, @MatchId, @TeamId, @TeamRole, @WonToss)",
                                new
                                {
                                    MatchTeamId = matchTeamId,
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

                            await connection.ExecuteAsync($@"INSERT INTO {Tables.MatchInnings} 
								(MatchInningsId, MatchId, BattingMatchTeamId, BowlingMatchTeamId, InningsOrderInMatch, Overs, Byes, Wides, NoBalls, BonusOrPenaltyRuns, Runs, Wickets)
								VALUES (@MatchInningsId, @MatchId, @BattingMatchTeamId, @BowlingMatchTeamId, @InningsOrderInMatch, @Overs, @Byes, @Wides, @NoBalls, @BonusOrPenaltyRuns, @Runs, @Wickets)",
                                new
                                {
                                    MatchInningsId = Guid.NewGuid(),
                                    migratedMatch.MatchId,
                                    BattingMatchTeamId = (i == 0) ? homeMatchTeamId : awayMatchTeamId,
                                    BowlingMatchTeamId = (i == 0) ? awayMatchTeamId : homeMatchTeamId,
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

                        transaction.Commit();
                    }

                }
            }
            catch (Exception e)
            {
                _logger.Error<SqlServerMatchDataMigrator>(e);
                throw;
            }

            await _redirectsRepository.InsertRedirect(match.MatchRoute, migratedMatch.MatchRoute, string.Empty).ConfigureAwait(false);
            await _redirectsRepository.InsertRedirect(match.MatchRoute, migratedMatch.MatchRoute, "/statistics").ConfigureAwait(false);
            await _redirectsRepository.InsertRedirect(match.MatchRoute, migratedMatch.MatchRoute, "/calendar.ics").ConfigureAwait(false);

            foreach (var audit in migratedMatch.History)
            {
                await _auditRepository.CreateAudit(audit).ConfigureAwait(false);
            }

            return migratedMatch;
        }
    }
}
