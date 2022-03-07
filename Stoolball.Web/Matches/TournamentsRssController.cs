using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Matches;
using Stoolball.Teams;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Matches
{
    public class TournamentsRssController : RenderController, IRenderControllerAsync
    {
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IMatchFilterQueryStringParser _matchFilterQueryStringParser;
        private readonly IMatchFilterHumanizer _matchFilterHumanizer;

        public TournamentsRssController(ILogger<TournamentsRssController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IMatchListingDataSource matchDataSource,
            IMatchFilterQueryStringParser matchFilterQueryStringParser,
            IMatchFilterHumanizer matchFilterHumanizer)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _matchFilterQueryStringParser = matchFilterQueryStringParser ?? throw new ArgumentNullException(nameof(matchFilterQueryStringParser));
            _matchFilterHumanizer = matchFilterHumanizer ?? throw new ArgumentNullException(nameof(matchFilterHumanizer));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var model = new MatchListingViewModel(CurrentPage)
            {
                AppliedMatchFilter = _matchFilterQueryStringParser.ParseQueryString(new MatchFilter(), Request.QueryString.Value)
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
                if (!(Request.Query.ContainsKey("days") && int.TryParse(Request.Query["days"], out var daysAhead)))
                {
                    daysAhead = 365;
                }
                model.AppliedMatchFilter.UntilDate = DateTimeOffset.UtcNow.AddDays(daysAhead);
            }

            var playerType = Path.GetFileNameWithoutExtension(Request.Path.Value?.ToUpperInvariant());
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
            model.Matches = await _matchDataSource.ReadMatchListings(model.AppliedMatchFilter, MatchSortOrder.LatestUpdateFirst);

            return View((Request.Query.ContainsKey("format") && Request.Query["format"] == "tweet") ? "TournamentTweets" : "TournamentsRss", model);
        }
    }
}