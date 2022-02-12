using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Caching;
using Stoolball.Clubs;
using Stoolball.Navigation;
using Stoolball.Routing;
using Stoolball.Security;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Website.Controllers;
using static Stoolball.Constants;

namespace Stoolball.Web.Clubs
{
    public class CreateClubSurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly IClubRepository _clubRepository;
        private readonly IRouteGenerator _routeGenerator;
        private readonly IAuthorizationPolicy<Club> _authorizationPolicy;
        private readonly ICacheOverride _cacheOverride;

        public CreateClubSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberManager memberManager,
            IClubRepository clubRepository, IRouteGenerator routeGenerator, IAuthorizationPolicy<Club> authorizationPolicy,
            ICacheOverride cacheOverride)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _clubRepository = clubRepository ?? throw new ArgumentNullException(nameof(clubRepository));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _cacheOverride = cacheOverride ?? throw new ArgumentNullException(nameof(cacheOverride));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<IActionResult> CreateClub([Bind("ClubName", "Teams", Prefix = "Club")] Club club)
        {
            if (club is null)
            {
                throw new ArgumentNullException(nameof(club));
            }

            // We're not interested in validating the details of the selected teams
            foreach (var key in ModelState.Keys.Where(x => x.StartsWith("Club.Teams", StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.Remove(key);
            }

            var isAuthorized = await _authorizationPolicy.IsAuthorized(club);

            if (isAuthorized[AuthorizedAction.CreateClub] && ModelState.IsValid)
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
                var currentMember = await _memberManager.GetCurrentMemberAsync();
                if (!await _memberManager.IsMemberAuthorizedAsync(null, new[] { Groups.Administrators }, null))
                {
                    Services.MemberService.AssignRole(currentMember.Id, group!.Name);
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