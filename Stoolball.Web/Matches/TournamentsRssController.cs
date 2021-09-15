using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Humanizer;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Teams;
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
    public class TournamentsRssController : RenderMvcControllerAsync
    {
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IDateTimeFormatter _dateFormatter;
        private readonly IMatchFilterQueryStringParser _matchFilterQueryStringParser;
        private readonly IMatchFilterHumanizer _matchFilterHumanizer;

        public TournamentsRssController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IMatchListingDataSource matchDataSource,
           IDateTimeFormatter dateFormatter,
           IMatchFilterQueryStringParser matchFilterQueryStringParser,
           IMatchFilterHumanizer matchFilterHumanizer)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
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
                AppliedMatchFilter = _matchFilterQueryStringParser.ParseQueryString(new MatchFilter(), HttpUtility.ParseQueryString(Request.Url.Query)),
                DateTimeFormatter = _dateFormatter
            };

            model.AppliedMatchFilter.IncludeTournaments = true;
            model.AppliedMatchFilter.IncludeTournamentMatches = false;
            model.AppliedMatchFilter.IncludeMatches = false;
            if (!model.AppliedMatchFilter.FromDate.HasValue)
            {
                model.AppliedMatchFilter.FromDate = DateTimeOffset.UtcNow.AddDays(-1);
            }
            if (!model.AppliedMatchFilter.UntilDate.HasValue)
            {
                if (!int.TryParse(Request.QueryString["days"], out var daysAhead))
                {
                    daysAhead = 365;
                }
                model.AppliedMatchFilter.UntilDate = DateTimeOffset.UtcNow.AddDays(daysAhead);
            }

            var playerType = Path.GetFileNameWithoutExtension(Request.RawUrl.ToUpperInvariant());
            switch (playerType)
            {
                case "MIXED":
                    model.AppliedMatchFilter.PlayerTypes.Add(PlayerType.Mixed);
                    break;
                case "LADIES":
                    model.AppliedMatchFilter.PlayerTypes.Add(PlayerType.Ladies);
                    break;
                case "JUNIOR":
                    model.AppliedMatchFilter.PlayerTypes.Add(PlayerType.JuniorMixed);
                    model.AppliedMatchFilter.PlayerTypes.Add(PlayerType.JuniorGirls);
                    model.AppliedMatchFilter.PlayerTypes.Add(PlayerType.JuniorBoys);
                    break;
                default:
                    playerType = null;
                    break;
            }

            // Remove date from filter and describe the remainder in the feed title, because the date range is not the subject of the feed,
            // it's just what we're including in the feed right now to return only currently relevant data.
            var clonedFilter = model.AppliedMatchFilter.Clone();
            clonedFilter.FromDate = clonedFilter.UntilDate = null;
            model.Metadata.PageTitle = "Stoolball tournaments" + _matchFilterHumanizer.MatchingFilter(clonedFilter);
            model.Metadata.Description = $"New or updated stoolball tournaments on the Stoolball England website";
            if (!string.IsNullOrEmpty(playerType))
            {
                model.Metadata.PageTitle = $"{playerType.ToLower(CultureInfo.CurrentCulture).Humanize(LetterCasing.Sentence)} {model.Metadata.PageTitle.ToLower(CultureInfo.CurrentCulture)}";
                model.Metadata.Description = $"New or updated {playerType.Humanize(LetterCasing.LowerCase)} stoolball tournaments on the Stoolball England website";
            }
            model.Matches = await _matchDataSource.ReadMatchListings(model.AppliedMatchFilter, MatchSortOrder.LatestUpdateFirst).ConfigureAwait(false);

            return View(Request.QueryString["format"] == "tweet" ? "TournamentTweets" : "TournamentsRss", model);
        }
    }
}