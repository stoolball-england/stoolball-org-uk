using System;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Stoolball.Web.Matches
{
    public class TournamentsController : RenderMvcControllerAsync
    {
        private readonly IMatchListingDataSource _matchesDataSource;
        private readonly IDateTimeFormatter _dateTimeFormatter;
        private readonly IMatchFilterQueryStringParser _matchFilterQueryStringParser;
        private readonly IMatchFilterHumanizer _matchFilterHumanizer;

        public TournamentsController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IMatchListingDataSource matchesDataSource,
           IDateTimeFormatter dateTimeFormatter,
           IMatchFilterQueryStringParser matchFilterQueryStringParser,
           IMatchFilterHumanizer matchFilterHumanizer)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _matchesDataSource = matchesDataSource ?? throw new ArgumentNullException(nameof(matchesDataSource));
            _dateTimeFormatter = dateTimeFormatter ?? throw new ArgumentNullException(nameof(dateTimeFormatter));
            _matchFilterQueryStringParser = matchFilterQueryStringParser ?? throw new ArgumentNullException(nameof(matchFilterQueryStringParser));
            _matchFilterHumanizer = matchFilterHumanizer ?? throw new ArgumentNullException(nameof(matchFilterHumanizer));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var model = new MatchListingViewModel(contentModel.Content, Services?.UserService)
            {
                DefaultMatchFilter = new MatchFilter
                {
                    FromDate = DateTimeOffset.UtcNow.Date,
                    IncludeMatches = false,
                    IncludeTournamentMatches = false,
                    IncludeTournaments = true,
                    Paging = new Paging
                    {
                        PageNumber = int.TryParse(Request.QueryString["page"], out var pageNumber) ? pageNumber > 0 ? pageNumber : 1 : 1,
                        PageSize = Constants.Defaults.PageSize,
                        PageUrl = Request.Url
                    }
                },
                DateTimeFormatter = _dateTimeFormatter
            };
            model.AppliedMatchFilter = _matchFilterQueryStringParser.ParseQueryString(model.DefaultMatchFilter, HttpUtility.ParseQueryString(Request.Url.Query));
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