using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Caching;
using Stoolball.MatchLocations;
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

namespace Stoolball.Web.MatchLocations
{
    public class CreateMatchLocationSurfaceController : SurfaceController
    {
        private readonly IMatchLocationRepository _matchLocationRepository;
        private readonly IAuthorizationPolicy<MatchLocation> _authorizationPolicy;
        private readonly IRouteGenerator _routeGenerator;
        private readonly ICacheOverride _cacheOverride;

        public CreateMatchLocationSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, IMatchLocationRepository matchLocationRepository,
           IAuthorizationPolicy<MatchLocation> authorizationPolicy, IRouteGenerator routeGenerator, ICacheOverride cacheOverride)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _matchLocationRepository = matchLocationRepository ?? throw new System.ArgumentNullException(nameof(matchLocationRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new System.ArgumentNullException(nameof(authorizationPolicy));
            _routeGenerator = routeGenerator ?? throw new System.ArgumentNullException(nameof(routeGenerator));
            _cacheOverride = cacheOverride ?? throw new ArgumentNullException(nameof(cacheOverride));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(GoogleMaps = true, TinyMCE = true, Forms = true)]
        public async Task<ActionResult> CreateMatchLocation([Bind(Prefix = "MatchLocation", Include = "SecondaryAddressableObjectName,PrimaryAddressableObjectName,StreetDescription,Locality,Town,AdministrativeArea,Postcode,GeoPrecision,Latitude,Longitude")] MatchLocation location)
        {
            if (location is null)
            {
                throw new System.ArgumentNullException(nameof(location));
            }

            // get this from the unvalidated form instead of via modelbinding so that HTML can be allowed
            location.MatchLocationNotes = Request.Unvalidated.Form["MatchLocation.MatchLocationNotes"];

            var isAuthorized = _authorizationPolicy.IsAuthorized(location);

            if (isAuthorized[Stoolball.Security.AuthorizedAction.CreateMatchLocation] && ModelState.IsValid)
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
                var currentMember = Members.GetCurrentMember();
                if (!Members.IsMemberAuthorized(null, new[] { Groups.Administrators }, null))
                {
                    Services.MemberService.AssignRole(currentMember.Id, group.Name);
                }

                // Create the location
                var createdMatchLocation = await _matchLocationRepository.CreateMatchLocation(location, currentMember.Key, currentMember.Name).ConfigureAwait(false);
                await _cacheOverride.OverrideCacheForCurrentMember(CacheConstants.MatchLocationsCacheKeyPrefix).ConfigureAwait(false);

                // Redirect to the location
                return Redirect(createdMatchLocation.MatchLocationRoute);
            }

            var viewModel = new MatchLocationViewModel(CurrentPage, Services.UserService)
            {
                MatchLocation = location,
            };
            viewModel.IsAuthorized = isAuthorized;
            viewModel.Metadata.PageTitle = $"Add a ground or sports centre";

            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.MatchLocations, Url = new Uri(Constants.Pages.MatchLocationsUrl, UriKind.Relative) });

            return View("CreateMatchLocation", viewModel);
        }
    }
}