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
    public class MatchMigrationController : UmbracoAuthorizedJsonController
    {
        private readonly IMatchDataMigrator _matchDataMigrator;

        public MatchMigrationController(IGlobalSettings globalSettings,
            IUmbracoContextAccessor umbracoContextAccessor,
            ISqlContext sqlContext,
            ServiceContext serviceContext,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IRuntimeState runtimeState,
            UmbracoHelper umbracoHelper,
            IMatchDataMigrator matchDataMigrator) :
            base(globalSettings, umbracoContextAccessor, sqlContext, serviceContext, appCaches, profilingLogger, runtimeState, umbracoHelper)
        {
            _matchDataMigrator = matchDataMigrator;
        }

        [HttpPost]
        public async Task<IHttpActionResult> CreateMatch(MigratedMatch[] matches)
        {
            if (matches is null)
            {
                throw new ArgumentNullException(nameof(matches));
            }

            foreach (var match in matches)
            {
                await _matchDataMigrator.MigrateMatch(match).ConfigureAwait(false);
            }
            return Created(new Uri(Request.RequestUri, new Uri("/matches", UriKind.Relative)), JsonConvert.SerializeObject(matches));
        }

        [HttpDelete]
        public async Task<IHttpActionResult> DeleteMatches()
        {
            await _matchDataMigrator.DeleteMatches().ConfigureAwait(false);
            return Ok();
        }
    }
}
