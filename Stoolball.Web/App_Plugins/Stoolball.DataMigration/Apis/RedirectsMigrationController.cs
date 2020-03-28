using Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators;
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
    public class RedirectsMigrationController : UmbracoAuthorizedJsonController
    {
        private readonly IRedirectsDataMigrator _redirectsDataMigrator;

        public RedirectsMigrationController(IGlobalSettings globalSettings,
            IUmbracoContextAccessor umbracoContextAccessor,
            ISqlContext sqlContext,
            ServiceContext serviceContext,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IRuntimeState runtimeState,
            UmbracoHelper umbracoHelper,
            IRedirectsDataMigrator redirectsDataMigrator) :
            base(globalSettings, umbracoContextAccessor, sqlContext, serviceContext, appCaches, profilingLogger, runtimeState, umbracoHelper)
        {
            _redirectsDataMigrator = redirectsDataMigrator;
        }

        [HttpPost]
        public void EnsureRedirects()
        {
            _redirectsDataMigrator.EnsureRedirects(Umbraco.ContentQuery);
        }
    }
}
