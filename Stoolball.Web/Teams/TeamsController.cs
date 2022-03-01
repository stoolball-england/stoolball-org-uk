using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Listings;
using Stoolball.Teams;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Stoolball.Web.Teams.Models;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Teams
{
    public class TeamsController : RenderController, IRenderControllerAsync
    {
        private readonly ITeamListingDataSource _teamDataSource;
        private readonly IListingsModelBuilder<TeamListing, TeamListingFilter, TeamsViewModel> _listingsModelBuilder;

        public TeamsController(ILogger<TeamsController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            ITeamListingDataSource teamDataSource,
            IListingsModelBuilder<TeamListing, TeamListingFilter, TeamsViewModel> listingsModelBuilder)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _listingsModelBuilder = listingsModelBuilder ?? throw new ArgumentNullException(nameof(listingsModelBuilder));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var model = await _listingsModelBuilder.BuildModel(() => new TeamsViewModel(CurrentPage)
            {
                Filter = new TeamListingFilter
                {
                    TeamTypes = new List<TeamType?> { TeamType.LimitedMembership, TeamType.Occasional, TeamType.Regular, TeamType.Representative, TeamType.SchoolClub, null }
                }
            },
            _teamDataSource.ReadTotalTeams,
            _teamDataSource.ReadTeamListings,
            Constants.Pages.Teams,
            new Uri(Request.GetEncodedUrl()),
            Request.QueryString.Value);

            return CurrentTemplate(model);
        }
    }
}