using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Clubs;
using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.Matches;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
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
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IDateTimeFormatter _dateFormatter;
        private readonly ISeasonEstimator _seasonEstimator;
        private readonly IAuthorizationPolicy<Club> _authorizationPolicy;

        public MatchesForClubController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IClubDataSource clubDataSource,
           IMatchListingDataSource matchDataSource,
           IDateTimeFormatter dateFormatter,
           ISeasonEstimator seasonEstimator,
           IAuthorizationPolicy<Club> authorizationPolicy)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _clubDataSource = clubDataSource ?? throw new ArgumentNullException(nameof(clubDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
            _seasonEstimator = seasonEstimator ?? throw new ArgumentNullException(nameof(seasonEstimator));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var club = await _clubDataSource.ReadClubByRoute(Request.RawUrl).ConfigureAwait(false);

            if (club == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                var model = new ClubViewModel(contentModel.Content, Services?.UserService)
                {
                    Club = club,
                    Matches = new MatchListingViewModel
                    {
                        Matches = await _matchDataSource.ReadMatchListings(new MatchQuery
                        {
                            TeamIds = club.Teams.Select(team => team.TeamId.Value).ToList(),
                            FromDate = _seasonEstimator.EstimateSeasonDates(DateTimeOffset.UtcNow).fromDate
                        }).ConfigureAwait(false),
                        DateTimeFormatter = _dateFormatter
                    },
                };

                model.IsAuthorized = _authorizationPolicy.IsAuthorized(model.Club);

                model.Metadata.PageTitle = $"Matches for {model.Club.ClubName}";

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Teams, Url = new Uri(Constants.Pages.TeamsUrl, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}