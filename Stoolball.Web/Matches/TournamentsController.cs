using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Dates;
using Stoolball.Matches;
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
        private readonly IMatchFilterUrlParser _matchFilterUrlParser;
        private readonly IMatchFilterHumanizer _matchFilterHumanizer;

        public TournamentsController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IMatchListingDataSource matchesDataSource,
           IDateTimeFormatter dateTimeFormatter,
           IMatchFilterUrlParser matchFilterUrlParser,
           IMatchFilterHumanizer matchFilterHumanizer)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _matchesDataSource = matchesDataSource ?? throw new ArgumentNullException(nameof(matchesDataSource));
            _dateTimeFormatter = dateTimeFormatter ?? throw new ArgumentNullException(nameof(dateTimeFormatter));
            _matchFilterUrlParser = matchFilterUrlParser ?? throw new ArgumentNullException(nameof(matchFilterUrlParser));
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
                MatchFilter = _matchFilterUrlParser.ParseUrl(Request.Url),
                DateTimeFormatter = _dateTimeFormatter
            };
            if (Request.QueryString["from"] == null)
            {
                model.MatchFilter.FromDate = DateTimeOffset.UtcNow.Date;
            }
            model.MatchFilter.IncludeMatches = false;
            model.MatchFilter.IncludeTournamentMatches = false;
            model.MatchFilter.IncludeTournaments = true;

            _ = int.TryParse(Request.QueryString["page"], out var pageNumber);
            model.MatchFilter.Paging.PageNumber = pageNumber > 0 ? pageNumber : 1;
            model.MatchFilter.Paging.PageSize = Constants.Defaults.PageSize;
            model.MatchFilter.Paging.PageUrl = Request.Url;
            model.MatchFilter.Paging.Total = await _matchesDataSource.ReadTotalMatches(model.MatchFilter).ConfigureAwait(false);
            model.Matches = await _matchesDataSource.ReadMatchListings(model.MatchFilter, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            model.FilterDescription = _matchFilterHumanizer.MatchesAndTournamentsMatchingFilter(model.MatchFilter);
            model.Metadata.PageTitle = model.FilterDescription;

            return CurrentTemplate(model);
        }
    }
}