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
    public class PlayerPerformanceMigrationController : UmbracoAuthorizedJsonController
    {
        private readonly IPlayerPerformanceDataMigrator _playerPerformanceDataMigrator;

        public PlayerPerformanceMigrationController(IGlobalSettings globalSettings,
            IUmbracoContextAccessor umbracoContextAccessor,
            ISqlContext sqlContext,
            ServiceContext serviceContext,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IRuntimeState runtimeState,
            UmbracoHelper umbracoHelper,
            IPlayerPerformanceDataMigrator playerPerformanceDataMigrator) :
            base(globalSettings, umbracoContextAccessor, sqlContext, serviceContext, appCaches, profilingLogger, runtimeState, umbracoHelper)
        {
            _playerPerformanceDataMigrator = playerPerformanceDataMigrator;
        }

        [HttpPost]
        public async Task<IHttpActionResult> CreatePlayerInnings(MigratedPlayerInnings[] multipleInnings)
        {
            if (multipleInnings is null)
            {
                throw new ArgumentNullException(nameof(multipleInnings));
            }

            foreach (var innings in multipleInnings)
            {
                await _playerPerformanceDataMigrator.MigratePlayerInnings(innings).ConfigureAwait(false);
            }
            return Created("/matches", JsonConvert.SerializeObject(multipleInnings));
        }

        [HttpDelete]
        public async Task<IHttpActionResult> DeletePlayerInnings()
        {
            await _playerPerformanceDataMigrator.DeletePlayerInnings().ConfigureAwait(false);
            return Ok();
        }

        [HttpPost]
        public async Task<IHttpActionResult> CreateOver(MigratedOver[] multipleOvers)
        {
            if (multipleOvers is null)
            {
                throw new ArgumentNullException(nameof(multipleOvers));
            }

            foreach (var over in multipleOvers)
            {
                await _playerPerformanceDataMigrator.MigrateOver(over).ConfigureAwait(false);
            }
            return Created("/matches", JsonConvert.SerializeObject(multipleOvers));
        }

        [HttpDelete]
        public async Task<IHttpActionResult> DeleteOvers()
        {
            await _playerPerformanceDataMigrator.DeleteOvers().ConfigureAwait(false);
            return Ok();
        }
    }
}
