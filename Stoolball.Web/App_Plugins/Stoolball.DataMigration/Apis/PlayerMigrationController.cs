using Newtonsoft.Json;
using Stoolball.Teams;
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
    public class PlayerMigrationController : UmbracoAuthorizedJsonController
    {
        private readonly IPlayerDataMigrator _playerDataMigrator;

        public PlayerMigrationController(IGlobalSettings globalSettings,
            IUmbracoContextAccessor umbracoContextAccessor,
            ISqlContext sqlContext,
            ServiceContext serviceContext,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IRuntimeState runtimeState,
            UmbracoHelper umbracoHelper,
            IPlayerDataMigrator playerDataMigrator) :
            base(globalSettings, umbracoContextAccessor, sqlContext, serviceContext, appCaches, profilingLogger, runtimeState, umbracoHelper)
        {
            _playerDataMigrator = playerDataMigrator;
        }

        [HttpPost]
        public async Task<IHttpActionResult> CreatePlayer(PlayerIdentity player)
        {
            if (player is null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            var migrated = await _playerDataMigrator.MigratePlayer(player).ConfigureAwait(false);
            return Created(new Uri(Request.RequestUri, new Uri(migrated.PlayerIdentityRoute, UriKind.Relative)), JsonConvert.SerializeObject(migrated));
        }

        [HttpDelete]
        public async Task<IHttpActionResult> DeletePlayers()
        {
            await _playerDataMigrator.DeletePlayers().ConfigureAwait(false);
            return Ok();
        }
    }
}
