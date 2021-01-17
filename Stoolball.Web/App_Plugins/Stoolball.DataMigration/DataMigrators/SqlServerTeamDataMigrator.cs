using System;
using System.Globalization;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Data.SqlServer;
using Stoolball.Logging;
using Stoolball.Routing;
using Stoolball.Security;
using Stoolball.Teams;
using Umbraco.Core.Services;
using static Stoolball.Constants;
using Tables = Stoolball.Constants.Tables;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class SqlServerTeamDataMigrator : ITeamDataMigrator
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly ServiceContext _serviceContext;
        private readonly IRedirectsRepository _redirectsRepository;
        private readonly IAuditHistoryBuilder _auditHistoryBuilder;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger _logger;
        private readonly IRouteGenerator _routeGenerator;
        private readonly IDataRedactor _dataRedactor;

        public SqlServerTeamDataMigrator(IDatabaseConnectionFactory databaseConnectionFactory, ServiceContext serviceContext, IRedirectsRepository redirectsRepository,
            IAuditHistoryBuilder auditHistoryBuilder, IAuditRepository auditRepository, ILogger logger, IRouteGenerator routeGenerator, IDataRedactor dataRedactor)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _serviceContext = serviceContext ?? throw new ArgumentNullException(nameof(serviceContext));
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
            _auditHistoryBuilder = auditHistoryBuilder ?? throw new ArgumentNullException(nameof(auditHistoryBuilder));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
            _dataRedactor = dataRedactor ?? throw new ArgumentNullException(nameof(dataRedactor));
        }

        /// <summary>
        /// Clear down all the team data ready for a fresh import
        /// </summary>
        /// <returns></returns>
        public async Task DeleteTeams()
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    await connection.ExecuteAsync($"DELETE FROM {Tables.Player}", null, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.PlayerIdentity}", null, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.TeamMatchLocation}", null, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.TeamName}", null, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($@"DELETE FROM {Tables.Team}", null, transaction).ConfigureAwait(false);

                    await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/teams/", transaction).ConfigureAwait(false);

                    transaction.Commit();
                }
            }
        }

        /// <summary>
        /// Save the supplied team to the database with its existing <see cref="Team.TeamId"/>
        /// </summary>
        public async Task<Team> MigrateTeam(MigratedTeam team)
        {
            if (team is null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            var migratedTeam = CreateAuditableCopy(team);
            migratedTeam.TeamId = Guid.NewGuid();

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {

                    migratedTeam.TeamRoute = _routeGenerator.GenerateRoute("/teams", migratedTeam.TeamName, NoiseWords.TeamRoute);
                    int count;
                    do
                    {
                        count = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Team} WHERE TeamRoute = @TeamRoute", new { migratedTeam.TeamRoute }, transaction).ConfigureAwait(false);
                        if (count > 0)
                        {
                            migratedTeam.TeamRoute = _routeGenerator.IncrementRoute(migratedTeam.TeamRoute);
                        }
                    }
                    while (count > 0);

                    if (migratedTeam.TeamType == TeamType.Transient)
                    {
                        // Use a partial route that will be updated when the tournament is imported
                        migratedTeam.TeamRoute = migratedTeam.MigratedTeamId.ToString("00000", CultureInfo.InvariantCulture) + migratedTeam.TeamRoute;
                    }

                    _auditHistoryBuilder.BuildInitialAuditHistory(team, migratedTeam, nameof(SqlServerTeamDataMigrator), CreateRedactedCopy);

                    if (migratedTeam.MigratedClubId.HasValue)
                    {
                        migratedTeam.ClubId = await connection.ExecuteScalarAsync<Guid>($"SELECT ClubId FROM {Tables.Club} WHERE MigratedClubId = @MigratedClubId", new { migratedTeam.MigratedClubId }, transaction).ConfigureAwait(false);
                    }
                    if (migratedTeam.MigratedSchoolId.HasValue)
                    {
                        migratedTeam.SchoolId = await connection.ExecuteScalarAsync<Guid>($"SELECT SchoolId FROM {Tables.School} WHERE MigratedSchoolId = @MigratedSchoolId", new { migratedTeam.MigratedSchoolId }, transaction).ConfigureAwait(false);
                    }
                    if (migratedTeam.MigratedMatchLocationId.HasValue)
                    {
                        migratedTeam.MatchLocationId = await connection.ExecuteScalarAsync<Guid>($"SELECT MatchLocationId FROM {Tables.MatchLocation} WHERE MigratedMatchLocationId = @MigratedMatchLocationId", new { migratedTeam.MigratedMatchLocationId }, transaction).ConfigureAwait(false);
                    }

                    await connection.ExecuteAsync($@"INSERT INTO {Tables.Team}
						(TeamId, MigratedTeamId, ClubId, ClubMark, SchoolId, TeamType, PlayerType, Introduction, AgeRangeLower, AgeRangeUpper, 
						 UntilYear, Twitter, Facebook, Instagram, Website, PublicContactDetails, PrivateContactDetails, PlayingTimes, Cost,
						 MemberGroupKey, MemberGroupName, TeamRoute)
						VALUES 
                        (@TeamId, @MigratedTeamId, @ClubId, @ClubMark, @SchoolId, @TeamType, @PlayerType, @Introduction, @AgeRangeLower, @AgeRangeUpper, 
                         @UntilYear, @Twitter, @Facebook, @Instagram, @Website, @PublicContactDetails, @PrivateContactDetails, @PlayingTimes, @Cost, 
                         @MemberGroupKey, @MemberGroupName, @TeamRoute)",
                     new
                     {
                         migratedTeam.TeamId,
                         migratedTeam.MigratedTeamId,
                         migratedTeam.ClubId,
                         migratedTeam.ClubMark,
                         migratedTeam.SchoolId,
                         TeamType = migratedTeam.TeamType.ToString(),
                         PlayerType = migratedTeam.PlayerType.ToString(),
                         migratedTeam.Introduction,
                         migratedTeam.AgeRangeLower,
                         migratedTeam.AgeRangeUpper,
                         migratedTeam.UntilYear,
                         migratedTeam.Twitter,
                         migratedTeam.Facebook,
                         migratedTeam.Instagram,
                         migratedTeam.Website,
                         migratedTeam.PublicContactDetails,
                         migratedTeam.PrivateContactDetails,
                         migratedTeam.PlayingTimes,
                         migratedTeam.Cost,
                         migratedTeam.MemberGroupKey,
                         migratedTeam.MemberGroupName,
                         migratedTeam.TeamRoute
                     },
                     transaction).ConfigureAwait(false);

                    await connection.ExecuteAsync($@"INSERT INTO {Tables.TeamName} 
							(TeamNameId, TeamId, TeamName, TeamComparableName, FromDate) VALUES (@TeamNameId, @TeamId, @TeamName, @TeamComparableName, @FromDate)",
                        new
                        {
                            TeamNameId = Guid.NewGuid(),
                            migratedTeam.TeamId,
                            migratedTeam.TeamName,
                            TeamComparableName = migratedTeam.ComparableName(),
                            FromDate = migratedTeam.History[0].AuditDate
                        },
                        transaction).ConfigureAwait(false);

                    if (migratedTeam.MatchLocationId.HasValue)
                    {
                        await connection.ExecuteAsync($@"INSERT INTO {Tables.TeamMatchLocation} 
							(TeamMatchLocationId, TeamId, MatchLocationId) VALUES (@TeamMatchLocationId, @TeamId, @MatchLocationId)",
                        new
                        {
                            TeamMatchLocationId = Guid.NewGuid(),
                            migratedTeam.TeamId,
                            migratedTeam.MatchLocationId
                        },
                        transaction).ConfigureAwait(false);
                    }

                    await _redirectsRepository.InsertRedirect(team.TeamRoute, migratedTeam.TeamRoute, string.Empty, transaction).ConfigureAwait(false);
                    await _redirectsRepository.InsertRedirect(team.TeamRoute, migratedTeam.TeamRoute, "/matches.rss", transaction).ConfigureAwait(false);
                    await _redirectsRepository.InsertRedirect(team.TeamRoute, migratedTeam.TeamRoute, "/statistics", transaction).ConfigureAwait(false);
                    await _redirectsRepository.InsertRedirect(team.TeamRoute, migratedTeam.TeamRoute, "/players", transaction).ConfigureAwait(false);
                    await _redirectsRepository.InsertRedirect(team.TeamRoute, migratedTeam.TeamRoute, "/calendar.ics", transaction).ConfigureAwait(false);

                    foreach (var audit in migratedTeam.History)
                    {
                        await _auditRepository.CreateAudit(audit, transaction).ConfigureAwait(false);
                    }

                    transaction.Commit();

                    _logger.Info(GetType(), LoggingTemplates.Migrated, CreateRedactedCopy(migratedTeam), GetType(), nameof(MigrateTeam));
                }
            }

            return migratedTeam;
        }

        private MigratedTeam CreateRedactedCopy(MigratedTeam team)
        {
            var redacted = CreateAuditableCopy(team);
            redacted.Introduction = _dataRedactor.RedactPersonalData(team.Introduction);
            redacted.PlayingTimes = _dataRedactor.RedactPersonalData(team.PlayingTimes);
            redacted.Cost = _dataRedactor.RedactPersonalData(team.Cost);
            redacted.PublicContactDetails = _dataRedactor.RedactAll(team.PublicContactDetails);
            redacted.PrivateContactDetails = _dataRedactor.RedactAll(team.PrivateContactDetails);
            redacted.History.Clear();
            return redacted;
        }

        private static MigratedTeam CreateAuditableCopy(MigratedTeam team)
        {
            return new MigratedTeam
            {
                TeamId = team.TeamId,
                MigratedTeamId = team.MigratedTeamId,
                TeamName = team.TeamName,
                ClubMark = team.ClubMark,
                MigratedClubId = team.MigratedClubId,
                MigratedSchoolId = team.MigratedSchoolId,
                MigratedMatchLocationId = team.MigratedMatchLocationId,
                TeamType = team.TeamType,
                PlayerType = team.PlayerType,
                Introduction = team.Introduction,
                AgeRangeLower = team.AgeRangeLower,
                AgeRangeUpper = team.AgeRangeUpper,
                UntilYear = team.UntilYear,
                Twitter = team.Twitter,
                Facebook = team.Facebook,
                Instagram = team.Instagram,
                Website = team.Website,
                PublicContactDetails = team.PublicContactDetails,
                PrivateContactDetails = team.PrivateContactDetails,
                PlayingTimes = team.PlayingTimes,
                Cost = team.Cost,
                MemberGroupKey = team.MemberGroupKey,
                MemberGroupName = team.MemberGroupName
            };
        }
    }
}
