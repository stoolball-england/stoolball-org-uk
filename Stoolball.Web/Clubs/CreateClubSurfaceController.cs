using Stoolball.Clubs;
using Stoolball.Routing;
using Stoolball.Umbraco.Data.Clubs;
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

namespace Stoolball.Web.Clubs
{
    public class CreateClubSurfaceController : SurfaceController
    {
        private readonly IClubRepository _clubRepository;
        private readonly IRouteGenerator _routeGenerator;

        public CreateClubSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, IClubRepository clubRepository, IRouteGenerator routeGenerator)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _clubRepository = clubRepository ?? throw new System.ArgumentNullException(nameof(clubRepository));
            _routeGenerator = routeGenerator ?? throw new System.ArgumentNullException(nameof(routeGenerator));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        public async Task<ActionResult> CreateClub([Bind(Prefix = "Club", Include = "ClubName,ClubMark,Teams")]Club club)
        {
            if (club is null)
            {
                throw new System.ArgumentNullException(nameof(club));
            }

            var isAuthorized = Members.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.Editors, Groups.AllMembers }, null);

            if (isAuthorized && ModelState.IsValid)
            {
                // Create an owner group
                var groupName = _routeGenerator.GenerateRoute("club", club.ClubName, NoiseWords.ClubRoute);
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
                        club.MemberGroupId = group.Id;
                        club.MemberGroupName = group.Name;
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

                // Create the club
                await _clubRepository.CreateClub(club, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                // Redirect to the club
                return Redirect(club.ClubRoute);
            }

            var viewModel = new ClubViewModel(CurrentPage)
            {
                Club = club,
                IsAuthorized = isAuthorized
            };
            viewModel.Metadata.PageTitle = $"Add a club";
            return View("CreateClub", viewModel);
        }
    }
}