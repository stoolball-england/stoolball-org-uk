using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Data.Abstractions;
using Stoolball.MatchLocations;
using Stoolball.Navigation;
using Stoolball.Security;
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
    public class EditTeamSurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly ITeamDataSource _teamDataSource;
        private readonly ITeamRepository _teamRepository;
        private readonly IAuthorizationPolicy<Team> _authorizationPolicy;
        private readonly IListingCacheInvalidator<Team> _teamListingCacheClearer;
        private readonly IListingCacheInvalidator<MatchLocation> _matchLocationCacheClearer;

        public EditTeamSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberManager memberManager,
            ITeamDataSource teamDataSource, ITeamRepository teamRepository, IAuthorizationPolicy<Team> authorizationPolicy,
            IListingCacheInvalidator<Team> teamListingCacheClearer, IListingCacheInvalidator<MatchLocation> matchLocationCacheClearer)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _teamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _teamListingCacheClearer = teamListingCacheClearer ?? throw new ArgumentNullException(nameof(teamListingCacheClearer));
            _matchLocationCacheClearer = matchLocationCacheClearer ?? throw new ArgumentNullException(nameof(matchLocationCacheClearer));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(TinyMCE = true, Forms = true)]
        public async Task<IActionResult> UpdateTeam([Bind("TeamName", "TeamType", "AgeRangeLower", "AgeRangeUpper", "UntilYear", "PlayerType", "MatchLocations", "Facebook", "Twitter", "Instagram", "YouTube", "Website", Prefix = "Team")] Team team)
        {
            if (team is null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            var beforeUpdate = await _teamDataSource.ReadTeamByRoute(Request.Path).ConfigureAwait(false);
            team.TeamId = beforeUpdate.TeamId;
            team.TeamRoute = beforeUpdate.TeamRoute;

            // get this from the form instead of via modelbinding so that HTML can be allowed
            team.Introduction = Request.Form["Team.Introduction"];
            team.PlayingTimes = Request.Form["Team.PlayingTimes"];
            team.Cost = Request.Form["Team.Cost"];
            team.PublicContactDetails = Request.Form["Team.PublicContactDetails"];
            team.PrivateContactDetails = Request.Form["Team.PrivateContactDetails"];

            if (team.AgeRangeLower < 11 && !team.AgeRangeUpper.HasValue)
            {
                ModelState.AddModelError("Team.AgeRangeUpper", "School teams and junior teams must specify a maximum age for players");
            }

            if (team.AgeRangeUpper.HasValue && team.AgeRangeUpper < team.AgeRangeLower)
            {
                ModelState.AddModelError("Team.AgeRangeUpper", "The maximum age for players cannot be lower than the minimum age");
            }

            // We're not interested in validating the details of the selected locations
            foreach (var key in ModelState.Keys.Where(x => x.StartsWith("Team.MatchLocations", StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.Remove(key);
            }

            var isAuthorized = await _authorizationPolicy.IsAuthorized(beforeUpdate);

            if (isAuthorized[AuthorizedAction.EditTeam] && ModelState.IsValid)
            {
                var currentMember = await _memberManager.GetCurrentMemberAsync();
                var updatedTeam = await _teamRepository.UpdateTeam(team, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                _teamListingCacheClearer.InvalidateCache();
                _matchLocationCacheClearer.InvalidateCache(); // Because if a match location gets its first team, it should start appearing in listings

                // redirect back to the team actions
                return Redirect(updatedTeam.TeamRoute + "/edit");
            }

            var model = new TeamViewModel(CurrentPage, Services.UserService)
            {
                Team = team,
            };
            model.Authorization.CurrentMemberIsAuthorized = isAuthorized;
            model.Metadata.PageTitle = $"Edit {team.TeamName}";

            model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Teams, Url = new Uri(Constants.Pages.TeamsUrl, UriKind.Relative) });
            if (model.Team.Club != null)
            {
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Team.Club.ClubName, Url = new Uri(model.Team.Club.ClubRoute, UriKind.Relative) });
            }
            model.Breadcrumbs.Add(new Breadcrumb { Name = model.Team.TeamName, Url = new Uri(model.Team.TeamRoute, UriKind.Relative) });

            return View("EditTeam", model);
        }
    }
}