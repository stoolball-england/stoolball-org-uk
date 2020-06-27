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
    public class CompetitionSubscriptionMigrationController : UmbracoAuthorizedJsonController
    {
        private readonly ICompetitionSubscriptionDataMigrator _competitionSubscriptionDataMigrator;

        public CompetitionSubscriptionMigrationController(IGlobalSettings globalSettings,
            IUmbracoContextAccessor umbracoContextAccessor,
            ISqlContext sqlContext,
            ServiceContext serviceContext,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IRuntimeState runtimeState,
            UmbracoHelper umbracoHelper,
            ICompetitionSubscriptionDataMigrator competitionSubscriptionDataMigrator) :
            base(globalSettings, umbracoContextAccessor, sqlContext, serviceContext, appCaches, profilingLogger, runtimeState, umbracoHelper)
        {
            _competitionSubscriptionDataMigrator = competitionSubscriptionDataMigrator ?? throw new ArgumentNullException(nameof(competitionSubscriptionDataMigrator));
        }

        [HttpPost]
        public async Task<IHttpActionResult> CreateCompetitionSubscription(MigratedCompetitionSubscription subscription)
        {
            if (subscription is null)
            {
                throw new ArgumentNullException(nameof(subscription));
            }

            var migrated = await _competitionSubscriptionDataMigrator.MigrateCompetitionSubscription(subscription).ConfigureAwait(false);
            return Created("/", JsonConvert.SerializeObject(migrated));
        }

        [HttpDelete]
        public async Task<IHttpActionResult> DeleteCompetitionSubscriptions()
        {
            await _competitionSubscriptionDataMigrator.DeleteCompetitionSubscriptions().ConfigureAwait(false);
            return Ok();
        }
    }
}
