using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Caching;
using Stoolball.Clubs;
using Stoolball.Navigation;
using Stoolball.Routing;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;
using static Stoolball.Constants;

namespace Stoolball.Web.Clubs
{
    public class CreateClubSurfaceController : SurfaceController
    {
        private readonly IClubRepository _clubRepository;
        private readonly IRouteGenerator _routeGenerator;
        private readonly IAuthorizationPolicy<Club> _authorizationPolicy;
        private readonly ICacheOverride _cacheOverride;

        public CreateClubSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, IClubRepository clubRepository, IRouteGenerator routeGenerator,
            IAuthorizationPolicy<Club> authorizationPolicy, ICacheOverride cacheOverride)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _clubRepository = clubRepository ?? throw new System.ArgumentNullException(nameof(clubRepository));
            _routeGenerator = routeGenerator ?? throw new System.ArgumentNullException(nameof(routeGenerator));
            _authorizationPolicy = authorizationPolicy ?? throw new System.ArgumentNullException(nameof(authorizationPolicy));
            _cacheOverride = cacheOverride ?? throw new ArgumentNullException(nameof(cacheOverride));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<ActionResult> CreateClub([Bind(Prefix = "Club", Include = "ClubName,Teams")] Club club)
        {
            if (club is null)
            {
                throw new System.ArgumentNullException(nameof(club));
            }

            // We're not interested in validating the details of the selected teams
            foreach (var key in ModelState.Keys.Where(x => x.StartsWith("Club.Teams", StringComparison.OrdinalIgnoreCase)))
            {
                ModelState[key].Errors.Clear();
            }

            var isAuthorized = _authorizationPolicy.IsAuthorized(club);

            if (isAuthorized[Stoolball.Security.AuthorizedAction.CreateClub] && ModelState.IsValid)
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
                        club.MemberGroupKey = group.Key;
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
                if (!Members.IsMemberAuthorized(null, new[] { Groups.Administrators }, null))
                {
                    Services.MemberService.AssignRole(currentMember.Id, group.Name);
                }

                // Create the club
                var createdClub = await _clubRepository.CreateClub(club, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                await _cacheOverride.OverrideCacheForCurrentMember(CacheConstants.TeamListingsCacheKeyPrefix).ConfigureAwait(false);

                // Redirect to the club
                return Redirect(createdClub.ClubRoute);
            }

            var viewModel = new ClubViewModel(CurrentPage, Services.UserService)
            {
                Club = club,
            };
            viewModel.IsAuthorized = isAuthorized;
            viewModel.Metadata.PageTitle = $"Add a club";

            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Teams, Url = new Uri(Constants.Pages.TeamsUrl, UriKind.Relative) });

            return View("CreateClub", viewModel);
        }
    }
}