using Newtonsoft.Json;
using Stoolball.Matches;
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
        public async Task<IHttpActionResult> CreateBatting(Batting batting)
        {
            if (batting is null)
            {
                throw new ArgumentNullException(nameof(batting));
            }

            var migrated = await _playerPerformanceDataMigrator.MigrateBatting(batting).ConfigureAwait(false);
            return Created(migrated.EntityUri, JsonConvert.SerializeObject(migrated));
        }

        [HttpDelete]
        public async Task<IHttpActionResult> DeleteBatting()
        {
            await _playerPerformanceDataMigrator.DeleteBatting().ConfigureAwait(false);
            return Ok();
        }

        [HttpPost]
        public async Task<IHttpActionResult> CreateBowling(BowlingOver bowling)
        {
            if (bowling is null)
            {
                throw new ArgumentNullException(nameof(bowling));
            }

            var migrated = await _playerPerformanceDataMigrator.MigrateBowling(bowling).ConfigureAwait(false);
            return Created(migrated.EntityUri, JsonConvert.SerializeObject(migrated));
        }

        [HttpDelete]
        public async Task<IHttpActionResult> DeleteBowling()
        {
            await _playerPerformanceDataMigrator.DeleteBowling().ConfigureAwait(false);
            return Ok();
        }
    }
}
