using Stoolball.Competitions;
using Stoolball.Routing;
using Stoolball.Umbraco.Data.Audit;
using Stoolball.Umbraco.Data.Redirects;
using System;
using System.Globalization;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using Umbraco.Core.Scoping;
using static Stoolball.Umbraco.Data.Constants;
using Tables = Stoolball.Umbraco.Data.Constants.Tables;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class SqlServerCompetitionDataMigrator : ICompetitionDataMigrator
    {
        private readonly IRedirectsRepository _redirectsRepository;
        private readonly IScopeProvider _scopeProvider;
        private readonly IAuditHistoryBuilder _auditHistoryBuilder;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger _logger;
        private readonly IRouteGenerator _routeGenerator;

        public SqlServerCompetitionDataMigrator(IRedirectsRepository redirectsRepository, IScopeProvider scopeProvider, IAuditHistoryBuilder auditHistoryBuilder, IAuditRepository auditRepository,
            ILogger logger, IRouteGenerator routeGenerator)
        {
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
            _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
            _auditHistoryBuilder = auditHistoryBuilder ?? throw new ArgumentNullException(nameof(auditHistoryBuilder));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
        }

        /// <summary>
        /// Clear down all the competition data ready for a fresh import
        /// </summary>
        /// <returns></returns>
        public async Task DeleteCompetitions()
        {
            try
            {
                await DeleteSeasons().ConfigureAwait(false);

                using (var scope = _scopeProvider.CreateScope())
                {
                    var database = scope.Database;

                    using (var transaction = database.GetTransaction())
                    {
                        await database.ExecuteAsync($"DELETE FROM {Tables.Competition}").ConfigureAwait(false);
                        transaction.Complete();
                    }

                    scope.Complete();
                }
            }
            catch (Exception e)
            {
                _logger.Error<SqlServerCompetitionDataMigrator>(e);
                throw;
            }

            await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/competitions/").ConfigureAwait(false);
        }

        /// <summary>
        /// Save the supplied competition to the database with its existing <see cref="Competition.CompetitionId"/>
        /// </summary>
        public async Task<Competition> MigrateCompetition(MigratedCompetition competition)
        {
            if (competition is null)
            {
                throw new System.ArgumentNullException(nameof(competition));
            }

            var migratedCompetition = new MigratedCompetition
            {
                CompetitionId = Guid.NewGuid(),
                MigratedCompetitionId = competition.MigratedCompetitionId,
                CompetitionName = competition.CompetitionName,
                Introduction = competition.Introduction,
                PublicContactDetails = competition.PublicContactDetails,
                Website = competition.Website,
                Twitter = competition.Twitter,
                Facebook = competition.Facebook,
                Instagram = competition.Instagram,
                PlayersPerTeam = competition.PlayersPerTeam,
                Overs = competition.Overs,
                PlayerType = competition.PlayerType,
                MemberGroupId = competition.MemberGroupId,
                MemberGroupName = competition.MemberGroupName,
                UntilYear = competition.UntilYear
            };

            using (var scope = _scopeProvider.CreateScope())
            {
                migratedCompetition.CompetitionRoute = _routeGenerator.GenerateRoute("/competitions", migratedCompetition.CompetitionName, NoiseWords.CompetitionRoute);
                int count;
                do
                {
                    count = await scope.Database.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Competition} WHERE CompetitionRoute = @CompetitionRoute", new { migratedCompetition.CompetitionRoute }).ConfigureAwait(false);
                    if (count > 0)
                    {
                        migratedCompetition.CompetitionRoute = _routeGenerator.IncrementRoute(migratedCompetition.CompetitionRoute);
                    }
                }
                while (count > 0);
                scope.Complete();
            }

            _auditHistoryBuilder.BuildInitialAuditHistory(competition, migratedCompetition, nameof(SqlServerCompetitionDataMigrator));
            migratedCompetition.FromYear = competition.History[0].AuditDate.Year;

            using (var scope = _scopeProvider.CreateScope())
            {
                try
                {
                    var database = scope.Database;
                    using (var transaction = database.GetTransaction())
                    {
                        await database.ExecuteAsync($@"INSERT INTO {Tables.Competition}
						(CompetitionId, MigratedCompetitionId, CompetitionName, Introduction, Twitter, Facebook, Instagram, PublicContactDetails, Website, PlayersPerTeam, 
						 Overs, PlayerType, FromYear, UntilYear, MemberGroupId, MemberGroupName, CompetitionRoute)
						VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10, @11, @12, @13, @14, @15, @16)",
                            migratedCompetition.CompetitionId,
                            migratedCompetition.MigratedCompetitionId,
                            migratedCompetition.CompetitionName,
                            migratedCompetition.Introduction,
                            migratedCompetition.Twitter,
                            migratedCompetition.Facebook,
                            migratedCompetition.Instagram,
                            migratedCompetition.PublicContactDetails,
                            migratedCompetition.Website,
                            migratedCompetition.PlayersPerTeam,
                            migratedCompetition.Overs,
                            migratedCompetition.PlayerType.ToString(),
                            migratedCompetition.FromYear,
                            migratedCompetition.UntilYear,
                            migratedCompetition.MemberGroupId,
                            migratedCompetition.MemberGroupName,
                            migratedCompetition.CompetitionRoute).ConfigureAwait(false);
                        transaction.Complete();
                    }

                }
                catch (Exception e)
                {
                    _logger.Error<SqlServerCompetitionDataMigrator>(e);
                    throw;
                }
                scope.Complete();
            }

            await _redirectsRepository.InsertRedirect(competition.CompetitionRoute, migratedCompetition.CompetitionRoute, string.Empty).ConfigureAwait(false);
            await _redirectsRepository.InsertRedirect(competition.CompetitionRoute, migratedCompetition.CompetitionRoute, "/statistics").ConfigureAwait(false);
            await _redirectsRepository.InsertRedirect(competition.CompetitionRoute, migratedCompetition.CompetitionRoute, "/map").ConfigureAwait(false);
            await _redirectsRepository.InsertRedirect(competition.CompetitionRoute, migratedCompetition.CompetitionRoute, "/matches.rss").ConfigureAwait(false);

            foreach (var audit in migratedCompetition.History)
            {
                await _auditRepository.CreateAudit(audit).ConfigureAwait(false);
            }

            return migratedCompetition;
        }

        /// <summary>
        /// Clear down all the season data ready for a fresh import
        /// </summary>
        /// <returns></returns>
        public async Task DeleteSeasons()
        {
            try
            {
                using (var scope = _scopeProvider.CreateScope())
                {
                    var database = scope.Database;

                    using (var transaction = database.GetTransaction())
                    {
                        await database.ExecuteAsync($"DELETE FROM {Tables.SeasonPointsAdjustment}").ConfigureAwait(false);
                        await database.ExecuteAsync($"DELETE FROM {Tables.SeasonPointsRule}").ConfigureAwait(false);
                        await database.ExecuteAsync($"DELETE FROM {Tables.SeasonMatchType}").ConfigureAwait(false);
                        await database.ExecuteAsync($"DELETE FROM {Tables.TournamentSeason}").ConfigureAwait(false);
                        await database.ExecuteAsync($"DELETE FROM {Tables.SeasonTeam}").ConfigureAwait(false);
                        await database.ExecuteAsync($"DELETE FROM {Tables.Season}").ConfigureAwait(false);
                        transaction.Complete();
                    }

                    scope.Complete();
                }
            }
            catch (Exception e)
            {
                _logger.Error<SqlServerCompetitionDataMigrator>(e);
                throw;
            }
        }

        /// <summary>
        /// Save the supplied season to the database with its existing <see cref="Season.SeasonId"/>
        /// </summary>
        public async Task<Season> MigrateSeason(MigratedSeason season)
        {
            if (season is null)
            {
                throw new System.ArgumentNullException(nameof(season));
            }

            var migratedSeason = new MigratedSeason
            {
                SeasonId = Guid.NewGuid(),
                MigratedSeasonId = season.MigratedSeasonId,
                MigratedCompetition = season.MigratedCompetition,
                FromYear = season.FromYear,
                UntilYear = season.UntilYear,
                Introduction = season.Introduction,
                MigratedTeams = season.MigratedTeams,
                MatchTypes = season.MatchTypes,
                PointsRules = season.PointsRules,
                MigratedPointsAdjustments = season.MigratedPointsAdjustments,
                Results = season.Results,
                EnableTournaments = season.EnableTournaments,
                EnableResultsTable = season.EnableResultsTable,
                EnableRunsScored = season.EnableRunsScored,
                EnableRunsConceded = season.EnableRunsConceded
            };

            using (var scope = _scopeProvider.CreateScope())
            {
                var competitionRoute = await scope.Database.ExecuteScalarAsync<string>($"SELECT CompetitionRoute FROM {Tables.Competition} WHERE MigratedCompetitionId = @MigratedCompetitionId", new { migratedSeason.MigratedCompetition.MigratedCompetitionId }).ConfigureAwait(false);
                migratedSeason.SeasonRoute = $"{competitionRoute}/{migratedSeason.FromYear}";
                if (migratedSeason.UntilYear > migratedSeason.FromYear)
                {
                    migratedSeason.SeasonRoute = $"{migratedSeason.SeasonRoute}-{migratedSeason.UntilYear.ToString(CultureInfo.InvariantCulture).Substring(2)}";
                }
                scope.Complete();
            }

            _auditHistoryBuilder.BuildInitialAuditHistory(season, migratedSeason, nameof(SqlServerCompetitionDataMigrator));

            using (var scope = _scopeProvider.CreateScope())
            {
                try
                {
                    var database = scope.Database;
                    using (var transaction = database.GetTransaction())
                    {
                        migratedSeason.MigratedCompetition.CompetitionId = await database.ExecuteScalarAsync<Guid>($"SELECT CompetitionId FROM {Tables.Competition} WHERE MigratedCompetitionId = @0", season.MigratedCompetition.MigratedCompetitionId).ConfigureAwait(false);

                        await database.ExecuteAsync($@"INSERT INTO {Tables.Season}
						(SeasonId, MigratedSeasonId, CompetitionId, FromYear, UntilYear, Introduction, 
						 Results, EnableTournaments, EnableResultsTable, ResultsTableIsLeagueTable, EnableRunsScored, EnableRunsConceded, SeasonRoute)
						VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10, @11, @12)",
                            migratedSeason.SeasonId,
                            migratedSeason.MigratedSeasonId,
                            migratedSeason.MigratedCompetition.CompetitionId,
                            migratedSeason.FromYear,
                            migratedSeason.UntilYear,
                            migratedSeason.Introduction,
                            migratedSeason.Results,
                            migratedSeason.EnableTournaments,
                            migratedSeason.EnableResultsTable,
                            true,
                            migratedSeason.EnableRunsScored,
                            migratedSeason.EnableRunsConceded,
                            migratedSeason.SeasonRoute).ConfigureAwait(false);
                        foreach (var teamInSeason in migratedSeason.MigratedTeams)
                        {
                            var teamId = await database.ExecuteScalarAsync<Guid>($"SELECT TeamId FROM {Tables.Team} WHERE MigratedTeamId = @0", teamInSeason.MigratedTeamId).ConfigureAwait(false);

                            await database.ExecuteAsync($@"INSERT INTO {Tables.SeasonTeam}
								(SeasonTeamId, SeasonId, TeamId, WithdrawnDate) VALUES (@0, @1, @2, @3)",
                                Guid.NewGuid(),
                                migratedSeason.SeasonId,
                                teamId,
                                teamInSeason.WithdrawnDate
                                ).ConfigureAwait(false);
                        }
                        foreach (var matchType in migratedSeason.MatchTypes)
                        {
                            await database.ExecuteAsync($@"INSERT INTO {Tables.SeasonMatchType}
								(SeasonMatchTypeId, SeasonId, MatchType) VALUES (@0, @1, @2)",
                                Guid.NewGuid(),
                                migratedSeason.SeasonId,
                                matchType.ToString()
                                ).ConfigureAwait(false);
                        }
                        foreach (var rule in migratedSeason.PointsRules)
                        {
                            await database.ExecuteAsync($@"INSERT INTO {Tables.SeasonPointsRule}
								(SeasonPointsRuleId, SeasonId, MatchResultType, HomePoints, AwayPoints) 
								 VALUES (@0, @1, @2, @3, @4)",
                                Guid.NewGuid(),
                                migratedSeason.SeasonId,
                                rule.MatchResultType.ToString(),
                                rule.HomePoints,
                                rule.AwayPoints
                                ).ConfigureAwait(false);
                        }
                        foreach (var point in migratedSeason.MigratedPointsAdjustments)
                        {
                            var teamId = await database.ExecuteScalarAsync<Guid>($"SELECT TeamId FROM {Tables.Team} WHERE MigratedTeamId = @0", point.MigratedTeamId).ConfigureAwait(false);

                            await database.ExecuteAsync($@"INSERT INTO {Tables.SeasonPointsAdjustment}
								(SeasonPointsAdjustmentId, SeasonId, TeamId, Points, Reason) 
								 VALUES (@0, @1, @2, @3, @4)",
                                Guid.NewGuid(),
                                migratedSeason.SeasonId,
                                teamId,
                                point.Points,
                                point.Reason
                                ).ConfigureAwait(false);
                        }
                        transaction.Complete();
                    }

                }
                catch (Exception e)
                {
                    _logger.Error<SqlServerCompetitionDataMigrator>(e);
                    throw;
                }
                scope.Complete();
            }

            await _redirectsRepository.InsertRedirect(season.SeasonRoute, migratedSeason.SeasonRoute, string.Empty).ConfigureAwait(false);
            await _redirectsRepository.InsertRedirect(season.SeasonRoute, migratedSeason.SeasonRoute, "/statistics").ConfigureAwait(false);
            await _redirectsRepository.InsertRedirect(season.SeasonRoute, migratedSeason.SeasonRoute, "/table").ConfigureAwait(false);
            await _redirectsRepository.InsertRedirect(season.SeasonRoute, migratedSeason.SeasonRoute, "/map").ConfigureAwait(false);

            foreach (var audit in migratedSeason.History)
            {
                await _auditRepository.CreateAudit(audit).ConfigureAwait(false);
            }

            return migratedSeason;
        }
    }
}
