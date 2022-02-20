using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Listings;
using Stoolball.Teams;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Stoolball.Web.Teams
{
    public class TeamsController : RenderMvcControllerAsync
    {
        private readonly ITeamListingDataSource _teamDataSource;
        private readonly IListingsModelBuilder<TeamListing, TeamListingFilter, TeamsViewModel> _listingsModelBuilder;

        public TeamsController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ITeamListingDataSource teamDataSource,
           IListingsModelBuilder<TeamListing, TeamListingFilter, TeamsViewModel> listingsModelBuilder)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _teamDataSource = teamDataSource ?? throw new System.ArgumentNullException(nameof(teamDataSource));
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

            var model = await _listingsModelBuilder.BuildModel(() => new TeamsViewModel(contentModel.Content, Services?.UserService)
            {
                Filter = new TeamListingFilter
                {
                    TeamTypes = new List<TeamType?> { TeamType.LimitedMembership, TeamType.Occasional, TeamType.Regular, TeamType.Representative, TeamType.SchoolClub, null }
                }
            },
            _teamDataSource.ReadTotalTeams,
            _teamDataSource.ReadTeamListings,
            Constants.Pages.Teams,
            Request.Url,
            Request.Url.Query).ConfigureAwait(false);

            return CurrentTemplate(model);
        }
    }
}