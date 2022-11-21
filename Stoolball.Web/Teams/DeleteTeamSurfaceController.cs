using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Web.Security;
using Stoolball.Web.Teams.Models;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Website.Controllers;

namespace Stoolball.Web.Teams
{
    public class DeleteTeamSurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly ITeamDataSource _teamDataSource;
        private readonly ITeamRepository _teamRepository;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IPlayerDataSource _playerDataSource;
        private readonly IAuthorizationPolicy<Team> _authorizationPolicy;
        private readonly IListingCacheInvalidator<Team> _teamListingCacheClearer;
        private readonly IMatchListingCacheInvalidator _matchListingCacheClearer;

        public DeleteTeamSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberManager memberManager,
            ITeamDataSource teamDataSource, ITeamRepository teamRepository, IMatchListingDataSource matchDataSource, IPlayerDataSource playerDataSource,
            IAuthorizationPolicy<Team> authorizationPolicy, IListingCacheInvalidator<Team> teamListingCacheClearer, IMatchListingCacheInvalidator matchListingCacheClearer)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _teamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _playerDataSource = playerDataSource ?? throw new ArgumentNullException(nameof(playerDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _teamListingCacheClearer = teamListingCacheClearer ?? throw new ArgumentNullException(nameof(teamListingCacheClearer));
            _matchListingCacheClearer = matchListingCacheClearer ?? throw new ArgumentNullException(nameof(matchListingCacheClearer));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<IActionResult> DeleteTeam([Bind("RequiredText", "ConfirmationText", Prefix = "ConfirmDeleteRequest")] MatchingTextConfirmation postedModel)
        {
            if (postedModel is null)
            {
                throw new ArgumentNullException(nameof(postedModel));
            }

            var model = new DeleteTeamViewModel(CurrentPage, Services.UserService)
            {
                Team = await _teamDataSource.ReadTeamByRoute(Request.Path, true).ConfigureAwait(false),
            };
            model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(model.Team);

            if (model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.DeleteTeam] && ModelState.IsValid)
            {
                var memberGroup = Services.MemberGroupService.GetById(model.Team.MemberGroupKey!.Value);
                if (memberGroup != null)
                {
                    Services.MemberGroupService.Delete(memberGroup);
                }

                var currentMember = await _memberManager.GetCurrentMemberAsync();
                await _teamRepository.DeleteTeam(model.Team, currentMember.Key, currentMember.Name);
                _teamListingCacheClearer.InvalidateCache();
                await _matchListingCacheClearer.InvalidateCacheForTeam(model.Team.TeamId!.Value).ConfigureAwait(false);
                model.Deleted = true;
            }
            else
            {
                var teamIds = new List<Guid> { model.Team.TeamId!.Value };
                model.TotalMatches = await _matchDataSource.ReadTotalMatches(new MatchFilter
                {
                    TeamIds = teamIds,
                    IncludeTournamentMatches = true
                });
                model.Team.Players = (await _playerDataSource.ReadPlayerIdentities(new PlayerFilter
                {
                    TeamIds = teamIds
                }))?.Select(x => new Player { PlayerIdentities = new List<PlayerIdentity> { x } }).ToList();
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