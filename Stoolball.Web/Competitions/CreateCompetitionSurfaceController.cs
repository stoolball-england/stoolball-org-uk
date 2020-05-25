using Stoolball.Competitions;
using Stoolball.Routing;
using Stoolball.Umbraco.Data.Competitions;
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

namespace Stoolball.Web.Competitions
{
    public class CreateCompetitionSurfaceController : SurfaceController
    {
        private readonly ICompetitionRepository _competitionRepository;
        private readonly IRouteGenerator _routeGenerator;

        public CreateCompetitionSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, ICompetitionRepository seasonRepository, IRouteGenerator routeGenerator)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _competitionRepository = seasonRepository ?? throw new System.ArgumentNullException(nameof(seasonRepository));
            _routeGenerator = routeGenerator ?? throw new System.ArgumentNullException(nameof(routeGenerator));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        public async Task<ActionResult> CreateCompetition([Bind(Prefix = "Competition", Include = "CompetitionName,FromYear,UntilYear,PlayerType,PlayersPerTeam,Overs,Facebook,Twitter,Instagram,YouTube,Website")]Competition competition)
        {
            if (competition is null)
            {
                throw new System.ArgumentNullException(nameof(competition));
            }

            // get this from the unvalidated form instead of via modelbinding so that HTML can be allowed
            competition.Introduction = Request.Unvalidated.Form["Competition.Introduction"];
            competition.PublicContactDetails = Request.Unvalidated.Form["Competition.PublicContactDetails"];
            competition.PrivateContactDetails = Request.Unvalidated.Form["Competition.PrivateContactDetails"];

            var isAuthorized = Members.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.Editors, Groups.AllMembers }, null);

            if (isAuthorized && ModelState.IsValid)
            {
                // Create an owner group
                var groupName = _routeGenerator.GenerateRoute("competition", competition.CompetitionName, NoiseWords.CompetitionRoute);
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
                        competition.MemberGroupId = group.Id;
                        competition.MemberGroupName = group.Name;
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

                // Create the competition
                await _competitionRepository.CreateCompetition(competition, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                // Redirect to the competition
                return Redirect(competition.CompetitionRoute);
            }

            var viewModel = new CompetitionViewModel(CurrentPage)
            {
                Competition = competition,
                IsAuthorized = isAuthorized
            };
            viewModel.Metadata.PageTitle = $"Add a competition";
            return View("CreateCompetition", viewModel);
        }
    }
}