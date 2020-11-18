using System;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Data.SqlServer;
using Stoolball.Logging;
using Stoolball.Routing;
using Stoolball.Schools;
using static Stoolball.Data.SqlServer.Constants;
using Tables = Stoolball.Data.SqlServer.Constants.Tables;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class SqlServerSchoolDataMigrator : ISchoolDataMigrator
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IRedirectsRepository _redirectsRepository;
        private readonly IAuditHistoryBuilder _auditHistoryBuilder;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger _logger;

        public SqlServerSchoolDataMigrator(IDatabaseConnectionFactory databaseConnectionFactory, IRedirectsRepository redirectsRepository, IAuditHistoryBuilder auditHistoryBuilder, IAuditRepository auditRepository, ILogger logger)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
            _auditHistoryBuilder = auditHistoryBuilder ?? throw new ArgumentNullException(nameof(auditHistoryBuilder));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Clear down all the school data ready for a fresh import
        /// </summary>
        /// <returns></returns>
        public async Task DeleteSchools()
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    await transaction.Connection.ExecuteAsync($"UPDATE {Tables.Team} SET SchoolId = NULL", null, transaction).ConfigureAwait(false);
                    await transaction.Connection.ExecuteAsync($"DELETE FROM {Tables.SchoolName}", null, transaction).ConfigureAwait(false);
                    await transaction.Connection.ExecuteAsync($@"DELETE FROM {Tables.School}", null, transaction).ConfigureAwait(false);

                    await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/schools/", transaction).ConfigureAwait(false);

                    transaction.Commit();
                }
            }
        }

        /// <summary>
        /// Save the supplied School to the database with its existing <see cref="School.SchoolId"/>
        /// </summary>
        public async Task<School> MigrateSchool(MigratedSchool school)
        {
            if (school is null)
            {
                throw new System.ArgumentNullException(nameof(school));
            }

            var migratedSchool = new MigratedSchool
            {
                SchoolId = Guid.NewGuid(),
                MigratedSchoolId = school.MigratedSchoolId,
                SchoolName = school.SchoolName,
                Twitter = school.Twitter,
                Facebook = school.Facebook,
                Instagram = school.Instagram,
                MemberGroupKey = school.MemberGroupKey,
                MemberGroupName = school.MemberGroupName,
                SchoolRoute = "/schools" + school.SchoolRoute.Substring(6)
            };

            _auditHistoryBuilder.BuildInitialAuditHistory(school, migratedSchool, nameof(SqlServerSchoolDataMigrator), x => x);

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    await connection.ExecuteAsync($@"INSERT INTO {Tables.School}
						(SchoolId, MigratedSchoolId, Twitter, Facebook, Instagram, MemberGroupKey, MemberGroupName, SchoolRoute)
						VALUES (@SchoolId, @MigratedSchoolId, @Twitter, @Facebook, @Instagram, @MemberGroupKey, @MemberGroupName, @SchoolRoute)",
                    new
                    {
                        migratedSchool.SchoolId,
                        migratedSchool.MigratedSchoolId,
                        migratedSchool.Twitter,
                        migratedSchool.Facebook,
                        migratedSchool.Instagram,
                        migratedSchool.MemberGroupKey,
                        migratedSchool.MemberGroupName,
                        migratedSchool.SchoolRoute
                    },
                    transaction).ConfigureAwait(false);

                    await connection.ExecuteAsync($@"INSERT INTO {Tables.SchoolName} 
							(SchoolNameId, SchoolId, SchoolName, FromDate) VALUES (@SchoolNameId, @SchoolId, @SchoolName, @FromDate)",
                        new
                        {
                            SchoolNameId = Guid.NewGuid(),
                            migratedSchool.SchoolId,
                            migratedSchool.SchoolName,
                            FromDate = migratedSchool.History[0].AuditDate
                        },
                        transaction).ConfigureAwait(false);

                    await _redirectsRepository.InsertRedirect(school.SchoolRoute, migratedSchool.SchoolRoute, string.Empty, transaction).ConfigureAwait(false);
                    await _redirectsRepository.InsertRedirect(school.SchoolRoute, migratedSchool.SchoolRoute, "/matches.rss", transaction).ConfigureAwait(false);

                    foreach (var audit in migratedSchool.History)
                    {
                        await _auditRepository.CreateAudit(audit, transaction).ConfigureAwait(false);
                    }

                    transaction.Commit();

                    migratedSchool.History.Clear();
                    _logger.Info(GetType(), LoggingTemplates.Migrated, migratedSchool, GetType(), nameof(MigrateSchool));
                }
            }

            return migratedSchool;
        }
    }
}
