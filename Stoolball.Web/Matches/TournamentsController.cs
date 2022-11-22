using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Matches
{
    public class TournamentsController : RenderController, IRenderControllerAsync
    {
        private readonly IMatchListingDataSource _matchesDataSource;
        private readonly IMatchFilterQueryStringParser _matchFilterQueryStringParser;
        private readonly IMatchFilterHumanizer _matchFilterHumanizer;

        public TournamentsController(ILogger<TournamentsController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IMatchListingDataSource matchesDataSource,
            IMatchFilterQueryStringParser matchFilterQueryStringParser,
            IMatchFilterHumanizer matchFilterHumanizer)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _matchesDataSource = matchesDataSource ?? throw new ArgumentNullException(nameof(matchesDataSource));
            _matchFilterQueryStringParser = matchFilterQueryStringParser ?? throw new ArgumentNullException(nameof(matchFilterQueryStringParser));
            _matchFilterHumanizer = matchFilterHumanizer ?? throw new ArgumentNullException(nameof(matchFilterHumanizer));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var model = new MatchListingViewModel(CurrentPage)
            {
                DefaultMatchFilter = new MatchFilter
                {
                    FromDate = DateTimeOffset.UtcNow.Date,
                    IncludeMatches = false,
                    IncludeTournamentMatches = false,
                    IncludeTournaments = true,
                    Paging = new Paging
                    {
                        PageNumber = (Request.Query.ContainsKey("page") && int.TryParse(Request.Query["page"], out var pageNumber)) ? pageNumber > 0 ? pageNumber : 1 : 1,
                        PageSize = Constants.Defaults.PageSize,
                        PageUrl = new Uri(Request.GetEncodedUrl())
                    }
                }
            };
            model.AppliedMatchFilter = _matchFilterQueryStringParser.ParseQueryString(model.DefaultMatchFilter, Request.QueryString.Value);
            model.AppliedMatchFilter.Paging.Total = await _matchesDataSource.ReadTotalMatches(model.AppliedMatchFilter);
            model.Matches = await _matchesDataSource.ReadMatchListings(model.AppliedMatchFilter, MatchSortOrder.MatchDateEarliestFirst);

            var userFilter = _matchFilterHumanizer.MatchingFilter(model.AppliedMatchFilter);
            if (!string.IsNullOrWhiteSpace(userFilter))
            {
                model.FilterDescription = _matchFilterHumanizer.MatchesAndTournaments(model.AppliedMatchFilter) + _matchFilterHumanizer.MatchingFilter(model.AppliedMatchFilter);
                model.Metadata.PageTitle = model.FilterDescription;
            }
            else
            {
                model.Metadata.PageTitle = _matchFilterHumanizer.MatchesAndTournaments(model.AppliedMatchFilter);
            }

            return CurrentTemplate(model);
        }
    }
}