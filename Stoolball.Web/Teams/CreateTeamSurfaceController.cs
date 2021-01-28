using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
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
    public class CreateTeamSurfaceController : SurfaceController
    {
        private readonly ITeamRepository _teamRepository;
        private readonly IAuthorizationPolicy<Team> _authorizationPolicy;

        public CreateTeamSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, ITeamRepository teamRepository,
            IAuthorizationPolicy<Team> authorizationPolicy)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _teamRepository = teamRepository ?? throw new System.ArgumentNullException(nameof(teamRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new System.ArgumentNullException(nameof(authorizationPolicy));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(TinyMCE = true, Forms = true)]
        public async Task<ActionResult> CreateTeam([Bind(Prefix = "Team", Include = "TeamName,TeamType,AgeRangeLower,AgeRangeUpper,UntilYear,PlayerType,MatchLocations,Facebook,Twitter,Instagram,YouTube,Website")] Team team)
        {
            if (team is null)
            {
                throw new System.ArgumentNullException(nameof(team));
            }

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

            var isAuthorized = _authorizationPolicy.IsAuthorized(team);

            if (isAuthorized[AuthorizedAction.CreateTeam] && ModelState.IsValid)
            {
                // Create the team
                var currentMember = Members.GetCurrentMember();
                var createdTeam = await _teamRepository.CreateTeam(team, currentMember.Key, Members.CurrentUserName, currentMember.Name).ConfigureAwait(false);

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