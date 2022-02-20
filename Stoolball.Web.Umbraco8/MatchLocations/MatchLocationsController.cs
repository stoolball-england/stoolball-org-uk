using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Listings;
using Stoolball.MatchLocations;
using Stoolball.Teams;
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
    public class MatchLocationsController : RenderMvcControllerAsync
    {
        private readonly IMatchLocationDataSource _matchLocationDataSource;
        private readonly IListingsModelBuilder<MatchLocation, MatchLocationFilter, MatchLocationsViewModel> _listingsModelBuilder;

        public MatchLocationsController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IMatchLocationDataSource matchLocationDataSource,
           IListingsModelBuilder<MatchLocation, MatchLocationFilter, MatchLocationsViewModel> listingsModelBuilder)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _matchLocationDataSource = matchLocationDataSource ?? throw new System.ArgumentNullException(nameof(matchLocationDataSource));
            _listingsModelBuilder = listingsModelBuilder ?? throw new System.ArgumentNullException(nameof(listingsModelBuilder));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var model = await _listingsModelBuilder.BuildModel(() => new MatchLocationsViewModel(contentModel.Content, Services?.UserService)
            {
                Filter = new MatchLocationFilter
                {
                    TeamTypes = new List<TeamType> { TeamType.LimitedMembership, TeamType.Occasional, TeamType.Regular, TeamType.Representative, TeamType.SchoolAgeGroup, TeamType.SchoolAgeGroup, TeamType.SchoolClub, TeamType.SchoolOther }
                }
            },
            _matchLocationDataSource.ReadTotalMatchLocations,
            _matchLocationDataSource.ReadMatchLocations,
            Constants.Pages.MatchLocations,
            Request.Url,
            Request.Url.Query
            ).ConfigureAwait(false);

            return CurrentTemplate(model);
        }
    }
}