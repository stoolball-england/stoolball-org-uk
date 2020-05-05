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
    public class ClubMigrationController : UmbracoAuthorizedJsonController
    {
        private readonly IClubDataMigrator _clubDataMigrator;

        public ClubMigrationController(IGlobalSettings globalSettings,
            IUmbracoContextAccessor umbracoContextAccessor,
            ISqlContext sqlContext,
            ServiceContext serviceContext,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IRuntimeState runtimeState,
            UmbracoHelper umbracoHelper,
            IClubDataMigrator clubDataMigrator) :
            base(globalSettings, umbracoContextAccessor, sqlContext, serviceContext, appCaches, profilingLogger, runtimeState, umbracoHelper)
        {
            _clubDataMigrator = clubDataMigrator;
        }

        [HttpPost]
        public async Task<IHttpActionResult> CreateClub(MigratedClub club)
        {
            if (club is null)
            {
                throw new ArgumentNullException(nameof(club));
            }

            var migrated = await _clubDataMigrator.MigrateClub(club).ConfigureAwait(false);
            return Created(new Uri(Request.RequestUri, new Uri(migrated.ClubRoute, UriKind.Relative)), JsonConvert.SerializeObject(migrated));
        }

        [HttpDelete]
        public async Task<IHttpActionResult> DeleteClubs()
        {
            await _clubDataMigrator.DeleteClubs().ConfigureAwait(false);
            return Ok();
        }
    }
}
