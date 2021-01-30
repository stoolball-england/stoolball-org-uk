using System;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Clubs;
using Stoolball.Data.SqlServer;
using Stoolball.Logging;
using Stoolball.Routing;
using static Stoolball.Constants;


namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class SqlServerClubDataMigrator : IClubDataMigrator
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IRedirectsRepository _redirectsRepository;
        private readonly IAuditHistoryBuilder _auditHistoryBuilder;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger _logger;
        private readonly IRouteGenerator _routeGenerator;

        public SqlServerClubDataMigrator(IDatabaseConnectionFactory databaseConnectionFactory, IRedirectsRepository redirectsRepository, IAuditHistoryBuilder auditHistoryBuilder,
            IAuditRepository auditRepository, ILogger logger, IRouteGenerator routeGenerator)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
            _auditHistoryBuilder = auditHistoryBuilder ?? throw new ArgumentNullException(nameof(auditHistoryBuilder));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
        }

        /// <summary>
        /// Clear down all the club data ready for a fresh import
        /// </summary>
        /// <returns></returns>
        public async Task DeleteClubs()
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    await connection.ExecuteAsync($"UPDATE {Tables.Team} SET ClubId = NULL", null, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.ClubVersion}", null, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($@"DELETE FROM {Tables.Club}", null, transaction).ConfigureAwait(false);

                    await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/clubs/", transaction).ConfigureAwait(false);

                    transaction.Commit();
                }
            }
        }

        /// <summary>
        /// Save the supplied Club to the database with its existing <see cref="Club.ClubId"/>
        /// </summary>
        public async Task<Club> MigrateClub(MigratedClub club)
        {
            if (club is null)
            {
                throw new System.ArgumentNullException(nameof(club));
            }

            var migratedClub = new MigratedClub
            {
                ClubId = Guid.NewGuid(),
                MigratedClubId = club.MigratedClubId,
                ClubName = club.ClubName,
                MemberGroupKey = club.MemberGroupKey,
                MemberGroupName = club.MemberGroupName,
            };

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    migratedClub.ClubRoute = _routeGenerator.GenerateRoute("/clubs", club.ClubName, NoiseWords.ClubRoute);
                    int count;
                    do
                    {
                        count = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Club} WHERE ClubRoute = @ClubRoute", new { migratedClub.ClubRoute }, transaction).ConfigureAwait(false);
                        if (count > 0)
                        {
                            migratedClub.ClubRoute = _routeGenerator.IncrementRoute(migratedClub.ClubRoute);
                        }
                    }
                    while (count > 0);

                    _auditHistoryBuilder.BuildInitialAuditHistory(club, migratedClub, nameof(SqlServerClubDataMigrator), x => x);

                    await connection.ExecuteAsync($@"INSERT INTO {Tables.Club}
						(ClubId, MigratedClubId, MemberGroupKey, MemberGroupName, ClubRoute)
						VALUES (@ClubId, @MigratedClubId, @MemberGroupKey, @MemberGroupName, @ClubRoute)",
                        new
                        {
                            migratedClub.ClubId,
                            migratedClub.MigratedClubId,
                            migratedClub.MemberGroupKey,
                            migratedClub.MemberGroupName,
                            migratedClub.ClubRoute
                        },
                        transaction).ConfigureAwait(false);

                    await connection.ExecuteAsync($@"INSERT INTO {Tables.ClubVersion} 
						(ClubVersionId, ClubId, ClubName, ComparableName, FromDate) VALUES (@ClubVersionId, @ClubId, @ClubName, @ComparableName, @FromDate)",
                        new
                        {
                            ClubVersionId = Guid.NewGuid(),
                            migratedClub.ClubId,
                            migratedClub.ClubName,
                            ComparableName = migratedClub.ComparableName(),
                            FromDate = migratedClub.History[0].AuditDate
                        },
                        transaction).ConfigureAwait(false);

                    await _redirectsRepository.InsertRedirect(club.ClubRoute, migratedClub.ClubRoute, string.Empty, transaction).ConfigureAwait(false);
                    await _redirectsRepository.InsertRedirect(club.ClubRoute, migratedClub.ClubRoute, "/matches.rss", transaction).ConfigureAwait(false);

                    foreach (var audit in migratedClub.History)
                    {
                        await _auditRepository.CreateAudit(audit, transaction).ConfigureAwait(false);
                    }

                    transaction.Commit();

                    migratedClub.History.Clear();
                    _logger.Info(GetType(), LoggingTemplates.Migrated, migratedClub, GetType(), nameof(MigrateClub));
                }
            }

            return migratedClub;
        }
    }
}
