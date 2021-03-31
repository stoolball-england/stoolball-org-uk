using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.MatchLocations;
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

namespace Stoolball.Web.MatchLocations
{
    public class MatchesForMatchLocationController : RenderMvcControllerAsync
    {
        private readonly IMatchFilterFactory _matchFilterFactory;
        private readonly IMatchLocationDataSource _matchLocationDataSource;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IAuthorizationPolicy<MatchLocation> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateFormatter;

        public MatchesForMatchLocationController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IMatchFilterFactory matchFilterFactory,
           IMatchLocationDataSource matchLocationDataSource,
           IMatchListingDataSource matchDataSource,
           IAuthorizationPolicy<MatchLocation> authorizationPolicy,
           IDateTimeFormatter dateFormatter)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _matchFilterFactory = matchFilterFactory ?? throw new ArgumentNullException(nameof(matchFilterFactory));
            _matchLocationDataSource = matchLocationDataSource ?? throw new ArgumentNullException(nameof(matchLocationDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var location = await _matchLocationDataSource.ReadMatchLocationByRoute(Request.RawUrl, false).ConfigureAwait(false);

            if (location == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                var filter = _matchFilterFactory.MatchesForMatchLocation(location.MatchLocationId.Value);
                var model = new MatchLocationViewModel(contentModel.Content, Services?.UserService)
                {
                    MatchLocation = location,
                    Matches = new MatchListingViewModel(contentModel.Content, Services?.UserService)
                    {
                        Matches = await _matchDataSource.ReadMatchListings(filter.filter, filter.sortOrder).ConfigureAwait(false),
                        DateTimeFormatter = _dateFormatter
                    },
                };

                model.IsAuthorized = _authorizationPolicy.IsAuthorized(model.MatchLocation);

                model.Metadata.PageTitle = $"Matches for {model.MatchLocation.NameAndLocalityOrTownIfDifferent()}";

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.MatchLocations, Url = new Uri(Constants.Pages.MatchLocationsUrl, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}