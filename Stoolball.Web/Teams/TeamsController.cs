using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
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

        public TeamsController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ITeamListingDataSource teamDataSource)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _teamDataSource = teamDataSource ?? throw new System.ArgumentNullException(nameof(teamDataSource));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            _ = int.TryParse(Request.QueryString["page"], out var pageNumber);
            var model = new TeamsViewModel(contentModel.Content, Services?.UserService)
            {
                TeamQuery = new TeamQuery
                {
                    Query = Request.QueryString["q"]?.Trim(),
                    IncludeClubTeams = false,
                    TeamTypes = new List<TeamType> { TeamType.LimitedMembership, TeamType.Occasional, TeamType.Regular, TeamType.Representative, TeamType.SchoolClub },
                    PageNumber = pageNumber > 0 ? pageNumber : 1
                }
            };

            model.TotalTeams = await _teamDataSource.ReadTotalTeams(model.TeamQuery).ConfigureAwait(false);
            model.Teams = await _teamDataSource.ReadTeamListings(model.TeamQuery).ConfigureAwait(false);

            model.Metadata.PageTitle = Constants.Pages.Teams;
            if (!string.IsNullOrEmpty(model.TeamQuery.Query))
            {
                model.Metadata.PageTitle += $" matching '{model.TeamQuery.Query}'";
            }

            return CurrentTemplate(model);
        }
    }
}