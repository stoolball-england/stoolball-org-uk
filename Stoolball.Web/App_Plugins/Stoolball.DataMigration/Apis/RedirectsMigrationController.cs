using System.Threading.Tasks;
using System.Web.Http;
using Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators;
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
        private readonly IPublishedContentQuery _publishedContentQuery;

        public RedirectsMigrationController(IGlobalSettings globalSettings,
            IUmbracoContextAccessor umbracoContextAccessor,
            ISqlContext sqlContext,
            ServiceContext serviceContext,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IRuntimeState runtimeState,
            UmbracoHelper umbracoHelper,
            IRedirectsDataMigrator redirectsDataMigrator,
            IPublishedContentQuery publishedContentQuery) :
            base(globalSettings, umbracoContextAccessor, sqlContext, serviceContext, appCaches, profilingLogger, runtimeState, umbracoHelper)
        {
            _redirectsDataMigrator = redirectsDataMigrator;
            _publishedContentQuery = publishedContentQuery ?? throw new System.ArgumentNullException(nameof(publishedContentQuery));
        }

        [HttpPost]
        public async Task<IHttpActionResult> EnsureRedirects()
        {
            await _redirectsDataMigrator.EnsureRedirects(_publishedContentQuery).ConfigureAwait(false);
            return Ok();
        }
    }
}
