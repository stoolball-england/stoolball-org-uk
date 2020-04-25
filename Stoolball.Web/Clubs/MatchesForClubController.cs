using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Umbraco.Data.Clubs;
using Stoolball.Umbraco.Data.Matches;
using Stoolball.Web.Matches;
using Stoolball.Web.Routing;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Stoolball.Web.Clubs
{
    public class MatchesForClubController : RenderMvcControllerAsync
    {
        private readonly IClubDataSource _clubDataSource;
        private readonly IMatchDataSource _matchDataSource;
        private readonly IDateFormatter _dateFormatter;
        private readonly IEstimatedSeason _estimatedSeason;

        public MatchesForClubController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IClubDataSource clubDataSource,
           IMatchDataSource matchDataSource,
           IDateFormatter dateFormatter,
           IEstimatedSeason estimatedSeason)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _clubDataSource = clubDataSource ?? throw new ArgumentNullException(nameof(clubDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
            _estimatedSeason = estimatedSeason ?? throw new ArgumentNullException(nameof(estimatedSeason));
        }

        [HttpGet]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var club = await _clubDataSource.ReadClubByRoute(Request.Url.AbsolutePath).ConfigureAwait(false);

            if (club == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                var model = new ClubViewModel(contentModel.Content)
                {
                    Club = club,
                    Matches = new MatchListingViewModel
                    {
                        Matches = await _matchDataSource.ReadMatchListings(new MatchQuery
                        {
                            TeamIds = club.Teams.Select(team => team.TeamId.Value).ToList(),
                            FromDate = _estimatedSeason.StartDate
                        }).ConfigureAwait(false),
                        DateFormatter = _dateFormatter
                    },
                };

                model.Metadata.PageTitle = $"Matches for {model.Club.ClubName}";

                return CurrentTemplate(model);
            }
        }
    }
}