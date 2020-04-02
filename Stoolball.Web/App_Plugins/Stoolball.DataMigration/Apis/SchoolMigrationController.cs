using Stoolball.Schools;
using Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators;
using System;
using System.Web.Http;
using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Editors;
using Umbraco.Web.Mvc;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.Apis
{
    [PluginController("Migration")]
    public class SchoolMigrationController : UmbracoAuthorizedJsonController
    {
        private readonly ISchoolDataMigrator _schoolDataMigrator;

        public SchoolMigrationController(IGlobalSettings globalSettings,
            IUmbracoContextAccessor umbracoContextAccessor,
            ISqlContext sqlContext,
            ServiceContext serviceContext,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IRuntimeState runtimeState,
            UmbracoHelper umbracoHelper,
            ISchoolDataMigrator schoolDataMigrator) :
            base(globalSettings, umbracoContextAccessor, sqlContext, serviceContext, appCaches, profilingLogger, runtimeState, umbracoHelper)
        {
            _schoolDataMigrator = schoolDataMigrator;
        }

        [HttpPost]
        public void CreateSchool(School school)
        {
            if (school is null)
            {
                throw new ArgumentNullException(nameof(school));
            }

            _schoolDataMigrator.MigrateSchool(school);
        }

        [HttpDelete]
        public void DeleteSchools()
        {
            _schoolDataMigrator.DeleteSchools();
        }
    }
}
