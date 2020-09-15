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
        public async Task<IHttpActionResult> CreatePlayer(MigratedPlayerIdentity[] players)
        {
            if (players is null)
            {
                throw new ArgumentNullException(nameof(players));
            }

            foreach (var player in players)
            {
                await _playerDataMigrator.MigratePlayer(player).ConfigureAwait(false);
            }
            return Created(new Uri(Request.RequestUri, new Uri("/players", UriKind.Relative)), JsonConvert.SerializeObject(players));
        }

        [HttpDelete]
        public async Task<IHttpActionResult> DeletePlayers()
        {
            await _playerDataMigrator.DeletePlayers().ConfigureAwait(false);
            return Ok();
        }
    }
}
