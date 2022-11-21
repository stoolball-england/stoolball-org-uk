using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Data.Abstractions;
using Stoolball.MatchLocations;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.MatchLocations.Models;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Website.Controllers;

namespace Stoolball.Web.MatchLocations
{
    public class EditMatchLocationSurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly IMatchLocationDataSource _matchLocationDataSource;
        private readonly IMatchLocationRepository _matchLocationRepository;
        private readonly IAuthorizationPolicy<MatchLocation> _authorizationPolicy;
        private readonly IListingCacheInvalidator<MatchLocation> _cacheClearer;

        public EditMatchLocationSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberManager memberManager,
            IMatchLocationDataSource matchLocationDataSource, IMatchLocationRepository matchLocationRepository,
            IAuthorizationPolicy<MatchLocation> authorizationPolicy, IListingCacheInvalidator<MatchLocation> cacheClearer)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _matchLocationDataSource = matchLocationDataSource ?? throw new ArgumentNullException(nameof(matchLocationDataSource));
            _matchLocationRepository = matchLocationRepository ?? throw new ArgumentNullException(nameof(matchLocationRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _cacheClearer = cacheClearer ?? throw new ArgumentNullException(nameof(cacheClearer));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(GoogleMaps = true, TinyMCE = true, Forms = true)]
        public async Task<IActionResult> UpdateMatchLocation([Bind("SecondaryAddressableObjectName", "PrimaryAddressableObjectName", "StreetDescription", "Locality", "Town", "AdministrativeArea", "Postcode", "GeoPrecision", "Latitude", "Longitude", Prefix = "MatchLocation")] MatchLocation location)
        {
            var beforeUpdate = await _matchLocationDataSource.ReadMatchLocationByRoute(Request.Path);
            location.MatchLocationId = beforeUpdate.MatchLocationId;
            location.MatchLocationRoute = beforeUpdate.MatchLocationRoute;

            // get this from the form instead of via modelbinding so that HTML can be allowed
            location.MatchLocationNotes = Request.Form["MatchLocation.MatchLocationNotes"];

            var isAuthorized = await _authorizationPolicy.IsAuthorized(beforeUpdate);

            if (isAuthorized[AuthorizedAction.EditMatchLocation] && ModelState.IsValid)
            {
                var currentMember = await _memberManager.GetCurrentMemberAsync();
                var updatedMatchLocation = await _matchLocationRepository.UpdateMatchLocation(location, currentMember.Key, currentMember.Name);
                _cacheClearer.InvalidateCache();

                return Redirect(updatedMatchLocation.MatchLocationRoute + "/edit");
            }

            var viewModel = new MatchLocationViewModel(CurrentPage, Services.UserService)
            {
                MatchLocation = location,
            };
            viewModel.Authorization.CurrentMemberIsAuthorized = isAuthorized;
            viewModel.Metadata.PageTitle = $"Edit {location.NameAndLocalityOrTown()}";

            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.MatchLocations, Url = new Uri(Constants.Pages.MatchLocationsUrl, UriKind.Relative) });
            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = viewModel.MatchLocation.NameAndLocalityOrTownIfDifferent(), Url = new Uri(viewModel.MatchLocation.MatchLocationRoute, UriKind.Relative) });

            return View("EditMatchLocation", viewModel);
        }
    }
}