using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Security;
using Stoolball.Web.MatchLocations.Models;
using Stoolball.Web.Navigation;
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
    public class DeleteMatchLocationSurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly IMatchLocationDataSource _matchLocationDataSource;
        private readonly IMatchLocationRepository _matchLocationRepository;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IAuthorizationPolicy<MatchLocation> _authorizationPolicy;
        private readonly IListingCacheInvalidator<MatchLocation> _matchLocationCacheClearer;
        private readonly IMatchListingCacheInvalidator _matchListingCacheClearer;

        public DeleteMatchLocationSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory,
            ServiceContext serviceContext, AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberManager memberManager,
            IMatchLocationDataSource matchLocationDataSource, IMatchLocationRepository matchLocationRepository, IMatchListingDataSource matchDataSource,
            IAuthorizationPolicy<MatchLocation> authorizationPolicy, IListingCacheInvalidator<MatchLocation> matchLocationCacheClearer, IMatchListingCacheInvalidator matchListingCacheClearer)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _matchLocationDataSource = matchLocationDataSource ?? throw new ArgumentNullException(nameof(matchLocationDataSource));
            _matchLocationRepository = matchLocationRepository ?? throw new ArgumentNullException(nameof(matchLocationRepository));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _matchLocationCacheClearer = matchLocationCacheClearer ?? throw new ArgumentNullException(nameof(matchLocationCacheClearer));
            _matchListingCacheClearer = matchListingCacheClearer ?? throw new ArgumentNullException(nameof(matchListingCacheClearer));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<IActionResult> DeleteMatchLocation([Bind("RequiredText", "ConfirmationText", Prefix = "ConfirmDeleteRequest")] MatchingTextConfirmation model)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var viewModel = new DeleteMatchLocationViewModel(CurrentPage, Services.UserService)
            {
                MatchLocation = await _matchLocationDataSource.ReadMatchLocationByRoute(Request.Path, true).ConfigureAwait(false)
            };
            viewModel.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(viewModel.MatchLocation);

            if (viewModel.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.DeleteMatchLocation] && ModelState.IsValid)
            {
                var memberGroup = Services.MemberGroupService.GetById(viewModel.MatchLocation.MemberGroupKey!.Value);
                if (memberGroup != null)
                {
                    Services.MemberGroupService.Delete(memberGroup);
                }

                var currentMember = await _memberManager.GetCurrentMemberAsync();
                await _matchLocationRepository.DeleteMatchLocation(viewModel.MatchLocation, currentMember.Key, currentMember.Name).ConfigureAwait(false);
                _matchLocationCacheClearer.InvalidateCache();
                _matchListingCacheClearer.InvalidateCacheForMatchLocation(viewModel.MatchLocation.MatchLocationId!.Value);
                viewModel.Deleted = true;
            }
            else
            {
                viewModel.TotalMatches = await _matchDataSource.ReadTotalMatches(new MatchFilter
                {
                    MatchLocationIds = new List<Guid> { viewModel.MatchLocation.MatchLocationId!.Value },
                    IncludeTournamentMatches = true
                }).ConfigureAwait(false);
            }

            viewModel.Metadata.PageTitle = $"Delete " + viewModel.MatchLocation.NameAndLocalityOrTown();

            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.MatchLocations, Url = new Uri(Constants.Pages.MatchLocationsUrl, UriKind.Relative) });
            if (!viewModel.Deleted)
            {
                viewModel.Breadcrumbs.Add(new Breadcrumb { Name = viewModel.MatchLocation.NameAndLocalityOrTownIfDifferent(), Url = new Uri(viewModel.MatchLocation.MatchLocationRoute, UriKind.Relative) });
            }

            return View("DeleteMatchLocation", viewModel);
        }
    }
}