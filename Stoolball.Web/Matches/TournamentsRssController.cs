using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
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

        public TournamentsRssController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IMatchListingDataSource matchDataSource,
           IDateTimeFormatter dateFormatter)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _matchDataSource = matchDataSource ?? throw new System.ArgumentNullException(nameof(matchDataSource));
            _dateFormatter = dateFormatter ?? throw new System.ArgumentNullException(nameof(dateFormatter));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            if (!int.TryParse(Request.QueryString["days"], out var daysAhead))
            {
                daysAhead = 365;
            }

            var filter = new MatchFilter
            {
                IncludeTournaments = true,
                IncludeTournamentMatches = false,
                IncludeMatches = false,
                FromDate = DateTimeOffset.UtcNow.AddDays(-1),
                UntilDate = DateTimeOffset.UtcNow.AddDays(daysAhead)
            };

            var playerType = Path.GetFileNameWithoutExtension(Request.RawUrl.ToUpperInvariant());
            switch (playerType)
            {
                case "MIXED":
                    filter.PlayerTypes.Add(PlayerType.Mixed);
                    break;
                case "LADIES":
                    filter.PlayerTypes.Add(PlayerType.Ladies);
                    break;
                case "JUNIOR":
                    filter.PlayerTypes.Add(PlayerType.JuniorMixed);
                    filter.PlayerTypes.Add(PlayerType.JuniorGirls);
                    filter.PlayerTypes.Add(PlayerType.JuniorBoys);
                    break;
                default:
                    playerType = null;
                    break;
            }

            var model = new MatchListingViewModel(contentModel.Content, Services?.UserService)
            {
                Matches = await _matchDataSource.ReadMatchListings(filter, MatchSortOrder.LatestUpdateFirst).ConfigureAwait(false),
                DateTimeFormatter = _dateFormatter
            };

            model.Metadata.PageTitle = "Stoolball tournaments";
            model.Metadata.Description = $"New or updated stoolball tournaments on the Stoolball England website";
            if (!string.IsNullOrEmpty(playerType))
            {
                model.Metadata.PageTitle = $"{playerType.ToLower(CultureInfo.CurrentCulture).Humanize(LetterCasing.Sentence)} {model.Metadata.PageTitle.ToLower(CultureInfo.CurrentCulture)}";
                model.Metadata.Description = $"New or updated {playerType.ToLower(CultureInfo.CurrentCulture).Humanize(LetterCasing.LowerCase)} stoolball tournaments on the Stoolball England website";
            }

            return CurrentTemplate(model);
        }
    }
}