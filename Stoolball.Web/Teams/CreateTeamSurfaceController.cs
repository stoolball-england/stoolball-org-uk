using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Caching;
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
    public class CreateTeamSurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly ITeamRepository _teamRepository;
        private readonly IAuthorizationPolicy<Team> _authorizationPolicy;
        private readonly ICacheOverride _cacheOverride;

        public CreateTeamSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberManager memberManager,
            ITeamRepository teamRepository, IAuthorizationPolicy<Team> authorizationPolicy, ICacheOverride cacheOverride)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _teamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _cacheOverride = cacheOverride ?? throw new ArgumentNullException(nameof(cacheOverride));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(TinyMCE = true, Forms = true)]
        public async Task<IActionResult> CreateTeam([Bind("TeamName", "TeamType", "AgeRangeLower", "AgeRangeUpper", "UntilYear", "PlayerType", "MatchLocations", "Facebook", "Twitter", "Instagram", "YouTube", "Website", Prefix = "Team")] Team team)
        {
            if (team is null)
            {
                throw new ArgumentNullException(nameof(team));
            }

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

            var isAuthorized = await _authorizationPolicy.IsAuthorized(team);

            if (isAuthorized[AuthorizedAction.CreateTeam] && ModelState.IsValid)
            {
                // Create the team
                var currentMember = await _memberManager.GetCurrentMemberAsync();
                var createdTeam = await _teamRepository.CreateTeam(team, currentMember.Key, (await _memberManager.GetCurrentMemberAsync()).UserName, currentMember.Name).ConfigureAwait(false);

                await _cacheOverride.OverrideCacheForCurrentMember(CacheConstants.TeamListingsCacheKeyPrefix).ConfigureAwait(false);

                // Redirect to the team
                return Redirect(createdTeam.TeamRoute);
            }

            var viewModel = new TeamViewModel(CurrentPage, Services.UserService)
            {
                Team = team,
            };
            viewModel.IsAuthorized = isAuthorized;
            viewModel.Metadata.PageTitle = $"Add a team";

            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Teams, Url = new Uri(Constants.Pages.TeamsUrl, UriKind.Relative) });

            return View("CreateTeam", viewModel);
        }
    }
}