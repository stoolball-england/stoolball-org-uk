using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Caching;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Teams;
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
        private readonly ICacheOverride _cacheOverride;

        public DeleteTeamSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, ITeamDataSource teamDataSource, ITeamRepository teamRepository,
            IMatchListingDataSource matchDataSource, IPlayerDataSource playerDataSource, IAuthorizationPolicy<Team> authorizationPolicy, ICacheOverride cacheOverride)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _teamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _playerDataSource = playerDataSource ?? throw new ArgumentNullException(nameof(playerDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _cacheOverride = cacheOverride ?? throw new ArgumentNullException(nameof(cacheOverride));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<ActionResult> DeleteTeam([Bind(Prefix = "ConfirmDeleteRequest", Include = "RequiredText,ConfirmationText")] MatchingTextConfirmation postedModel)
        {
            if (postedModel is null)
            {
                throw new ArgumentNullException(nameof(postedModel));
            }

            var model = new DeleteTeamViewModel(CurrentPage, Services.UserService)
            {
                Team = await _teamDataSource.ReadTeamByRoute(Request.RawUrl, true).ConfigureAwait(false),
            };
            model.IsAuthorized = _authorizationPolicy.IsAuthorized(model.Team);

            if (model.IsAuthorized[AuthorizedAction.DeleteTeam] && ModelState.IsValid)
            {
                var memberGroup = Services.MemberGroupService.GetById(model.Team.MemberGroupKey.Value);
                if (memberGroup != null)
                {
                    Services.MemberGroupService.Delete(memberGroup);
                }

                var currentMember = Members.GetCurrentMember();
                await _teamRepository.DeleteTeam(model.Team, currentMember.Key, currentMember.Name).ConfigureAwait(false);
                _cacheOverride.OverrideCacheForCurrentMember(CacheConstants.TeamListingsCacheKeyPrefix);
                model.Deleted = true;
            }
            else
            {
                var teamIds = new List<Guid> { model.Team.TeamId.Value };
                model.TotalMatches = await _matchDataSource.ReadTotalMatches(new MatchFilter
                {
                    TeamIds = teamIds,
                    IncludeTournamentMatches = true
                }).ConfigureAwait(false);
                model.Team.Players = (await _playerDataSource.ReadPlayerIdentities(new PlayerFilter
                {
                    TeamIds = teamIds
                }).ConfigureAwait(false))?.Select(x => new Player { PlayerIdentities = new List<PlayerIdentity> { x } }).ToList();
            }

            model.Metadata.PageTitle = $"Delete {model.Team.TeamName}";

            model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Teams, Url = new Uri(Constants.Pages.TeamsUrl, UriKind.Relative) });
            if (model.Team.Club != null)
            {
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Team.Club.ClubName, Url = new Uri(model.Team.Club.ClubRoute, UriKind.Relative) });
            }
            if (!model.Deleted)
            {
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Team.TeamName, Url = new Uri(model.Team.TeamRoute, UriKind.Relative) });
            }

            return View("DeleteTeam", model);
        }
    }
}