using Stoolball.MatchLocations;
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
    public class MatchLocationMigrationController : UmbracoAuthorizedJsonController
    {
        private readonly IMatchLocationDataMigrator _matchLocationDataMigrator;

        public MatchLocationMigrationController(IGlobalSettings globalSettings,
            IUmbracoContextAccessor umbracoContextAccessor,
            ISqlContext sqlContext,
            ServiceContext serviceContext,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IRuntimeState runtimeState,
            UmbracoHelper umbracoHelper,
            IMatchLocationDataMigrator matchLocationDataMigrator) :
            base(globalSettings, umbracoContextAccessor, sqlContext, serviceContext, appCaches, profilingLogger, runtimeState, umbracoHelper)
        {
            _matchLocationDataMigrator = matchLocationDataMigrator;
        }

        [HttpPost]
        public void CreateMatchLocation(MatchLocation matchLocation)
        {
            if (matchLocation is null)
            {
                throw new ArgumentNullException(nameof(matchLocation));
            }

            _matchLocationDataMigrator.MigrateMatchLocation(matchLocation);
        }

        [HttpDelete]
        public void DeleteMatchLocations()
        {
            _matchLocationDataMigrator.DeleteMatchLocations();
        }
    }
}
