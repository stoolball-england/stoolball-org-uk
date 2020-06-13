using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Routing;
using Stoolball.Teams;
using Stoolball.Umbraco.Data.Audit;
using Stoolball.Umbraco.Data.Redirects;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using Umbraco.Core.Scoping;
using static Stoolball.Umbraco.Data.Constants;
using Tables = Stoolball.Umbraco.Data.Constants.Tables;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class SqlServerMatchDataMigrator : IMatchDataMigrator
    {
        private readonly IRedirectsRepository _redirectsRepository;
        private readonly IScopeProvider _scopeProvider;
        private readonly IAuditHistoryBuilder _auditHistoryBuilder;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger _logger;
        private readonly IRouteGenerator _routeGenerator;

        public SqlServerMatchDataMigrator(IRedirectsRepository redirectsRepository, IScopeProvider scopeProvider, IAuditHistoryBuilder auditHistoryBuilder,
            IAuditRepository auditRepository, ILogger logger, IRouteGenerator routeGenerator)
        {
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
            _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
            _auditHistoryBuilder = auditHistoryBuilder ?? throw new ArgumentNullException(nameof(auditHistoryBuilder));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
        }

        /// <summary>
        /// Clear down all the match data ready for a fresh import
        /// </summary>
        /// <returns></returns>
        public async Task DeleteMatches()
        {
            try
            {
                using (var scope = _scopeProvider.CreateScope())
                {
                    var database = scope.Database;

                    using (var transaction = database.GetTransaction())
                    {
                        await database.ExecuteAsync($"DELETE FROM {Tables.MatchComment}").ConfigureAwait(false);
                        await database.ExecuteAsync($"DELETE FROM {Tables.MatchInnings}").ConfigureAwait(false);
                        await database.ExecuteAsync($"DELETE FROM {Tables.MatchTeam}").ConfigureAwait(false);
                        await database.ExecuteAsync($"DELETE FROM {Tables.Match}").ConfigureAwait(false);
                        transaction.Complete();
                    }

                    scope.Complete();
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

            using (var scope = _scopeProvider.CreateScope())
            {
                string baseRoute = string.Empty;
                if (migratedMatch.MigratedTeams.Count > 0)
                {
                    var teamNames = await scope.Database.QueryAsync<string>($"SELECT TeamName FROM {Tables.Team} WHERE MigratedTeamId IN @MigratedTeamIds", new { MigratedTeamIds = match.MigratedTeams.Select(x => x.MigratedTeamId) }).ConfigureAwait(false);
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
                    count = await scope.Database.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Match} WHERE MatchRoute = @MatchRoute", new { migratedMatch.MatchRoute }).ConfigureAwait(false);
                    if (count > 0)
                    {
                        migratedMatch.MatchRoute = _routeGenerator.IncrementRoute(migratedMatch.MatchRoute);
                    }
                }
                while (count > 0);
                scope.Complete();
            }

            _auditHistoryBuilder.BuildInitialAuditHistory(match, migratedMatch, nameof(SqlServerMatchDataMigrator));

            using (var scope = _scopeProvider.CreateScope())
            {
                try
                {
                    var database = scope.Database;
                    using (var transaction = database.GetTransaction())
                    {
                        if (migratedMatch.MigratedMatchLocationId.HasValue)
                        {
                            migratedMatch.MatchLocation = new MatchLocation
                            {
                                MatchLocationId = await database.ExecuteScalarAsync<Guid>($"SELECT MatchLocationId FROM {Tables.MatchLocation} WHERE MigratedMatchLocationId = @0", migratedMatch.MigratedMatchLocationId).ConfigureAwait(false)
                            };
                        }

                        if (migratedMatch.MigratedTournamentId.HasValue)
                        {
                            migratedMatch.Tournament = new Tournament
                            {
                                TournamentId = await database.ExecuteScalarAsync<Guid>($"SELECT MatchId FROM {Tables.Match} WHERE MigratedMatchId = @0", migratedMatch.MigratedTournamentId).ConfigureAwait(false)
                            };
                        }

                        if (migratedMatch.MigratedSeasonIds.Count > 0)
                        {
                            migratedMatch.Season = new Season
                            {
                                SeasonId = await database.ExecuteScalarAsync<Guid>($"SELECT SeasonId FROM {Tables.Season} WHERE MigratedSeasonId = @0", migratedMatch.MigratedSeasonIds.First()).ConfigureAwait(false)
                            };
                        }

                        await database.ExecuteAsync($@"INSERT INTO {Tables.Match}
						(MatchId, MigratedMatchId, MatchName, UpdateMatchNameAutomatically, MatchLocationId, MatchType, PlayerType, PlayersPerTeam,
						 InningsOrderIsKnown, TournamentId, OrderInTournament, StartTime, StartTimeIsKnown, MatchResultType, MatchNotes, SeasonId, MatchRoute)
						VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10, @11, @12, @13, @14, @15)",
                            migratedMatch.MatchId,
                            migratedMatch.MigratedMatchId,
                            migratedMatch.MatchName,
                            migratedMatch.UpdateMatchNameAutomatically,
                            migratedMatch.MatchLocation?.MatchLocationId,
                            migratedMatch.MatchType.ToString(),
                            migratedMatch.PlayerType.ToString(),
                            migratedMatch.PlayersPerTeam,
                            migratedMatch.InningsOrderIsKnown,
                            migratedMatch.Tournament?.TournamentId,
                            migratedMatch.OrderInTournament,
                            migratedMatch.StartTime,
                            migratedMatch.StartTimeIsKnown,
                            migratedMatch.MatchResultType?.ToString(),
                            migratedMatch.MatchNotes,
                            migratedMatch.Season?.SeasonId,
                            migratedMatch.MatchRoute).ConfigureAwait(false);
                        foreach (var innings in migratedMatch.MigratedMatchInnings)
                        {
                            if (innings.MigratedTeamId.HasValue)
                            {
                                innings.Team = new Team
                                {
                                    TeamId = await database.ExecuteScalarAsync<Guid>($"SELECT TeamId FROM {Tables.Team} WHERE MigratedTeamId = @0", innings.MigratedTeamId).ConfigureAwait(false)
                                };
                            }

                            await database.ExecuteAsync($@"INSERT INTO {Tables.MatchInnings} 
								(MatchInningsId, MatchId, TeamId, InningsOrderInMatch, Overs, Runs, Wickets)
								VALUES (@0, @1, @2, @3, @4, @5, @6)",
                                Guid.NewGuid(),
                                migratedMatch.MatchId,
                                innings.Team?.TeamId,
                                innings.InningsOrderInMatch,
                                innings.Overs,
                                innings.Runs,
                                innings.Wickets).ConfigureAwait(false);
                        }
                        foreach (var team in migratedMatch.MigratedTeams)
                        {
                            team.Team = new Team
                            {
                                TeamId = await database.ExecuteScalarAsync<Guid>($"SELECT TeamId FROM {Tables.Team} WHERE MigratedTeamId = @0", team.MigratedTeamId).ConfigureAwait(false)
                            };

                            await database.ExecuteAsync($@"INSERT INTO {Tables.MatchTeam} 
								(MatchTeamId, MatchId, TeamId, TeamRole, WonToss) VALUES (@0, @1, @2, @3, @4)",
                                Guid.NewGuid(),
                                migratedMatch.MatchId,
                                team.Team.TeamId,
                                team.TeamRole.ToString(),
                                team.WonToss).ConfigureAwait(false);
                        }
                        transaction.Complete();
                    }

                }
                catch (Exception e)
                {
                    _logger.Error<SqlServerMatchDataMigrator>(e);
                    throw;
                }
                scope.Complete();
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
