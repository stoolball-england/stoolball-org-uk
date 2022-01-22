using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Competitions;
using Stoolball.Matches;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.WebApi
{
    public class SeasonsApiController : UmbracoApiController
    {
        private readonly ISeasonDataSource _seasonDataSource;

        public SeasonsApiController(ISeasonDataSource seasonDataSource) : base()
        {
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
        }

        [HttpGet]
        [Route("api/seasons/autocomplete")]
        public async Task<AutocompleteResultSet> Autocomplete([FromQuery] string? query = null, [FromQuery] string[]? matchType = null)
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