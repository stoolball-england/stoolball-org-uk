using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Teams;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.WebApi
{
    public class TeamsApiController : UmbracoApiController
    {
        private readonly ITeamDataSource _teamDataSource;

        public TeamsApiController(ITeamDataSource teamDataSource) :
            base()
        {
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
        }

        [HttpGet]
        [Route("api/teams/autocomplete")]
        public async Task<AutocompleteResultSet> Autocomplete([FromQuery] string query, [FromQuery] string[] not, [FromQuery] string[] teamType, [FromQuery] bool includeClubTeams = true)
        {
            if (not is null)
            {
                throw new ArgumentNullException(nameof(not));
            }

            if (teamType is null)
            {
                throw new ArgumentNullException(nameof(teamType));
            }

            var teamQuery = new TeamFilter { Query = query };
            teamQuery.IncludeClubTeams = includeClubTeams;

            foreach (var guid in not)
            {
                if (guid == null) continue;

                try
                {
                    teamQuery.ExcludeTeamIds.Add(new Guid(guid));
                }
                catch (FormatException)
                {
                    // ignore that one
                }
            }

            foreach (var unparsedType in teamType)
            {
                if (Enum.TryParse<TeamType>(unparsedType, out var parsedType))
                {
                    teamQuery.TeamTypes.Add(parsedType);
                }
            }

            var teams = await _teamDataSource.ReadTeams(teamQuery).ConfigureAwait(false);
            return new AutocompleteResultSet
            {
                suggestions = teams.Select(x => new AutocompleteResult
                {
                    value = x.UntilYear.HasValue ? x.TeamName + " (no longer active)" : x.TeamName,
                    data = x.TeamId.ToString()
                })
            };
        }
    }
}