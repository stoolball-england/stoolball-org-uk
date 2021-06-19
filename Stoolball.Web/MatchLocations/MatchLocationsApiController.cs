using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Stoolball.MatchLocations;
using Stoolball.Teams;
using Stoolball.Web.WebApi;
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
        public async Task<AutocompleteResultSet> Autocomplete([FromUri] string query = null, [FromUri] string[] not = null, [FromUri] string[] teamType = null, [FromUri] bool? hasActiveTeams = null, [FromUri] Guid? season = null)
        {
            var locations = await QueryMatchLocations(query, not, hasActiveTeams, teamType, season).ConfigureAwait(false);

            return new AutocompleteResultSet
            {
                suggestions = locations.Select(x => new AutocompleteResult
                {
                    value = x.NameAndLocalityOrTownIfDifferent(),
                    data = x.MatchLocationId.ToString()
                })
            };
        }

        [HttpGet]
        [Route("api/locations/map")]
        public async Task<IEnumerable<MatchLocationResult>> Map([FromUri] string query = null, [FromUri] string[] not = null, [FromUri] string[] teamType = null, [FromUri] bool? hasActiveTeams = null, [FromUri] Guid? season = null)
        {
            var locations = await QueryMatchLocations(query, not, hasActiveTeams, teamType, season).ConfigureAwait(false);

            return locations.Select(x => new MatchLocationResult
            {
                name = x.NameAndLocalityOrTownIfDifferent(),
                latitude = x.Latitude,
                longitude = x.Longitude,
                teams = x.Teams.Select(t => new TeamResult
                {
                    name = t.TeamNameAndPlayerType(),
                    route = t.TeamRoute,
                    active = !t.UntilYear.HasValue
                })
            });
        }

        private async Task<List<MatchLocation>> QueryMatchLocations(string query, string[] not, bool? hasActiveTeams, string[] teamTypes, Guid? seasonId)
        {
            var filter = new MatchLocationFilter { Query = query, HasActiveTeams = hasActiveTeams };
            if (not != null)
            {
                foreach (var guid in not)
                {
                    if (guid == null) continue;

                    try
                    {
                        filter.ExcludeMatchLocationIds.Add(new Guid(guid));
                    }
                    catch (FormatException)
                    {
                        // ignore that one
                    }
                }
            }

            if (teamTypes != null)
            {
                foreach (var teamType in teamTypes)
                {
                    if (Enum.TryParse<TeamType>(teamType, true, out var parsedType))
                    {
                        filter.TeamTypes.Add(parsedType);
                    }
                }
            }

            if (seasonId.HasValue)
            {
                filter.SeasonIds.Add(seasonId.Value);
            }

            return await _locationDataSource.ReadMatchLocations(filter).ConfigureAwait(false);
        }
    }
}