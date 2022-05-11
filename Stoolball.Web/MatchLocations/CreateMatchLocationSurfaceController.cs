using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Caching;
using Stoolball.MatchLocations;
using Stoolball.Navigation;
using Stoolball.Routing;
using Stoolball.Security;
using Stoolball.Web.MatchLocations.Models;
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

namespace Stoolball.Web.MatchLocations
{
    public class CreateMatchLocationSurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly IMatchLocationRepository _matchLocationRepository;
        private readonly IAuthorizationPolicy<MatchLocation> _authorizationPolicy;
        private readonly IRouteGenerator _routeGenerator;
        private readonly ICacheOverride _cacheOverride;

        public CreateMatchLocationSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberManager memberManager,
            IMatchLocationRepository matchLocationRepository, IAuthorizationPolicy<MatchLocation> authorizationPolicy,
            IRouteGenerator routeGenerator, ICacheOverride cacheOverride)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _matchLocationRepository = matchLocationRepository ?? throw new ArgumentNullException(nameof(matchLocationRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
            _cacheOverride = cacheOverride ?? throw new ArgumentNullException(nameof(cacheOverride));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(GoogleMaps = true, TinyMCE = true, Forms = true)]
        public async Task<IActionResult> CreateMatchLocation([Bind("SecondaryAddressableObjectName", "PrimaryAddressableObjectName", "StreetDescription", "Locality", "Town", "AdministrativeArea", "Postcode", "GeoPrecision", "Latitude", "Longitude", Prefix = "MatchLocation")] MatchLocation location)
        {
            if (location is null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            // get this from the form instead of via modelbinding so that HTML can be allowed
            location.MatchLocationNotes = Request.Form["MatchLocation.MatchLocationNotes"];

            var isAuthorized = await _authorizationPolicy.IsAuthorized(location);

            if (isAuthorized[AuthorizedAction.CreateMatchLocation] && ModelState.IsValid)
            {
                // Create an owner group
                var groupName = _routeGenerator.GenerateRoute("location", location.NameAndLocalityOrTownIfDifferent(), NoiseWords.MatchLocationRoute);
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
                        location.MemberGroupKey = group.Key;
                        location.MemberGroupName = group.Name;
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

                // Create the location
                var createdMatchLocation = await _matchLocationRepository.CreateMatchLocation(location, currentMember.Key, currentMember.Name);
                await _cacheOverride.OverrideCacheForCurrentMember(CacheConstants.MatchLocationsCacheKeyPrefix);

                // Redirect to the location
                return Redirect(createdMatchLocation.MatchLocationRoute);
            }

            var viewModel = new MatchLocationViewModel(CurrentPage, Services.UserService)
            {
                MatchLocation = location,
            };
            viewModel.Authorization.CurrentMemberIsAuthorized = isAuthorized;
            viewModel.Metadata.PageTitle = $"Add a ground or sports centre";

            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.MatchLocations, Url = new Uri(Constants.Pages.MatchLocationsUrl, UriKind.Relative) });

            return View("CreateMatchLocation", viewModel);
        }
    }
}