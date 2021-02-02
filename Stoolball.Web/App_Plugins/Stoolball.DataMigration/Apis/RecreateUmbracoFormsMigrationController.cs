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
    public class RecreateUmbracoFormsMigrationController : UmbracoAuthorizedJsonController
    {
        private readonly IUmbracoFormsDataMigrator _umbracoFormsDataMigrator;

        public RecreateUmbracoFormsMigrationController(IGlobalSettings globalSettings,
            IUmbracoContextAccessor umbracoContextAccessor,
            ISqlContext sqlContext,
            ServiceContext serviceContext,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IRuntimeState runtimeState,
            UmbracoHelper umbracoHelper,
            IUmbracoFormsDataMigrator umbracoFormsDataMigrator) :
            base(globalSettings, umbracoContextAccessor, sqlContext, serviceContext, appCaches, profilingLogger, runtimeState, umbracoHelper)
        {
            _umbracoFormsDataMigrator = umbracoFormsDataMigrator;
        }

        [HttpPost]
        public async Task<IHttpActionResult> RecreateUmbracoForms()
        {
            await _umbracoFormsDataMigrator.RecreateForms().ConfigureAwait(false);
            return Ok();
        }
    }
}
