using Stoolball.Competitions;
using Stoolball.Matches;
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

namespace Stoolball.Web.Competitions
{
    public class SeasonsApiController : UmbracoApiController
    {
        private readonly ISeasonDataSource _seasonDataSource;

        public SeasonsApiController(IGlobalSettings globalSettings, IUmbracoContextAccessor umbracoContextAccessor, ISqlContext sqlContext, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IRuntimeState runtimeState, UmbracoHelper umbracoHelper, UmbracoMapper umbracoMapper, ISeasonDataSource seasonDataSource) :
            base(globalSettings, umbracoContextAccessor, sqlContext, serviceContext, appCaches, profilingLogger, runtimeState, umbracoHelper, umbracoMapper)
        {
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
        }

        [HttpGet]
        [Route("api/seasons/autocomplete")]
        public async Task<AutocompleteResultSet> Autocomplete([FromUri] string query = null, [FromUri] string[] matchType = null)
        {
            var competitionQuery = new CompetitionFilter { Query = query };
            if (matchType != null)
            {
                foreach (var mt in matchType)
                {
                    if (mt == null) continue;

                    try
                    {
                        competitionQuery.MatchTypes.Add((MatchType)Enum.Parse(typeof(MatchType), mt, true));
                    }
                    catch (ArgumentException)
                    {
                        // ignore that one
                    }
                }
            }

            var seasons = await _seasonDataSource.ReadSeasons(competitionQuery).ConfigureAwait(false);
            return new AutocompleteResultSet
            {
                suggestions = seasons.Select(x => new AutocompleteResult
                {
                    value = x.SeasonFullName(),
                    data = x.SeasonId.ToString()
                })
            };
        }
    }
}