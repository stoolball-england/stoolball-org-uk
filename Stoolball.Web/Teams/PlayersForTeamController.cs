using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Data.Abstractions;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Web.Navigation;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Stoolball.Web.Teams.Models;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Teams
{
    public class PlayersForTeamController : RenderController, IRenderControllerAsync
    {
        private readonly ITeamDataSource _teamDataSource;
        private readonly IAuthorizationPolicy<Team> _authorizationPolicy;
        private readonly IPlayerDataSource _playerDataSource;
        private readonly ITeamBreadcrumbBuilder _breadcrumbBuilder;

        public PlayersForTeamController(ILogger<PlayersForTeamController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            ITeamDataSource teamDataSource,
            IAuthorizationPolicy<Team> authorizationPolicy,
            IPlayerDataSource playerDataSource,
            ITeamBreadcrumbBuilder breadcrumbBuilder)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _playerDataSource = playerDataSource ?? throw new ArgumentNullException(nameof(playerDataSource));
            _breadcrumbBuilder = breadcrumbBuilder ?? throw new ArgumentNullException(nameof(breadcrumbBuilder));
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
                model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(model.Team);

                model.PlayerIdentities = await _playerDataSource.ReadPlayerIdentities(new PlayerFilter { TeamIds = new List<Guid> { model.Team.TeamId!.Value } });
                model.Players = model.PlayerIdentities.Select(x => x.Player).OfType<Player>().Distinct(new PlayerEqualityComparer()).ToList();

                model.Metadata.PageTitle = "Players for " + model.Team.TeamName + " stoolball team";
                model.Metadata.Description = model.Team.Description();

                _breadcrumbBuilder.BuildBreadcrumbsForTeam(model.Breadcrumbs, model.Team, false);

                return CurrentTemplate(model);
            }
        }
    }
}