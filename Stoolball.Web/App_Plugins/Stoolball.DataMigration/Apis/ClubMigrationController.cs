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
    public class ClubMigrationController : UmbracoAuthorizedJsonController
    {
        private readonly IClubDataMigrator _clubDataMigrator;
        private readonly IRouteGenerator _routeGenerator;

        public ClubMigrationController(IGlobalSettings globalSettings,
            IUmbracoContextAccessor umbracoContextAccessor,
            ISqlContext sqlContext,
            ServiceContext serviceContext,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IRuntimeState runtimeState,
            UmbracoHelper umbracoHelper,
            IClubDataMigrator clubDataMigrator,
            IRouteGenerator routeGenerator) :
            base(globalSettings, umbracoContextAccessor, sqlContext, serviceContext, appCaches, profilingLogger, runtimeState, umbracoHelper)
        {
            _clubDataMigrator = clubDataMigrator ?? throw new ArgumentNullException(nameof(clubDataMigrator));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
        }

        [HttpPost]
        public async Task<IHttpActionResult> CreateClub(MigratedClub club)
        {
            if (club is null)
            {
                throw new ArgumentNullException(nameof(club));
            }


            // Create an owner group
            var groupName = _routeGenerator.GenerateRoute("club", club.ClubName, NoiseWords.ClubRoute);
            var group = Services.MemberGroupService.GetByName(groupName);
            if (group == null)
            {
                group = new MemberGroup
                {
                    Name = groupName
                };
                Services.MemberGroupService.Save(group);
            }
            club.MemberGroupId = group.Id;
            club.MemberGroupName = group.Name;

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
