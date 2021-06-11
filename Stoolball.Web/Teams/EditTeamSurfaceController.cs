using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Caching;
using Stoolball.Navigation;
using Stoolball.Security;
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
    public class EditTeamSurfaceController : SurfaceController
    {
        private readonly ITeamDataSource _teamDataSource;
        private readonly ITeamRepository _teamRepository;
        private readonly IAuthorizationPolicy<Team> _authorizationPolicy;
        private readonly ICacheOverride _cacheOverride;

        public EditTeamSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, ITeamDataSource teamDataSource, ITeamRepository teamRepository,
            IAuthorizationPolicy<Team> authorizationPolicy, ICacheOverride cacheOverride)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _teamDataSource = teamDataSource;
            _teamRepository = teamRepository ?? throw new System.ArgumentNullException(nameof(teamRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new System.ArgumentNullException(nameof(authorizationPolicy));
            _cacheOverride = cacheOverride ?? throw new ArgumentNullException(nameof(cacheOverride));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(TinyMCE = true, Forms = true)]
        public async Task<ActionResult> UpdateTeam([Bind(Prefix = "Team", Include = "TeamName,TeamType,AgeRangeLower,AgeRangeUpper,UntilYear,PlayerType,MatchLocations,Facebook,Twitter,Instagram,YouTube,Website")] Team team)
        {
            if (team is null)
            {
                throw new System.ArgumentNullException(nameof(team));
            }

            var beforeUpdate = await _teamDataSource.ReadTeamByRoute(Request.RawUrl).ConfigureAwait(false);
            team.TeamId = beforeUpdate.TeamId;
            team.TeamRoute = beforeUpdate.TeamRoute;

            // get this from the unvalidated form instead of via modelbinding so that HTML can be allowed
            team.Introduction = Request.Unvalidated.Form["Team.Introduction"];
            team.PlayingTimes = Request.Unvalidated.Form["Team.PlayingTimes"];
            team.Cost = Request.Unvalidated.Form["Team.Cost"];
            team.PublicContactDetails = Request.Unvalidated.Form["Team.PublicContactDetails"];
            team.PrivateContactDetails = Request.Unvalidated.Form["Team.PrivateContactDetails"];

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
                ModelState[key].Errors.Clear();
            }

            var isAuthorized = _authorizationPolicy.IsAuthorized(beforeUpdate);

            if (isAuthorized[AuthorizedAction.EditTeam] && ModelState.IsValid)
            {
                var currentMember = Members.GetCurrentMember();
                var updatedTeam = await _teamRepository.UpdateTeam(team, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                _cacheOverride.OverrideCacheForCurrentMember(CacheConstants.TeamListingsCacheKeyPrefix);

                // redirect back to the team actions
                return Redirect(updatedTeam.TeamRoute + "/edit");
            }

            var model = new TeamViewModel(CurrentPage, Services.UserService)
            {
                Team = team,
            };
            model.IsAuthorized = isAuthorized;
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