using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Teams;
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

        public PlayersForTeamController(ILogger<PlayersForTeamController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            ITeamDataSource teamDataSource,
            IAuthorizationPolicy<Team> authorizationPolicy,
            IPlayerDataSource playerDataSource)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _playerDataSource = playerDataSource ?? throw new ArgumentNullException(nameof(playerDataSource));
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
                var identities = await _playerDataSource.ReadPlayerIdentities(new PlayerFilter { TeamIds = new List<Guid> { model.Team.TeamId!.Value } });
                model.Players = identities.Select(x => x.Player).Distinct(new PlayerEqualityComparer()).ToList();
                foreach (var player in model.Players)
                {
                    player.PlayerIdentities = identities.Where(x => x.Player.PlayerId == player.PlayerId).ToList();
                }

                model.Metadata.PageTitle = "Players for " + model.Team.TeamName + " stoolball team";
                model.Metadata.Description = model.Team.Description();

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