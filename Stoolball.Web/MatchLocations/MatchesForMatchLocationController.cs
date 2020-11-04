using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.MatchLocations;
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
        private readonly IMatchLocationDataSource _matchLocationDataSource;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IAuthorizationPolicy<MatchLocation> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateFormatter;
        private readonly ISeasonEstimator _seasonEstimator;

        public MatchesForMatchLocationController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IMatchLocationDataSource matchLocationDataSource,
           IMatchListingDataSource matchDataSource,
           IAuthorizationPolicy<MatchLocation> authorizationPolicy,
           IDateTimeFormatter dateFormatter,
           ISeasonEstimator seasonEstimator)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _matchLocationDataSource = matchLocationDataSource ?? throw new ArgumentNullException(nameof(matchLocationDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
            _seasonEstimator = seasonEstimator ?? throw new ArgumentNullException(nameof(seasonEstimator));
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
                var model = new MatchLocationViewModel(contentModel.Content, Services?.UserService)
                {
                    MatchLocation = location,
                    Matches = new MatchListingViewModel
                    {
                        Matches = await _matchDataSource.ReadMatchListings(new MatchQuery
                        {
                            MatchLocationIds = new List<Guid> { location.MatchLocationId.Value },
                            FromDate = _seasonEstimator.EstimateSeasonDates(DateTimeOffset.UtcNow).fromDate
                        }).ConfigureAwait(false),
                        DateTimeFormatter = _dateFormatter
                    },
                };

                model.IsAuthorized = _authorizationPolicy.IsAuthorized(model.MatchLocation);

                model.Metadata.PageTitle = $"Matches for {model.MatchLocation}";

                return CurrentTemplate(model);
            }
        }
    }
}