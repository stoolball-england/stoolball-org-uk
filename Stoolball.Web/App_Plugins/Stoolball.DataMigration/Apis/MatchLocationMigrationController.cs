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
        public async Task<IHttpActionResult> CreateMatchLocation(MigratedMatchLocation[] matchLocations)
        {
            if (matchLocations is null)
            {
                throw new ArgumentNullException(nameof(matchLocations));
            }

            foreach (var matchLocation in matchLocations)
            {
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
                matchLocation.MemberGroupKey = group.Key;
                matchLocation.MemberGroupName = group.Name;

                await _matchLocationDataMigrator.MigrateMatchLocation(matchLocation).ConfigureAwait(false);
            }
            return Created(new Uri(Request.RequestUri, new Uri("/locations", UriKind.Relative)), JsonConvert.SerializeObject(matchLocations));
        }

        [HttpDelete]
        public async Task<IHttpActionResult> DeleteMatchLocations()
        {
            await _matchLocationDataMigrator.DeleteMatchLocations().ConfigureAwait(false);
            return Ok();
        }
    }
}
