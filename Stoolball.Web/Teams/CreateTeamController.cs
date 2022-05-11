using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Stoolball.Web.Teams.Models;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Teams
{
    public class CreateTeamController : RenderController, IRenderControllerAsync
    {
        private readonly IAuthorizationPolicy<Team> _authorizationPolicy;

        public CreateTeamController(ILogger<CreateTeamController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IAuthorizationPolicy<Team> authorizationPolicy)
           : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
        }

        [HttpGet]
        [ContentSecurityPolicy(TinyMCE = true, Forms = true)]
        public async new Task<IActionResult> Index()
        {
            var model = new TeamViewModel(CurrentPage)
            {
                Team = new Team
                {
                    TeamType = TeamType.Regular,
                    PlayerType = PlayerType.Mixed,
                    AgeRangeLower = 11
                }
            };

            model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(model.Team);

            model.Metadata.PageTitle = "Add a team";

            model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Teams, Url = new Uri(Constants.Pages.TeamsUrl, UriKind.Relative) });

            return CurrentTemplate(model);
        }
    }
}