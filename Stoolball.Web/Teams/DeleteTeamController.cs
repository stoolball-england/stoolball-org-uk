using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Umbraco.Data.Matches;
using Stoolball.Umbraco.Data.Teams;
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
    public class DeleteTeamController : RenderMvcControllerAsync
    {
        private readonly ITeamDataSource _teamDataSource;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IPlayerDataSource _playerDataSource;
        private readonly IAuthorizationPolicy<Team> _authorizationPolicy;

        public DeleteTeamController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ITeamDataSource teamDataSource,
           IMatchListingDataSource matchDataSource,
           IPlayerDataSource playerDataSource,
           IAuthorizationPolicy<Team> authorizationPolicy)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _playerDataSource = playerDataSource ?? throw new ArgumentNullException(nameof(playerDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true)]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var model = new DeleteTeamViewModel(contentModel.Content, Services?.UserService)
            {
                Team = await _teamDataSource.ReadTeamByRoute(Request.RawUrl, true).ConfigureAwait(false)
            };

            if (model.Team == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                var teamIds = new List<Guid> { model.Team.TeamId.Value };
                model.TotalMatches = await _matchDataSource.ReadTotalMatches(new MatchQuery
                {
                    TeamIds = teamIds,
                    IncludeTournamentMatches = true
                }).ConfigureAwait(false);
                model.Team.Players = (await _playerDataSource.ReadPlayerIdentities(new PlayerIdentityQuery
                {
                    TeamIds = teamIds,
                    PlayerRoles = new List<PlayerRole> { PlayerRole.Player }
                }).ConfigureAwait(false))?.Select(x => new Player { PlayerIdentities = new List<PlayerIdentity> { x } }).ToList();

                model.ConfirmDeleteRequest.RequiredText = model.Team.TeamName;

                model.IsAuthorized = _authorizationPolicy.IsAuthorized(model.Team);

                model.Metadata.PageTitle = "Delete " + model.Team.TeamName;

                return CurrentTemplate(model);
            }
        }
    }
}