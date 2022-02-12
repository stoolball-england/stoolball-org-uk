using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Clubs;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Clubs
{
    public class MatchesForClubController : RenderController, IRenderControllerAsync
    {
        private readonly IClubDataSource _clubDataSource;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IMatchFilterFactory _matchFilterFactory;
        private readonly IAuthorizationPolicy<Club> _authorizationPolicy;
        private readonly IMatchFilterQueryStringParser _matchFilterQueryStringParser;
        private readonly IMatchFilterHumanizer _matchFilterHumanizer;

        public MatchesForClubController(ILogger<MatchesForClubController> logger,
           ICompositeViewEngine compositeViewEngine,
           IUmbracoContextAccessor umbracoContextAccessor,
           IClubDataSource clubDataSource,
           IMatchListingDataSource matchDataSource,
           IDateTimeFormatter dateFormatter,
           IMatchFilterFactory matchFilterFactory,
           IAuthorizationPolicy<Club> authorizationPolicy,
           IMatchFilterQueryStringParser matchFilterUrlParser,
           IMatchFilterHumanizer matchFilterHumanizer)
           : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _clubDataSource = clubDataSource ?? throw new ArgumentNullException(nameof(clubDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _matchFilterFactory = matchFilterFactory ?? throw new ArgumentNullException(nameof(matchFilterFactory));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _matchFilterQueryStringParser = matchFilterUrlParser ?? throw new ArgumentNullException(nameof(matchFilterUrlParser));
            _matchFilterHumanizer = matchFilterHumanizer ?? throw new ArgumentNullException(nameof(matchFilterHumanizer));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var club = await _clubDataSource.ReadClubByRoute(Request.Path).ConfigureAwait(false);

            if (club == null)
            {
                return NotFound();
            }
            else
            {
                var filter = _matchFilterFactory.MatchesForTeams(club.Teams.Select(team => team.TeamId!.Value).ToList());
                var model = new ClubViewModel(CurrentPage)
                {
                    Club = club,
                    DefaultMatchFilter = filter.filter,
                    Matches = new MatchListingViewModel(CurrentPage)
                };
                model.AppliedMatchFilter = _matchFilterQueryStringParser.ParseQueryString(model.DefaultMatchFilter, Request.QueryString.ToString());

                // Only get matches if there are teams, otherwise matches for all teams will be returned
                if (model.Club.Teams.Count > 0)
                {
                    model.Matches.Matches = await _matchDataSource.ReadMatchListings(model.AppliedMatchFilter, filter.sortOrder).ConfigureAwait(false);
                }

                model.IsAuthorized = await _authorizationPolicy.IsAuthorized(model.Club);

                var userFilter = _matchFilterHumanizer.MatchingFilter(model.AppliedMatchFilter);
                if (!string.IsNullOrWhiteSpace(userFilter))
                {
                    model.FilterDescription = _matchFilterHumanizer.MatchesAndTournaments(model.AppliedMatchFilter) + userFilter;
                }
                model.Metadata.PageTitle = $"{_matchFilterHumanizer.MatchesAndTournaments(model.AppliedMatchFilter)} for {model.Club.ClubName}{userFilter}";

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Teams, Url = new Uri(Constants.Pages.TeamsUrl, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}