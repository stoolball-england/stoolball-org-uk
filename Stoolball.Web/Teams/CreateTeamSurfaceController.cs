using Stoolball.Routing;
using Stoolball.Teams;
using Stoolball.Umbraco.Data.Teams;
using Stoolball.Web.Security;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Web.Teams
{
    public class CreateTeamSurfaceController : SurfaceController
    {
        private readonly ITeamRepository _teamRepository;
        private readonly IRouteGenerator _routeGenerator;

        public CreateTeamSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, ITeamRepository teamRepository, IRouteGenerator routeGenerator)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _teamRepository = teamRepository ?? throw new System.ArgumentNullException(nameof(teamRepository));
            _routeGenerator = routeGenerator ?? throw new System.ArgumentNullException(nameof(routeGenerator));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(TinyMCE = true, Forms = true)]
        public async Task<ActionResult> CreateTeam([Bind(Prefix = "Team", Include = "TeamName,TeamType,AgeRangeLower,AgeRangeUpper,FromYear,UntilYear,PlayerType,MatchLocations,Facebook,Twitter,Instagram,YouTube,Website")] Team team)
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

            var isAuthorized = Members.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.Editors, Groups.AllMembers }, null);

            if (isAuthorized && ModelState.IsValid)
            {
                // Create an owner group
                var groupName = _routeGenerator.GenerateRoute("team", team.TeamName, NoiseWords.TeamRoute);
                IMemberGroup group;
                do
                {
                    group = Services.MemberGroupService.GetByName(groupName);
                    if (group == null)
                    {
                        group = new MemberGroup
                        {
                            Name = groupName
                        };
                        Services.MemberGroupService.Save(group);
                        team.MemberGroupId = group.Id;
                        team.MemberGroupName = group.Name;
                        break;
                    }
                    else
                    {
                        groupName = _routeGenerator.IncrementRoute(groupName);
                    }
                }
                while (group != null);

                // Assign the current member to the group unless they're already admin
                var currentMember = Members.GetCurrentMember();
                if (!Members.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.Editors }, null))
                {
                    Services.MemberService.AssignRole(currentMember.Id, group.Name);
                }

                // Create the team
                await _teamRepository.CreateTeam(team, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                // Redirect to the team
                return Redirect(team.TeamRoute);
            }

            var viewModel = new TeamViewModel(CurrentPage)
            {
                Team = team,
                IsAuthorized = isAuthorized
            };
            viewModel.Metadata.PageTitle = $"Add a team";
            return View("CreateTeam", viewModel);
        }
    }
}