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
    public class MatchCommentMigrationController : UmbracoAuthorizedJsonController
    {
        private readonly IMatchCommentDataMigrator _matchCommentDataMigrator;

        public MatchCommentMigrationController(IGlobalSettings globalSettings,
            IUmbracoContextAccessor umbracoContextAccessor,
            ISqlContext sqlContext,
            ServiceContext serviceContext,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IRuntimeState runtimeState,
            UmbracoHelper umbracoHelper,
            IMatchCommentDataMigrator matchCommentDataMigrator) :
            base(globalSettings, umbracoContextAccessor, sqlContext, serviceContext, appCaches, profilingLogger, runtimeState, umbracoHelper)
        {
            _matchCommentDataMigrator = matchCommentDataMigrator ?? throw new ArgumentNullException(nameof(matchCommentDataMigrator));
        }

        [HttpPost]
        public async Task<IHttpActionResult> CreateMatchComment(MigratedMatchComment[] comments)
        {
            if (comments is null)
            {
                throw new ArgumentNullException(nameof(comments));
            }

            foreach (var comment in comments)
            {
                await _matchCommentDataMigrator.MigrateMatchComment(comment).ConfigureAwait(false);
            }
            return Created("/matches", JsonConvert.SerializeObject(comments));
        }

        [HttpDelete]
        public async Task<IHttpActionResult> DeleteMatchComments()
        {
            await _matchCommentDataMigrator.DeleteMatchComments().ConfigureAwait(false);
            return Ok();
        }
    }
}
