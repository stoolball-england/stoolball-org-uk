using System;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json;
using Stoolball.Routing;
using Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators;
using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Editors;
using Umbraco.Web.Mvc;
using static Stoolball.Data.SqlServer.Constants;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.Apis
{
    [PluginController("Migration")]
    public class CompetitionMigrationController : UmbracoAuthorizedJsonController
    {
        private readonly ICompetitionDataMigrator _competitionDataMigrator;
        private readonly IRouteGenerator _routeGenerator;

        public CompetitionMigrationController(IGlobalSettings globalSettings,
            IUmbracoContextAccessor umbracoContextAccessor,
            ISqlContext sqlContext,
            ServiceContext serviceContext,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IRuntimeState runtimeState,
            UmbracoHelper umbracoHelper,
            ICompetitionDataMigrator competitionDataMigrator,
            IRouteGenerator routeGenerator) :
            base(globalSettings, umbracoContextAccessor, sqlContext, serviceContext, appCaches, profilingLogger, runtimeState, umbracoHelper)
        {
            _competitionDataMigrator = competitionDataMigrator ?? throw new ArgumentNullException(nameof(competitionDataMigrator));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
        }

        [HttpPost]
        public async Task<IHttpActionResult> CreateCompetition(MigratedCompetition[] competitions)
        {
            if (competitions is null)
            {
                throw new ArgumentNullException(nameof(competitions));
            }

            foreach (var competition in competitions)
            {
                // Create an owner group
                var groupName = _routeGenerator.GenerateRoute("competition", competition.CompetitionName, NoiseWords.CompetitionRoute);
                var group = Services.MemberGroupService.GetByName(groupName);
                if (group == null)
                {
                    group = new MemberGroup
                    {
                        Name = groupName
                    };
                    Services.MemberGroupService.Save(group);
                }
                competition.MemberGroupKey = group.Key;
                competition.MemberGroupName = group.Name;

                await _competitionDataMigrator.MigrateCompetition(competition).ConfigureAwait(false);
            }
            return Created(new Uri(Request.RequestUri, new Uri("/competitions", UriKind.Relative)), JsonConvert.SerializeObject(competitions));
        }

        [HttpDelete]
        public async Task<IHttpActionResult> DeleteCompetitions()
        {
            await _competitionDataMigrator.DeleteCompetitions().ConfigureAwait(false);
            return Ok();
        }


        [HttpPost]
        public async Task<IHttpActionResult> CreateSeason(MigratedSeason[] seasons)
        {
            if (seasons is null)
            {
                throw new ArgumentNullException(nameof(seasons));
            }

            foreach (var season in seasons)
            {
                await _competitionDataMigrator.MigrateSeason(season).ConfigureAwait(false);
            }
            return Created(new Uri(Request.RequestUri, new Uri("/competitions", UriKind.Relative)), JsonConvert.SerializeObject(seasons));
        }

        [HttpDelete]
        public async Task<IHttpActionResult> DeleteSeasons()
        {
            await _competitionDataMigrator.DeleteSeasons().ConfigureAwait(false);
            return Ok();
        }
    }
}
