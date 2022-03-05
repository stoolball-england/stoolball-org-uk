using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Listings;
using Stoolball.MatchLocations;
using Stoolball.Teams;
using Stoolball.Web.MatchLocations.Models;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.MatchLocations
{
    public class MatchLocationsController : RenderController, IRenderControllerAsync
    {
        private readonly IMatchLocationDataSource _matchLocationDataSource;
        private readonly IListingsModelBuilder<MatchLocation, MatchLocationFilter, MatchLocationsViewModel> _listingsModelBuilder;

        public MatchLocationsController(ILogger<MatchLocationsController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IMatchLocationDataSource matchLocationDataSource,
            IListingsModelBuilder<MatchLocation, MatchLocationFilter, MatchLocationsViewModel> listingsModelBuilder)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _matchLocationDataSource = matchLocationDataSource ?? throw new ArgumentNullException(nameof(matchLocationDataSource));
            _listingsModelBuilder = listingsModelBuilder ?? throw new ArgumentNullException(nameof(listingsModelBuilder));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var model = await _listingsModelBuilder.BuildModel(() => new MatchLocationsViewModel(CurrentPage)
            {
                Filter = new MatchLocationFilter
                {
                    TeamTypes = new List<TeamType> { TeamType.LimitedMembership, TeamType.Occasional, TeamType.Regular, TeamType.Representative, TeamType.SchoolAgeGroup, TeamType.SchoolAgeGroup, TeamType.SchoolClub, TeamType.SchoolOther }
                }
            },
            _matchLocationDataSource.ReadTotalMatchLocations,
            _matchLocationDataSource.ReadMatchLocations,
            Constants.Pages.MatchLocations,
            new Uri(Request.GetEncodedUrl()),
            Request.QueryString.Value
            );

            return CurrentTemplate(model);
        }
    }
}