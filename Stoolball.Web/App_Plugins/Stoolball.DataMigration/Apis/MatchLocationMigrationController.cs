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
    public class MatchLocationMigrationController : UmbracoAuthorizedJsonController
    {
        private readonly IMatchLocationDataMigrator _matchLocationDataMigrator;
        private readonly IRouteGenerator _routeGenerator;

        public MatchLocationMigrationController(IGlobalSettings globalSettings,
            IUmbracoContextAccessor umbracoContextAccessor,
            ISqlContext sqlContext,
            ServiceContext serviceContext,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IRuntimeState runtimeState,
            UmbracoHelper umbracoHelper,
            IMatchLocationDataMigrator matchLocationDataMigrator,
            IRouteGenerator routeGenerator) :
            base(globalSettings, umbracoContextAccessor, sqlContext, serviceContext, appCaches, profilingLogger, runtimeState, umbracoHelper)
        {
            _matchLocationDataMigrator = matchLocationDataMigrator ?? throw new ArgumentNullException(nameof(matchLocationDataMigrator));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
        }

        [HttpPost]
        public async Task<IHttpActionResult> CreateMatchLocation(MigratedMatchLocation matchLocation)
        {
            if (matchLocation is null)
            {
                throw new ArgumentNullException(nameof(matchLocation));
            }

            // Create an owner group
            var groupName = _routeGenerator.GenerateRoute("location", matchLocation.NameAndLocalityOrTownIfDifferent(), NoiseWords.MatchLocationRoute);
            var group = Services.MemberGroupService.GetByName(groupName);
            if (group == null)
            {
                group = new MemberGroup
                {
                    Name = groupName
                };
                Services.MemberGroupService.Save(group);
            }
            matchLocation.MemberGroupId = group.Id;
            matchLocation.MemberGroupName = group.Name;

            var migrated = await _matchLocationDataMigrator.MigrateMatchLocation(matchLocation).ConfigureAwait(false);
            return Created(new Uri(Request.RequestUri, new Uri(migrated.MatchLocationRoute, UriKind.Relative)), JsonConvert.SerializeObject(migrated));
        }

        [HttpDelete]
        public async Task<IHttpActionResult> DeleteMatchLocations()
        {
            await _matchLocationDataMigrator.DeleteMatchLocations().ConfigureAwait(false);
            return Ok();
        }
    }
}
