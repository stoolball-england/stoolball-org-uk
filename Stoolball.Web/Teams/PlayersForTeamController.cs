using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Statistics;
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
    public class PlayersForTeamController : RenderMvcControllerAsync
    {
        private readonly ITeamDataSource _teamDataSource;
        private readonly IAuthorizationPolicy<Team> _authorizationPolicy;
        private readonly IPlayerDataSource _playerDataSource;

        public PlayersForTeamController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ITeamDataSource teamDataSource,
           IAuthorizationPolicy<Team> authorizationPolicy,
           IPlayerDataSource playerDataSource)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _playerDataSource = playerDataSource ?? throw new ArgumentNullException(nameof(playerDataSource));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var model = new TeamViewModel(contentModel.Content, Services?.UserService)
            {
                Team = await _teamDataSource.ReadTeamByRoute(Request.Url.AbsolutePath, true).ConfigureAwait(false)
            };

            if (model.Team == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                var identities = await _playerDataSource.ReadPlayerIdentities(new PlayerIdentityFilter { TeamIds = new List<Guid> { model.Team.TeamId.Value } }).ConfigureAwait(false);
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