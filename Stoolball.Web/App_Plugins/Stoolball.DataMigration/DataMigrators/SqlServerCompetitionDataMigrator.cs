using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Competitions;
using Stoolball.Data.SqlServer;
using Stoolball.Logging;
using Stoolball.Matches;
using Stoolball.Routing;
using Stoolball.Security;
using Stoolball.Teams;
using static Stoolball.Constants;


namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class SqlServerCompetitionDataMigrator : ICompetitionDataMigrator
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IRedirectsRepository _redirectsRepository;
        private readonly IAuditHistoryBuilder _auditHistoryBuilder;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger _logger;
        private readonly IRouteGenerator _routeGenerator;
        private readonly IDataRedactor _dataRedactor;

        public SqlServerCompetitionDataMigrator(IDatabaseConnectionFactory databaseConnectionFactory, IRedirectsRepository redirectsRepository, IAuditHistoryBuilder auditHistoryBuilder, IAuditRepository auditRepository,
            ILogger logger, IRouteGenerator routeGenerator, IDataRedactor dataRedactor)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
            _auditHistoryBuilder = auditHistoryBuilder ?? throw new ArgumentNullException(nameof(auditHistoryBuilder));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
            _dataRedactor = dataRedactor ?? throw new ArgumentNullException(nameof(dataRedactor));
        }

        /// <summary>
        /// Clear down all the competition data ready for a fresh import
        /// </summary>
        /// <returns></returns>
        public async Task DeleteCompetitions()
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    await DeleteSeasons(transaction).ConfigureAwait(false);

                    await connection.ExecuteAsync($"DELETE FROM {Tables.CompetitionVersion}", null, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.Competition}", null, transaction).ConfigureAwait(false);

                    await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/competitions/", transaction).ConfigureAwait(false);

                    transaction.Commit();
                }
            }
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

            var migratedCompetition = CreateAuditableCopy(competition);
            migratedCompetition.CompetitionId = Guid.NewGuid();

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    migratedCompetition.CompetitionRoute = _routeGenerator.GenerateRoute("/competitions", migratedCompetition.CompetitionName, NoiseWords.CompetitionRoute);
                    int count;
                    do
                    {
                        count = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Competition} WHERE CompetitionRoute = @CompetitionRoute", new { migratedCompetition.CompetitionRoute }, transaction).ConfigureAwait(false);
                        if (count > 0)
                        {
                            migratedCompetition.CompetitionRoute = _routeGenerator.IncrementRoute(migratedCompetition.CompetitionRoute);
                        }
                    }
                    while (count > 0);

                    _auditHistoryBuilder.BuildInitialAuditHistory(competition, migratedCompetition, nameof(SqlServerCompetitionDataMigrator), CreateRedactedCopyOfCompetition);

                    await connection.ExecuteAsync($@"INSERT INTO {Tables.Competition}
						(CompetitionId, MigratedCompetitionId, Introduction, Twitter, Facebook, Instagram, PublicContactDetails, Website, 
						 PlayerType, MemberGroupKey, MemberGroupName, CompetitionRoute)
						VALUES 
                        (@CompetitionId, @MigratedCompetitionId, @Introduction, @Twitter, @Facebook, @Instagram, @PublicContactDetails, 
                        @Website, @PlayerType, @MemberGroupKey, @MemberGroupName, @CompetitionRoute)",
                    new
                    {
                        migratedCompetition.CompetitionId,
                        migratedCompetition.MigratedCompetitionId,
                        migratedCompetition.Introduction,
                        migratedCompetition.Twitter,
                        migratedCompetition.Facebook,
                        migratedCompetition.Instagram,
                        migratedCompetition.PublicContactDetails,
                        migratedCompetition.Website,
                        PlayerType = migratedCompetition.PlayerType.ToString(),
                        migratedCompetition.MemberGroupKey,
                        migratedCompetition.MemberGroupName,
                        migratedCompetition.CompetitionRoute
                    },
                    transaction).ConfigureAwait(false);

                    await connection.ExecuteAsync($@"INSERT INTO {Tables.CompetitionVersion} 
						(CompetitionVersionId, CompetitionId, CompetitionName, ComparableName, FromDate, UntilDate) VALUES (@CompetitionVersionId, @CompetitionId, @CompetitionName, @ComparableName, @FromDate, @UntilDate)",
                        new
                        {
                            CompetitionVersionId = Guid.NewGuid(),
                            migratedCompetition.CompetitionId,
                            migratedCompetition.CompetitionName,
                            ComparableName = migratedCompetition.ComparableName(),
                            FromDate = migratedCompetition.History[0].AuditDate,
                            UntilDate = migratedCompetition.UntilYear.HasValue ? new DateTime(migratedCompetition.UntilYear.Value, 12, 31) : (DateTime?)null
                        },
                        transaction).ConfigureAwait(false);

                    await _redirectsRepository.InsertRedirect(competition.CompetitionRoute, migratedCompetition.CompetitionRoute, string.Empty, transaction).ConfigureAwait(false);
                    await _redirectsRepository.InsertRedirect(competition.CompetitionRoute, migratedCompetition.CompetitionRoute, "/statistics", transaction).ConfigureAwait(false);
                    await _redirectsRepository.InsertRedirect(competition.CompetitionRoute, migratedCompetition.CompetitionRoute, "/map", transaction).ConfigureAwait(false);
                    await _redirectsRepository.InsertRedirect(competition.CompetitionRoute, migratedCompetition.CompetitionRoute, "/matches.rss", transaction).ConfigureAwait(false);

                    foreach (var audit in migratedCompetition.History)
                    {
                        await _auditRepository.CreateAudit(audit, transaction).ConfigureAwait(false);
                    }

                    transaction.Commit();

                    _logger.Info(GetType(), LoggingTemplates.Migrated, CreateRedactedCopyOfCompetition(migratedCompetition), GetType(), nameof(MigrateCompetition));
                }
            }

            return migratedCompetition;
        }

        private MigratedCompetition CreateRedactedCopyOfCompetition(MigratedCompetition competition)
        {
            var redacted = CreateAuditableCopy(competition);
            redacted.Introduction = _dataRedactor.RedactPersonalData(redacted.Introduction);
            redacted.PrivateContactDetails = _dataRedactor.RedactAll(redacted.PrivateContactDetails);
            redacted.PublicContactDetails = _dataRedactor.RedactAll(redacted.PublicContactDetails);
            redacted.History.Clear();
            return redacted;
        }

        private static MigratedCompetition CreateAuditableCopy(MigratedCompetition competition)
        {
            return new MigratedCompetition
            {
                CompetitionId = competition.CompetitionId,
                MigratedCompetitionId = competition.MigratedCompetitionId,
                CompetitionName = competition.CompetitionName,
                Introduction = competition.Introduction,
                PublicContactDetails = competition.PublicContactDetails,
                Website = competition.Website,
                Twitter = competition.Twitter,
                Facebook = competition.Facebook,
                Instagram = competition.Instagram,
                PlayerType = competition.PlayerType,
                MemberGroupKey = competition.MemberGroupKey,
                MemberGroupName = competition.MemberGroupName,
                UntilYear = competition.UntilYear
            };
        }

        /// <summary>
        /// Clear down all the season data ready for a fresh import
        /// </summary>
        /// <returns></returns>
        public async Task DeleteSeasons()
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    await DeleteSeasons(transaction).ConfigureAwait(false);
                    transaction.Commit();
                }
            }
        }

        /// <summary>
        /// Clear down all the season data ready for a fresh import
        /// </summary>
        /// <returns></returns>
        private static async Task DeleteSeasons(IDbTransaction transaction)
        {
            if (transaction is null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            await transaction.Connection.ExecuteAsync($"DELETE FROM {Tables.SeasonPointsAdjustment}", null, transaction).ConfigureAwait(false);
            await transaction.Connection.ExecuteAsync($"DELETE FROM {Tables.SeasonPointsRule}", null, transaction).ConfigureAwait(false);
            await transaction.Connection.ExecuteAsync($"DELETE FROM {Tables.SeasonMatchType}", null, transaction).ConfigureAwait(false);
            await transaction.Connection.ExecuteAsync($"DELETE FROM {Tables.OverSet} WHERE SeasonId IS NOT NULL", null, transaction).ConfigureAwait(false);
            await transaction.Connection.ExecuteAsync($"DELETE FROM {Tables.TournamentSeason}", null, transaction).ConfigureAwait(false);
            await transaction.Connection.ExecuteAsync($"DELETE FROM {Tables.SeasonTeam}", null, transaction).ConfigureAwait(false);
            await transaction.Connection.ExecuteAsync($"DELETE FROM {Tables.Season}", null, transaction).ConfigureAwait(false);
        }

        /// <summary>
        /// Save the supplied season to the database with its existing <see cref="Season.SeasonId"/>
        /// </summary>
        public async Task<Season> MigrateSeason(MigratedSeason season)
        {
            if (season is null)
            {
                throw new ArgumentNullException(nameof(season));
            }

            var migratedSeason = CreateAuditableCopy(season);
            migratedSeason.SeasonId = Guid.NewGuid();

            // create some default points rules to ensure all seasons have them
            if (migratedSeason.PointsRules.SingleOrDefault(x => x.MatchResultType == MatchResultType.HomeWin) == null)
            {
                migratedSeason.PointsRules.Add(new PointsRule { MatchResultType = MatchResultType.HomeWin, HomePoints = 2, AwayPoints = 0 });
            }
            if (migratedSeason.PointsRules.SingleOrDefault(x => x.MatchResultType == MatchResultType.AwayWin) == null)
            {
                migratedSeason.PointsRules.Add(new PointsRule { MatchResultType = MatchResultType.AwayWin, HomePoints = 0, AwayPoints = 2 });
            }
            if (migratedSeason.PointsRules.SingleOrDefault(x => x.MatchResultType == MatchResultType.HomeWinByForfeit) == null)
            {
                migratedSeason.PointsRules.Add(new PointsRule { MatchResultType = MatchResultType.HomeWinByForfeit, HomePoints = 2, AwayPoints = 0 });
            }
            if (migratedSeason.PointsRules.SingleOrDefault(x => x.MatchResultType == MatchResultType.AwayWinByForfeit) == null)
            {
                migratedSeason.PointsRules.Add(new PointsRule { MatchResultType = MatchResultType.AwayWinByForfeit, HomePoints = 0, AwayPoints = 2 });
            }
            if (migratedSeason.PointsRules.SingleOrDefault(x => x.MatchResultType == MatchResultType.Tie) == null)
            {
                migratedSeason.PointsRules.Add(new PointsRule { MatchResultType = MatchResultType.Tie, HomePoints = 1, AwayPoints = 1 });
            }
            if (migratedSeason.PointsRules.SingleOrDefault(x => x.MatchResultType == MatchResultType.Cancelled) == null)
            {
                migratedSeason.PointsRules.Add(new PointsRule { MatchResultType = MatchResultType.Cancelled, HomePoints = 1, AwayPoints = 1 });
            }
            if (migratedSeason.PointsRules.SingleOrDefault(x => x.MatchResultType == MatchResultType.AbandonedDuringPlayAndCancelled) == null)
            {
                migratedSeason.PointsRules.Add(new PointsRule { MatchResultType = MatchResultType.AbandonedDuringPlayAndCancelled, HomePoints = 1, AwayPoints = 1 });
            }

            migratedSeason.EnableLastPlayerBatsOn = (migratedSeason.FromYear != migratedSeason.UntilYear);

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var competitionRoute = await connection.ExecuteScalarAsync<string>($"SELECT CompetitionRoute FROM {Tables.Competition} WHERE MigratedCompetitionId = @MigratedCompetitionId", new { migratedSeason.MigratedCompetition.MigratedCompetitionId }, transaction).ConfigureAwait(false);
                    migratedSeason.SeasonRoute = $"{competitionRoute}/{migratedSeason.FromYear}";
                    if (migratedSeason.UntilYear > migratedSeason.FromYear)
                    {
                        migratedSeason.SeasonRoute = $"{migratedSeason.SeasonRoute}-{migratedSeason.UntilYear.ToString(CultureInfo.InvariantCulture).Substring(2)}";
                    }

                    migratedSeason.MigratedCompetition.CompetitionId = await connection.ExecuteScalarAsync<Guid>($"SELECT CompetitionId FROM {Tables.Competition} WHERE MigratedCompetitionId = @MigratedCompetitionId", new { season.MigratedCompetition.MigratedCompetitionId }, transaction).ConfigureAwait(false);

                    await connection.ExecuteAsync($@"INSERT INTO {Tables.Season}
						(SeasonId, MigratedSeasonId, CompetitionId, FromYear, UntilYear, Introduction, Results, PlayersPerTeam, 
                         EnableLastPlayerBatsOn, EnableBonusOrPenaltyRuns, EnableTournaments, ResultsTableType, EnableRunsScored, EnableRunsConceded, SeasonRoute)
						VALUES 
                        (@SeasonId, @MigratedSeasonId, @CompetitionId, @FromYear, @UntilYear, @Introduction, @Results, @PlayersPerTeam, 
                        @EnableLastPlayerBatsOn, @EnableBonusOrPenaltyRuns, @EnableTournaments, @ResultsTableType, @EnableRunsScored, @EnableRunsConceded, @SeasonRoute)",
                    new
                    {
                        migratedSeason.SeasonId,
                        migratedSeason.MigratedSeasonId,
                        migratedSeason.MigratedCompetition.CompetitionId,
                        migratedSeason.FromYear,
                        migratedSeason.UntilYear,
                        migratedSeason.Introduction,
                        migratedSeason.Results,
                        migratedSeason.PlayersPerTeam,
                        migratedSeason.EnableLastPlayerBatsOn,
                        migratedSeason.EnableBonusOrPenaltyRuns,
                        migratedSeason.EnableTournaments,
                        ResultsTableType = migratedSeason.ResultsTableType.ToString(),
                        migratedSeason.EnableRunsScored,
                        migratedSeason.EnableRunsConceded,
                        migratedSeason.SeasonRoute
                    },
                    transaction).ConfigureAwait(false);

                    if (migratedSeason.Overs.HasValue)
                    {
                        await transaction.Connection.ExecuteAsync($"INSERT INTO {Tables.OverSet} (OverSetId, SeasonId, OverSetNumber, Overs, BallsPerOver) VALUES (@OverSetId, @SeasonId, @OverSetNumber, @Overs, @BallsPerOver)",
                            new
                            {
                                OverSetId = Guid.NewGuid(),
                                migratedSeason.SeasonId,
                                OverSetNumber = 1,
                                migratedSeason.Overs,
                                BallsPerOver = 8
                            },
                            transaction).ConfigureAwait(false);
                    }

                    foreach (var teamInSeason in migratedSeason.MigratedTeams)
                    {
                        teamInSeason.Team = new Team { TeamId = await connection.ExecuteScalarAsync<Guid>($"SELECT TeamId FROM {Tables.Team} WHERE MigratedTeamId = @MigratedTeamId", new { teamInSeason.MigratedTeamId }, transaction).ConfigureAwait(false) };

                        await connection.ExecuteAsync($@"INSERT INTO {Tables.SeasonTeam}
								(SeasonTeamId, SeasonId, TeamId, WithdrawnDate) VALUES (@SeasonTeamId, @SeasonId, @TeamId, @WithdrawnDate)",
                            new
                            {
                                SeasonTeamId = Guid.NewGuid(),
                                migratedSeason.SeasonId,
                                teamInSeason.Team.TeamId,
                                teamInSeason.WithdrawnDate
                            },
                            transaction).ConfigureAwait(false);
                    }

                    foreach (var matchType in migratedSeason.MatchTypes)
                    {
                        await connection.ExecuteAsync($@"INSERT INTO {Tables.SeasonMatchType}
								(SeasonMatchTypeId, SeasonId, MatchType) VALUES (@SeasonMatchTypeId, @SeasonId, @MatchType)",
                            new
                            {
                                SeasonMatchTypeId = Guid.NewGuid(),
                                migratedSeason.SeasonId,
                                MatchType = matchType.ToString()
                            },
                            transaction).ConfigureAwait(false);
                    }

                    foreach (var rule in migratedSeason.PointsRules)
                    {
                        rule.PointsRuleId = Guid.NewGuid();

                        await connection.ExecuteAsync($@"INSERT INTO {Tables.SeasonPointsRule}
								(SeasonPointsRuleId, SeasonId, MatchResultType, HomePoints, AwayPoints) 
								 VALUES (@SeasonPointsRuleId, @SeasonId, @MatchResultType, @HomePoints, @AwayPoints)",
                             new
                             {
                                 SeasonPointsRuleId = rule.PointsRuleId,
                                 migratedSeason.SeasonId,
                                 MatchResultType = rule.MatchResultType.ToString(),
                                 rule.HomePoints,
                                 rule.AwayPoints
                             },
                            transaction).ConfigureAwait(false);
                    }

                    foreach (var point in migratedSeason.MigratedPointsAdjustments)
                    {
                        point.Team = new Team { TeamId = await connection.ExecuteScalarAsync<Guid>($"SELECT TeamId FROM {Tables.Team} WHERE MigratedTeamId = @MigratedTeamId", new { point.MigratedTeamId }, transaction).ConfigureAwait(false) };

                        await connection.ExecuteAsync($@"INSERT INTO {Tables.SeasonPointsAdjustment}
								(SeasonPointsAdjustmentId, SeasonId, TeamId, Points, Reason) 
								 VALUES (@SeasonPointsAdjustmentId, @SeasonId, @TeamId, @Points, @Reason)",
                             new
                             {
                                 SeasonPointsAdjustmentId = Guid.NewGuid(),
                                 migratedSeason.SeasonId,
                                 point.Team.TeamId,
                                 point.Points,
                                 point.Reason
                             },
                             transaction).ConfigureAwait(false);
                    }

                    await _redirectsRepository.InsertRedirect(season.SeasonRoute, migratedSeason.SeasonRoute, string.Empty, transaction).ConfigureAwait(false);
                    await _redirectsRepository.InsertRedirect(season.SeasonRoute, migratedSeason.SeasonRoute, "/statistics", transaction).ConfigureAwait(false);
                    await _redirectsRepository.InsertRedirect(season.SeasonRoute, migratedSeason.SeasonRoute, "/table", transaction).ConfigureAwait(false);
                    await _redirectsRepository.InsertRedirect(season.SeasonRoute, migratedSeason.SeasonRoute, "/map", transaction).ConfigureAwait(false);

                    _auditHistoryBuilder.BuildInitialAuditHistory(season, migratedSeason, nameof(SqlServerCompetitionDataMigrator), CreateRedactedCopyOfSeason);
                    foreach (var audit in migratedSeason.History)
                    {
                        await _auditRepository.CreateAudit(audit, transaction).ConfigureAwait(false);
                    }

                    transaction.Commit();

                    _logger.Info(GetType(), LoggingTemplates.Migrated, CreateRedactedCopyOfSeason(migratedSeason), GetType(), nameof(MigrateSeason));

                    return migratedSeason;
                }
            }
        }

        private static MigratedSeason CreateAuditableCopy(MigratedSeason season)
        {
            return new MigratedSeason
            {
                SeasonId = season.SeasonId,
                MigratedSeasonId = season.MigratedSeasonId,
                MigratedCompetition = season.MigratedCompetition,
                FromYear = season.FromYear,
                UntilYear = season.UntilYear,
                Introduction = season.Introduction,
                MigratedTeams = season.MigratedTeams,
                MatchTypes = season.MatchTypes,
                PlayersPerTeam = season.PlayersPerTeam,
                Overs = season.Overs,
                PointsRules = season.PointsRules,
                MigratedPointsAdjustments = season.MigratedPointsAdjustments,
                Results = season.Results,
                EnableTournaments = season.EnableTournaments,
                EnableBonusOrPenaltyRuns = true,
                ResultsTableType = season.ResultsTableType,
                EnableRunsScored = season.EnableRunsScored,
                EnableRunsConceded = season.EnableRunsConceded
            };
        }

        private MigratedSeason CreateRedactedCopyOfSeason(MigratedSeason season)
        {
            var redacted = CreateAuditableCopy(season);
            redacted.Introduction = _dataRedactor.RedactPersonalData(redacted.Introduction);
            redacted.Results = _dataRedactor.RedactPersonalData(redacted.Results);
            redacted.History.Clear();
            return redacted;
        }
    }
}
