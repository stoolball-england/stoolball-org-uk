using Stoolball.Umbraco.Data.Teams;
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

namespace Stoolball.Web.Teams
{
    public class TeamsApiController : UmbracoApiController
    {
        private readonly ITeamDataSource _teamDataSource;

        public TeamsApiController(IGlobalSettings globalSettings, IUmbracoContextAccessor umbracoContextAccessor, ISqlContext sqlContext, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IRuntimeState runtimeState, UmbracoHelper umbracoHelper, UmbracoMapper umbracoMapper, ITeamDataSource teamDataSource) :
            base(globalSettings, umbracoContextAccessor, sqlContext, serviceContext, appCaches, profilingLogger, runtimeState, umbracoHelper, umbracoMapper)
        {
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
        }

        [HttpGet]
        [Route("api/teams/autocomplete")]
        public async Task<AutocompleteResultSet> Autocomplete([FromUri] string query, [FromUri] string[] not)
        {
            if (not is null)
            {
                throw new ArgumentNullException(nameof(not));
            }

            var teamQuery = new TeamQuery { Query = query };
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
            var teams = await _teamDataSource.ReadTeams(teamQuery).ConfigureAwait(false);
            return new AutocompleteResultSet
            {
                suggestions = teams.Select(x => new AutocompleteResult
                {
                    value = (x.UntilYear.HasValue ? x.TeamName + " (no longer active)" : x.TeamName),
                    data = x.TeamId.ToString()
                })
            };
        }
    }
}