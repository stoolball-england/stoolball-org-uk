using Newtonsoft.Json;
using Stoolball.Routing;
using Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators;
using System;
using System.Threading.Tasks;
using System.Web.Http;
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
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.Apis
{
    [PluginController("Migration")]
    public class TeamMigrationController : UmbracoAuthorizedJsonController
    {
        private readonly ITeamDataMigrator _teamDataMigrator;
        private readonly IRouteGenerator _routeGenerator;

        public TeamMigrationController(IGlobalSettings globalSettings,
            IUmbracoContextAccessor umbracoContextAccessor,
            ISqlContext sqlContext,
            ServiceContext serviceContext,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IRuntimeState runtimeState,
            UmbracoHelper umbracoHelper,
            ITeamDataMigrator teamDataMigrator,
            IRouteGenerator routeGenerator) :
            base(globalSettings, umbracoContextAccessor, sqlContext, serviceContext, appCaches, profilingLogger, runtimeState, umbracoHelper)
        {
            _teamDataMigrator = teamDataMigrator ?? throw new ArgumentNullException(nameof(teamDataMigrator));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
        }

        [HttpPost]
        public async Task<IHttpActionResult> CreateTeam(MigratedTeam team)
        {
            if (team is null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            // There should already be a migrated group - update the group name
            var groupName = _routeGenerator.GenerateRoute("team", team.TeamName, NoiseWords.TeamRoute);
            var group = Services.MemberGroupService.GetByName("team/" + team.TeamRoute);
            if (group != null)
            {
                group.Name = groupName;
                Services.MemberGroupService.Save(group);
            }
            else
            {
                // Maybe it's there and already renamed from a previous import
                group = Services.MemberGroupService.GetByName(groupName);

                // If neither name matched, it needs to be created
                if (group == null)
                {
                    group = new MemberGroup
                    {
                        Name = groupName
                    };
                    Services.MemberGroupService.Save(group);
                }
            }
            team.MemberGroupId = group.Id;
            team.MemberGroupName = group.Name;

            var migrated = await _teamDataMigrator.MigrateTeam(team).ConfigureAwait(false);
            return Created(new Uri(Request.RequestUri, new Uri(migrated.TeamRoute, UriKind.Relative)), JsonConvert.SerializeObject(migrated));
        }

        [HttpDelete]
        public async Task<IHttpActionResult> DeleteTeams()
        {
            await _teamDataMigrator.DeleteTeams().ConfigureAwait(false);
            return Ok();
        }
    }
}
