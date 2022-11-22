using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Data.Abstractions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Matches
{
    public class MatchesController : RenderController, IRenderControllerAsync
    {
        private readonly IMatchListingDataSource _matchesDataSource;
        private readonly IDateTimeFormatter _dateTimeFormatter;
        private readonly IMatchFilterQueryStringParser _matchFilterQueryStringParser;
        private readonly IMatchFilterHumanizer _matchFilterHumanizer;

        public MatchesController(ILogger<MatchesController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IMatchListingDataSource matchesDataSource,
            IDateTimeFormatter dateTimeFormatter,
            IMatchFilterQueryStringParser matchFilterQueryStringParser,
            IMatchFilterHumanizer matchFilterHumanizer)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _matchesDataSource = matchesDataSource ?? throw new ArgumentNullException(nameof(matchesDataSource));
            _dateTimeFormatter = dateTimeFormatter ?? throw new ArgumentNullException(nameof(dateTimeFormatter));
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
                    IncludeMatches = true,
                    IncludeTournamentMatches = false,
                    IncludeTournaments = false,
                    Paging = new Paging
                    {
                        PageNumber = int.TryParse(Request.Query["page"], out var pageNumber) ? pageNumber > 0 ? pageNumber : 1 : 1,
                        PageSize = Constants.Defaults.PageSize,
                        PageUrl = new Uri(Request.GetEncodedUrl())
                    }
                }
            };
            model.AppliedMatchFilter = _matchFilterQueryStringParser.ParseQueryString(model.DefaultMatchFilter, Request.QueryString.Value);
            model.AppliedMatchFilter.Paging.Total = await _matchesDataSource.ReadTotalMatches(model.AppliedMatchFilter).ConfigureAwait(false);
            model.Matches = await _matchesDataSource.ReadMatchListings(model.AppliedMatchFilter, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

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