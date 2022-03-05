using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.MatchLocations.Models;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.MatchLocations
{
    public class MatchesForMatchLocationController : RenderController, IRenderControllerAsync
    {
        private readonly IMatchFilterFactory _matchFilterFactory;
        private readonly IMatchLocationDataSource _matchLocationDataSource;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IAuthorizationPolicy<MatchLocation> _authorizationPolicy;
        private readonly IMatchFilterQueryStringParser _matchFilterQueryStringParser;
        private readonly IMatchFilterHumanizer _matchFilterHumanizer;

        public MatchesForMatchLocationController(ILogger<MatchesForMatchLocationController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IMatchFilterFactory matchFilterFactory,
            IMatchLocationDataSource matchLocationDataSource,
            IMatchListingDataSource matchDataSource,
            IAuthorizationPolicy<MatchLocation> authorizationPolicy,
            IMatchFilterQueryStringParser matchFilterQueryStringParser,
            IMatchFilterHumanizer matchFilterHumanizer)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _matchFilterFactory = matchFilterFactory ?? throw new ArgumentNullException(nameof(matchFilterFactory));
            _matchLocationDataSource = matchLocationDataSource ?? throw new ArgumentNullException(nameof(matchLocationDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _matchFilterQueryStringParser = matchFilterQueryStringParser ?? throw new ArgumentNullException(nameof(matchFilterQueryStringParser));
            _matchFilterHumanizer = matchFilterHumanizer ?? throw new ArgumentNullException(nameof(matchFilterHumanizer));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var location = await _matchLocationDataSource.ReadMatchLocationByRoute(Request.Path, false);

            if (location == null)
            {
                return NotFound();
            }
            else
            {
                var filter = _matchFilterFactory.MatchesForMatchLocation(location.MatchLocationId!.Value);
                var model = new MatchLocationViewModel(CurrentPage)
                {
                    MatchLocation = location,
                    DefaultMatchFilter = filter.filter,
                    Matches = new MatchListingViewModel(CurrentPage),
                };
                model.AppliedMatchFilter = _matchFilterQueryStringParser.ParseQueryString(model.DefaultMatchFilter, Request.QueryString.Value);
                model.Matches.Matches = await _matchDataSource.ReadMatchListings(model.AppliedMatchFilter, filter.sortOrder).ConfigureAwait(false);

                model.IsAuthorized = await _authorizationPolicy.IsAuthorized(model.MatchLocation);

                var userFilter = _matchFilterHumanizer.MatchingFilter(model.AppliedMatchFilter);
                if (!string.IsNullOrWhiteSpace(userFilter))
                {
                    model.FilterDescription = _matchFilterHumanizer.MatchesAndTournaments(model.AppliedMatchFilter) + userFilter;
                }
                model.Metadata.PageTitle = $"{_matchFilterHumanizer.MatchesAndTournaments(model.AppliedMatchFilter)} at {model.MatchLocation.NameAndLocalityOrTownIfDifferent()}{userFilter}";

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.MatchLocations, Url = new Uri(Constants.Pages.MatchLocationsUrl, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}