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
    public class SchoolMigrationController : UmbracoAuthorizedJsonController
    {
        private readonly ISchoolDataMigrator _schoolDataMigrator;
        private readonly IRouteGenerator _routeGenerator;

        public SchoolMigrationController(IGlobalSettings globalSettings,
            IUmbracoContextAccessor umbracoContextAccessor,
            ISqlContext sqlContext,
            ServiceContext serviceContext,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IRuntimeState runtimeState,
            UmbracoHelper umbracoHelper,
            ISchoolDataMigrator schoolDataMigrator,
            IRouteGenerator routeGenerator) :
            base(globalSettings, umbracoContextAccessor, sqlContext, serviceContext, appCaches, profilingLogger, runtimeState, umbracoHelper)
        {
            _schoolDataMigrator = schoolDataMigrator ?? throw new ArgumentNullException(nameof(schoolDataMigrator));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
        }

        [HttpPost]
        public async Task<IHttpActionResult> CreateSchool(MigratedSchool[] schools)
        {
            if (schools is null)
            {
                throw new ArgumentNullException(nameof(schools));
            }

            foreach (var school in schools)
            {
                // Create an owner group
                var groupName = _routeGenerator.GenerateRoute("school", school.SchoolName, NoiseWords.SchoolRoute);
                var group = Services.MemberGroupService.GetByName(groupName);
                if (group == null)
                {
                    group = new MemberGroup
                    {
                        Name = groupName
                    };
                    Services.MemberGroupService.Save(group);
                }
                school.MemberGroupKey = group.Key;
                school.MemberGroupName = group.Name;

                await _schoolDataMigrator.MigrateSchool(school).ConfigureAwait(false);
            }
            return Created(new Uri(Request.RequestUri, new Uri("/schools", UriKind.Relative)), JsonConvert.SerializeObject(schools));
        }

        [HttpDelete]
        public async Task<IHttpActionResult> DeleteSchools()
        {
            await _schoolDataMigrator.DeleteSchools().ConfigureAwait(false);
            return Ok();
        }
    }
}
