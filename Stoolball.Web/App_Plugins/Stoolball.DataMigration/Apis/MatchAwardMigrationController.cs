using Newtonsoft.Json;
using Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators;
using System;
using System.Threading.Tasks;
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
    public class MatchAwardMigrationController : UmbracoAuthorizedJsonController
    {
        private readonly IMatchAwardDataMigrator _matchAwardDataMigrator;

        public MatchAwardMigrationController(IGlobalSettings globalSettings,
            IUmbracoContextAccessor umbracoContextAccessor,
            ISqlContext sqlContext,
            ServiceContext serviceContext,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IRuntimeState runtimeState,
            UmbracoHelper umbracoHelper,
            IMatchAwardDataMigrator matchAwardDataMigrator) :
            base(globalSettings, umbracoContextAccessor, sqlContext, serviceContext, appCaches, profilingLogger, runtimeState, umbracoHelper)
        {
            _matchAwardDataMigrator = matchAwardDataMigrator ?? throw new ArgumentNullException(nameof(matchAwardDataMigrator));
        }

        [HttpPost]
        public async Task<IHttpActionResult> CreateMatchAward(MigratedMatchAward matchAward)
        {
            if (matchAward is null)
            {
                throw new ArgumentNullException(nameof(matchAward));
            }

            var migrated = await _matchAwardDataMigrator.MigrateMatchAward(matchAward).ConfigureAwait(false);
            return Created(migrated.EntityUri, JsonConvert.SerializeObject(migrated));
        }

        [HttpDelete]
        public async Task<IHttpActionResult> DeleteMatchAwards()
        {
            await _matchAwardDataMigrator.DeleteMatchAwards().ConfigureAwait(false);
            return Ok();
        }
    }
}
