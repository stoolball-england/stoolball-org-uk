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
    public class TournamentMigrationController : UmbracoAuthorizedJsonController
    {
        private readonly ITournamentDataMigrator _tournamentDataMigrator;

        public TournamentMigrationController(IGlobalSettings globalSettings,
            IUmbracoContextAccessor umbracoContextAccessor,
            ISqlContext sqlContext,
            ServiceContext serviceContext,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IRuntimeState runtimeState,
            UmbracoHelper umbracoHelper,
            ITournamentDataMigrator tournamentDataMigrator) :
            base(globalSettings, umbracoContextAccessor, sqlContext, serviceContext, appCaches, profilingLogger, runtimeState, umbracoHelper)
        {
            _tournamentDataMigrator = tournamentDataMigrator;
        }

        [HttpPost]
        public async Task<IHttpActionResult> CreateTournament(MigratedTournament[] tournaments)
        {
            if (tournaments is null)
            {
                throw new ArgumentNullException(nameof(tournaments));
            }

            foreach (var tournament in tournaments)
            {
                await _tournamentDataMigrator.MigrateTournament(tournament).ConfigureAwait(false);
            }
            return Created(new Uri(Request.RequestUri, new Uri("/tournaments", UriKind.Relative)), JsonConvert.SerializeObject(tournaments));
        }

        [HttpDelete]
        public async Task<IHttpActionResult> DeleteTournaments()
        {
            await _tournamentDataMigrator.DeleteTournaments().ConfigureAwait(false);
            return Ok();
        }
    }
}
