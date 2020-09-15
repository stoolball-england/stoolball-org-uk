using System;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json;
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
    public class MatchCommentSubscriptionMigrationController : UmbracoAuthorizedJsonController
    {
        private readonly IMatchCommentSubscriptionDataMigrator _matchCommentSubscriptionDataMigrator;

        public MatchCommentSubscriptionMigrationController(IGlobalSettings globalSettings,
            IUmbracoContextAccessor umbracoContextAccessor,
            ISqlContext sqlContext,
            ServiceContext serviceContext,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IRuntimeState runtimeState,
            UmbracoHelper umbracoHelper,
            IMatchCommentSubscriptionDataMigrator matchCommentSubscriptionDataMigrator) :
            base(globalSettings, umbracoContextAccessor, sqlContext, serviceContext, appCaches, profilingLogger, runtimeState, umbracoHelper)
        {
            _matchCommentSubscriptionDataMigrator = matchCommentSubscriptionDataMigrator ?? throw new ArgumentNullException(nameof(matchCommentSubscriptionDataMigrator));
        }

        [HttpPost]
        public async Task<IHttpActionResult> CreateMatchCommentSubscription(MigratedMatchCommentSubscription[] subscriptions)
        {
            if (subscriptions is null)
            {
                throw new ArgumentNullException(nameof(subscriptions));
            }

            foreach (var subscription in subscriptions)
            {
                await _matchCommentSubscriptionDataMigrator.MigrateMatchCommentSubscription(subscription).ConfigureAwait(false);
            }
            return Created("/", JsonConvert.SerializeObject(subscriptions));
        }

        [HttpDelete]
        public async Task<IHttpActionResult> DeleteMatchCommentSubscriptions()
        {
            await _matchCommentSubscriptionDataMigrator.DeleteMatchCommentSubscriptions().ConfigureAwait(false);
            return Ok();
        }
    }
}
