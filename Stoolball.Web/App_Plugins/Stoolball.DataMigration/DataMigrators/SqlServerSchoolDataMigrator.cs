using System;
using System.Threading.Tasks;
using Stoolball.Logging;
using Stoolball.Routing;
using Stoolball.Schools;
using Umbraco.Core.Scoping;
using Tables = Stoolball.Data.SqlServer.Constants.Tables;
using UmbracoLogging = Umbraco.Core.Logging;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class SqlServerSchoolDataMigrator : ISchoolDataMigrator
    {
        private readonly IRedirectsRepository _redirectsRepository;
        private readonly IScopeProvider _scopeProvider;
        private readonly IAuditHistoryBuilder _auditHistoryBuilder;
        private readonly IAuditRepository _auditRepository;
        private readonly UmbracoLogging.ILogger _logger;

        public SqlServerSchoolDataMigrator(IRedirectsRepository redirectsRepository, IScopeProvider scopeProvider, IAuditHistoryBuilder auditHistoryBuilder, IAuditRepository auditRepository, UmbracoLogging.ILogger logger)
        {
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
            _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
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
            try
            {
                using (var scope = _scopeProvider.CreateScope())
                {
                    var database = scope.Database;

                    using (var transaction = database.GetTransaction())
                    {
                        await database.ExecuteAsync($"UPDATE {Tables.Team} SET SchoolId = NULL").ConfigureAwait(false);
                        await database.ExecuteAsync($"DELETE FROM {Tables.SchoolName}").ConfigureAwait(false);
                        await database.ExecuteAsync($@"DELETE FROM {Tables.School}").ConfigureAwait(false);
                        transaction.Complete();
                    }

                    scope.Complete();
                }
            }
            catch (Exception e)
            {
                _logger.Error(typeof(SqlServerSchoolDataMigrator), e);
                throw;
            }

            await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/schools/").ConfigureAwait(false);
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

            _auditHistoryBuilder.BuildInitialAuditHistory(school, migratedSchool, nameof(SqlServerSchoolDataMigrator));

            try
            {
                using (var scope = _scopeProvider.CreateScope())
                {
                    var database = scope.Database;
                    using (var transaction = database.GetTransaction())
                    {
                        await database.ExecuteAsync($@"INSERT INTO {Tables.School}
						(SchoolId, MigratedSchoolId, Twitter, Facebook, Instagram, MemberGroupKey, MemberGroupName, SchoolRoute)
						VALUES (@0, @1, @2, @3, @4, @5, @6, @7)",
                            migratedSchool.SchoolId,
                            migratedSchool.MigratedSchoolId,
                            migratedSchool.Twitter,
                            migratedSchool.Facebook,
                            migratedSchool.Instagram,
                            migratedSchool.MemberGroupKey,
                            migratedSchool.MemberGroupName,
                            migratedSchool.SchoolRoute).ConfigureAwait(false);
                        await database.ExecuteAsync($@"INSERT INTO {Tables.SchoolName} 
							(SchoolNameId, SchoolId, SchoolName, FromDate) VALUES (@0, @1, @2, @3)",
                            Guid.NewGuid(),
                            migratedSchool.SchoolId,
                            migratedSchool.SchoolName,
                            migratedSchool.History[0].AuditDate
                            ).ConfigureAwait(false);
                        transaction.Complete();
                    }

                    scope.Complete();
                }
            }
            catch (Exception e)
            {
                _logger.Error(typeof(SqlServerSchoolDataMigrator), e);
                throw;
            }

            await _redirectsRepository.InsertRedirect(school.SchoolRoute, migratedSchool.SchoolRoute, string.Empty).ConfigureAwait(false);
            await _redirectsRepository.InsertRedirect(school.SchoolRoute, migratedSchool.SchoolRoute, "/matches.rss").ConfigureAwait(false);

            foreach (var audit in migratedSchool.History)
            {
                await _auditRepository.CreateAudit(audit).ConfigureAwait(false);
            }

            return migratedSchool;
        }
    }
}
