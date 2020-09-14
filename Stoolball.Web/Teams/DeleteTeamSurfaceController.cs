using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Umbraco.Data.Matches;
using Stoolball.Umbraco.Data.Teams;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;

namespace Stoolball.Web.Teams
{
    public class DeleteTeamSurfaceController : SurfaceController
    {
        private readonly ITeamDataSource _teamDataSource;
        private readonly ITeamRepository _teamRepository;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IPlayerDataSource _playerDataSource;
        private readonly IAuthorizationPolicy<Team> _authorizationPolicy;

        public DeleteTeamSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, ITeamDataSource teamDataSource, ITeamRepository teamRepository,
            IMatchListingDataSource matchDataSource, IPlayerDataSource playerDataSource, IAuthorizationPolicy<Team> authorizationPolicy)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _teamDataSource = teamDataSource;
            _teamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _playerDataSource = playerDataSource ?? throw new ArgumentNullException(nameof(playerDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<ActionResult> DeleteTeam([Bind(Prefix = "ConfirmDeleteRequest", Include = "RequiredText,ConfirmationText")] MatchingTextConfirmation model)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var viewModel = new DeleteTeamViewModel(CurrentPage, Services.UserService)
            {
                Team = await _teamDataSource.ReadTeamByRoute(Request.RawUrl, true).ConfigureAwait(false),
            };
            viewModel.IsAuthorized = _authorizationPolicy.IsAuthorized(viewModel.Team);

            if (viewModel.IsAuthorized[AuthorizedAction.DeleteTeam] && ModelState.IsValid)
            {
                Services.MemberGroupService.Delete(Services.MemberGroupService.GetById(viewModel.Team.MemberGroupId));

                var currentMember = Members.GetCurrentMember();
                await _teamRepository.DeleteTeam(viewModel.Team, currentMember.Key, currentMember.Name).ConfigureAwait(false);
                viewModel.Deleted = true;
            }
            else
            {
                var teamIds = new List<Guid> { viewModel.Team.TeamId.Value };
                viewModel.TotalMatches = await _matchDataSource.ReadTotalMatches(new MatchQuery
                {
                    TeamIds = teamIds,
                    IncludeTournamentMatches = true
                }).ConfigureAwait(false);
                viewModel.Team.Players = (await _playerDataSource.ReadPlayerIdentities(new PlayerIdentityQuery
                {
                    TeamIds = teamIds
                }).ConfigureAwait(false))?.Select(x => new Player { PlayerIdentities = new List<PlayerIdentity> { x } }).ToList();
            }

            viewModel.Metadata.PageTitle = $"Delete {viewModel.Team.TeamName}";
            return View("DeleteTeam", viewModel);
        }
    }
}