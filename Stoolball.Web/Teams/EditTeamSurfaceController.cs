using Stoolball.Teams;
using Stoolball.Umbraco.Data.Teams;
using Stoolball.Web.Security;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Web.Teams
{
    public class EditTeamSurfaceController : SurfaceController
    {
        private readonly ITeamDataSource _teamDataSource;
        private readonly ITeamRepository _teamRepository;

        public EditTeamSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, ITeamDataSource teamDataSource, ITeamRepository teamRepository)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _teamDataSource = teamDataSource;
            _teamRepository = teamRepository ?? throw new System.ArgumentNullException(nameof(teamRepository));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(TinyMCE = true, Forms = true)]
        public async Task<ActionResult> UpdateTeam([Bind(Prefix = "Team", Include = "TeamName,TeamType,AgeRangeLower,AgeRangeUpper,FromYear,UntilYear,PlayerType,MatchLocations,Facebook,Twitter,Instagram,YouTube,Website")] Team team)
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

            var isAuthorized = Members.IsMemberAuthorized(null, new[] { Groups.Administrators, beforeUpdate.MemberGroupName }, null);

            if (isAuthorized && ModelState.IsValid)
            {
                var currentMember = Members.GetCurrentMember();
                await _teamRepository.UpdateTeam(team, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                // redirect back to the team
                return Redirect(team.TeamRoute);
            }

            var viewModel = new TeamViewModel(CurrentPage)
            {
                Team = team,
                IsAuthorized = isAuthorized
            };
            viewModel.Metadata.PageTitle = $"Edit {team.TeamName}";
            return View("EditTeam", viewModel);
        }
    }
}