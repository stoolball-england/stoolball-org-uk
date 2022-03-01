using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Email;
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
    public class TeamController : RenderController, IRenderControllerAsync
    {
        private readonly ITeamDataSource _teamDataSource;
        private readonly IAuthorizationPolicy<Team> _authorizationPolicy;
        private readonly IEmailProtector _emailProtector;

        public TeamController(ILogger<TeamController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            ITeamDataSource teamDataSource,
            IAuthorizationPolicy<Team> authorizationPolicy,
            IEmailProtector emailProtector)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _teamDataSource = teamDataSource ?? throw new System.ArgumentNullException(nameof(teamDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new System.ArgumentNullException(nameof(authorizationPolicy));
            _emailProtector = emailProtector ?? throw new System.ArgumentNullException(nameof(emailProtector));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var model = new TeamViewModel(CurrentPage)
            {
                Team = await _teamDataSource.ReadTeamByRoute(Request.Path, true)
            };

            if (model.Team == null)
            {
                return NotFound();
            }
            else
            {
                model.IsAuthorized = await _authorizationPolicy.IsAuthorized(model.Team);

                model.Metadata.PageTitle = model.Team.TeamName + " stoolball team";
                model.Metadata.Description = model.Team.Description();

                model.Team.Cost = _emailProtector.ProtectEmailAddresses(model.Team.Cost, User.Identity?.IsAuthenticated ?? false);
                model.Team.Introduction = _emailProtector.ProtectEmailAddresses(model.Team.Introduction, User.Identity?.IsAuthenticated ?? false);
                model.Team.PlayingTimes = _emailProtector.ProtectEmailAddresses(model.Team.PlayingTimes, User.Identity?.IsAuthenticated ?? false);
                model.Team.PublicContactDetails = _emailProtector.ProtectEmailAddresses(model.Team.PublicContactDetails, User.Identity?.IsAuthenticated ?? false);

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Teams, Url = new Uri(Constants.Pages.TeamsUrl, UriKind.Relative) });
                if (model.Team.Club != null)
                {
                    model.Breadcrumbs.Add(new Breadcrumb { Name = model.Team.Club.ClubName, Url = new Uri(model.Team.Club.ClubRoute, UriKind.Relative) });
                }

                return CurrentTemplate(model);
            }
        }
    }
}