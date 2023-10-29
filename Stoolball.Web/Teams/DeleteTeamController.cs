using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
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
    public class DeleteTeamController : RenderController, IRenderControllerAsync
    {
        private readonly ITeamDataSource _teamDataSource;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IPlayerDataSource _playerDataSource;
        private readonly IAuthorizationPolicy<Team> _authorizationPolicy;
        private readonly ITeamBreadcrumbBuilder _breadcrumbBuilder;

        public DeleteTeamController(ILogger<DeleteTeamController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            ITeamDataSource teamDataSource,
            IMatchListingDataSource matchDataSource,
            IPlayerDataSource playerDataSource,
            IAuthorizationPolicy<Team> authorizationPolicy,
            ITeamBreadcrumbBuilder breadcrumbBuilder)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _playerDataSource = playerDataSource ?? throw new ArgumentNullException(nameof(playerDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _breadcrumbBuilder = breadcrumbBuilder ?? throw new ArgumentNullException(nameof(breadcrumbBuilder));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true)]
        public async new Task<IActionResult> Index()
        {
            var model = new DeleteTeamViewModel(CurrentPage)
            {
                Team = await _teamDataSource.ReadTeamByRoute(Request.Path, true).ConfigureAwait(false)
            };

            if (model.Team == null)
            {
                return NotFound();
            }
            else
            {
                var teamIds = new List<Guid> { model.Team.TeamId!.Value };
                model.TotalMatches = await _matchDataSource.ReadTotalMatches(new MatchFilter
                {
                    TeamIds = teamIds,
                    IncludeTournamentMatches = true
                }).ConfigureAwait(false);
                model.Team.Players = (await _playerDataSource.ReadPlayerIdentities(new PlayerFilter
                {
                    TeamIds = teamIds
                }).ConfigureAwait(false)).Select(x => new Player { PlayerIdentities = new List<PlayerIdentity> { x } }).ToList();

                model.ConfirmDeleteRequest.RequiredText = model.Team.TeamName;

                model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(model.Team);

                model.Metadata.PageTitle = "Delete " + model.Team.TeamName;

                _breadcrumbBuilder.BuildBreadcrumbsForTeam(model.Breadcrumbs, model.Team, true);

                return CurrentTemplate(model);
            }
        }
    }
}