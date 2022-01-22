using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.MatchLocations;
using Stoolball.Teams;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.WebApi
{
    public class MatchLocationsApiController : UmbracoApiController
    {
        private readonly IMatchLocationDataSource _locationDataSource;

        public MatchLocationsApiController(IMatchLocationDataSource locationDataSource) : base()
        {
            _locationDataSource = locationDataSource ?? throw new ArgumentNullException(nameof(locationDataSource));
        }

        [HttpGet]
        [Route("api/locations/autocomplete")]
        public async Task<AutocompleteResultSet> Autocomplete([FromQuery] string? query = null, [FromQuery] string[]? not = null, [FromQuery] string[]? teamType = null, [FromQuery] bool? hasActiveTeams = null, [FromQuery] Guid? season = null)
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
        public async Task<IEnumerable<MatchLocationResult>> Map([FromQuery] string? query = null, [FromQuery] string[]? not = null, [FromQuery] string[]? teamType = null, [FromQuery] bool? hasActiveTeams = null, [FromQuery] Guid? season = null)
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

        private async Task<List<MatchLocation>> QueryMatchLocations(string? query, string[]? not, bool? hasActiveTeams, string[]? teamTypes, Guid? seasonId)
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