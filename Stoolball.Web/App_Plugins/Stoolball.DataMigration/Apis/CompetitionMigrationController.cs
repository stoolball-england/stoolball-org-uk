using Newtonsoft.Json;
using Stoolball.Competitions;
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
    public class CompetitionMigrationController : UmbracoAuthorizedJsonController
    {
        private readonly ICompetitionDataMigrator _competitionDataMigrator;

        public CompetitionMigrationController(IGlobalSettings globalSettings,
            IUmbracoContextAccessor umbracoContextAccessor,
            ISqlContext sqlContext,
            ServiceContext serviceContext,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IRuntimeState runtimeState,
            UmbracoHelper umbracoHelper,
            ICompetitionDataMigrator competitionDataMigrator) :
            base(globalSettings, umbracoContextAccessor, sqlContext, serviceContext, appCaches, profilingLogger, runtimeState, umbracoHelper)
        {
            _competitionDataMigrator = competitionDataMigrator;
        }

        [HttpPost]
        public async Task<IHttpActionResult> CreateCompetition(Competition competition)
        {
            if (competition is null)
            {
                throw new ArgumentNullException(nameof(competition));
            }

            var migrated = await _competitionDataMigrator.MigrateCompetition(competition).ConfigureAwait(false);
            return Created(new Uri(Request.RequestUri, new Uri(migrated.CompetitionRoute, UriKind.Relative)), JsonConvert.SerializeObject(migrated));
        }

        [HttpDelete]
        public async Task<IHttpActionResult> DeleteCompetitions()
        {
            await _competitionDataMigrator.DeleteCompetitions().ConfigureAwait(false);
            return Ok();
        }


        [HttpPost]
        public async Task<IHttpActionResult> CreateSeason(Season season)
        {
            if (season is null)
            {
                throw new ArgumentNullException(nameof(season));
            }

            var migrated = await _competitionDataMigrator.MigrateSeason(season).ConfigureAwait(false);
            return Created(new Uri(Request.RequestUri, new Uri(migrated.SeasonRoute, UriKind.Relative)), JsonConvert.SerializeObject(migrated));
        }

        [HttpDelete]
        public async Task<IHttpActionResult> DeleteSeasons()
        {
            await _competitionDataMigrator.DeleteSeasons().ConfigureAwait(false);
            return Ok();
        }
    }
}
