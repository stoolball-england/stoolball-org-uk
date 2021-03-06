﻿using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Caching;
using Stoolball.MatchLocations;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;

namespace Stoolball.Web.MatchLocations
{
    public class EditMatchLocationSurfaceController : SurfaceController
    {
        private readonly IMatchLocationDataSource _matchLocationDataSource;
        private readonly IMatchLocationRepository _matchLocationRepository;
        private readonly IAuthorizationPolicy<MatchLocation> _authorizationPolicy;
        private readonly ICacheOverride _cacheOverride;

        public EditMatchLocationSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, IMatchLocationDataSource matchLocationDataSource,
            IMatchLocationRepository matchLocationRepository, IAuthorizationPolicy<MatchLocation> authorizationPolicy, ICacheOverride cacheOverride)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _matchLocationDataSource = matchLocationDataSource ?? throw new System.ArgumentNullException(nameof(matchLocationDataSource));
            _matchLocationRepository = matchLocationRepository ?? throw new System.ArgumentNullException(nameof(matchLocationRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new System.ArgumentNullException(nameof(authorizationPolicy));
            _cacheOverride = cacheOverride ?? throw new ArgumentNullException(nameof(cacheOverride));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(GoogleMaps = true, TinyMCE = true, Forms = true)]
        public async Task<ActionResult> UpdateMatchLocation([Bind(Prefix = "MatchLocation", Include = "SecondaryAddressableObjectName,PrimaryAddressableObjectName,StreetDescription,Locality,Town,AdministrativeArea,Postcode,GeoPrecision,Latitude,Longitude")] MatchLocation location)
        {
            if (location is null)
            {
                throw new System.ArgumentNullException(nameof(location));
            }

            var beforeUpdate = await _matchLocationDataSource.ReadMatchLocationByRoute(Request.RawUrl).ConfigureAwait(false);
            location.MatchLocationId = beforeUpdate.MatchLocationId;
            location.MatchLocationRoute = beforeUpdate.MatchLocationRoute;

            // get this from the unvalidated form instead of via modelbinding so that HTML can be allowed
            location.MatchLocationNotes = Request.Unvalidated.Form["MatchLocation.MatchLocationNotes"];

            var isAuthorized = _authorizationPolicy.IsAuthorized(beforeUpdate);

            if (isAuthorized[AuthorizedAction.EditMatchLocation] && ModelState.IsValid)
            {
                var currentMember = Members.GetCurrentMember();
                var updatedMatchLocation = await _matchLocationRepository.UpdateMatchLocation(location, currentMember.Key, currentMember.Name).ConfigureAwait(false);
                _cacheOverride.OverrideCacheForCurrentMember(CacheConstants.MatchLocationsCacheKeyPrefix);

                return Redirect(updatedMatchLocation.MatchLocationRoute + "/edit");
            }

            var viewModel = new
                MatchLocationViewModel(CurrentPage, Services.UserService)
            {
                MatchLocation = location,
            };
            viewModel.IsAuthorized = isAuthorized;
            viewModel.Metadata.PageTitle = $"Edit {location.NameAndLocalityOrTown()}";

            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.MatchLocations, Url = new Uri(Constants.Pages.MatchLocationsUrl, UriKind.Relative) });
            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = viewModel.MatchLocation.NameAndLocalityOrTownIfDifferent(), Url = new Uri(viewModel.MatchLocation.MatchLocationRoute, UriKind.Relative) });

            return View("EditMatchLocation", viewModel);
        }
    }
}