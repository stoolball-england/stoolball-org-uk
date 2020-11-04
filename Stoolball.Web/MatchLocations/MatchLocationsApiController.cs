using Stoolball.MatchLocations;
using Stoolball.Web.WebApi;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Mapping;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.WebApi;

namespace Stoolball.Web.MatchLocations
{
    public class MatchLocationsApiController : UmbracoApiController
    {
        private readonly IMatchLocationDataSource _locationDataSource;

        public MatchLocationsApiController(IGlobalSettings globalSettings, IUmbracoContextAccessor umbracoContextAccessor, ISqlContext sqlContext, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IRuntimeState runtimeState, UmbracoHelper umbracoHelper, UmbracoMapper umbracoMapper,
            IMatchLocationDataSource locationDataSource) :
            base(globalSettings, umbracoContextAccessor, sqlContext, serviceContext, appCaches, profilingLogger, runtimeState, umbracoHelper, umbracoMapper)
        {
            _locationDataSource = locationDataSource ?? throw new ArgumentNullException(nameof(locationDataSource));
        }

        [HttpGet]
        [Route("api/locations/autocomplete")]
        public async Task<AutocompleteResultSet> Autocomplete([FromUri] string query, [FromUri] string[] not)
        {
            if (not is null)
            {
                throw new ArgumentNullException(nameof(not));
            }

            var locationQuery = new MatchLocationQuery { Query = query };
            foreach (var guid in not)
            {
                if (guid == null) continue;

                try
                {
                    locationQuery.ExcludeMatchLocationIds.Add(new Guid(guid));
                }
                catch (FormatException)
                {
                    // ignore that one
                }
            }
            var locations = await _locationDataSource.ReadMatchLocations(locationQuery).ConfigureAwait(false);
            return new AutocompleteResultSet
            {
                suggestions = locations.Select(x => new AutocompleteResult
                {
                    value = x.NameAndLocalityOrTownIfDifferent(),
                    data = x.MatchLocationId.ToString()
                })
            };
        }
    }
}