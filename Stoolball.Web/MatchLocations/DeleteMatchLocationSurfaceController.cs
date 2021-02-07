using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Matches;
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
    public class DeleteMatchLocationSurfaceController : SurfaceController
    {
        private readonly IMatchLocationDataSource _matchLocationDataSource;
        private readonly IMatchLocationRepository _matchLocationRepository;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IAuthorizationPolicy<MatchLocation> _authorizationPolicy;

        public DeleteMatchLocationSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory,
            ServiceContext serviceContext, AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper,
            IMatchLocationDataSource matchLocationDataSource, IMatchLocationRepository matchLocationRepository, IMatchListingDataSource matchDataSource,
           IAuthorizationPolicy<MatchLocation> authorizationPolicy)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _matchLocationDataSource = matchLocationDataSource ?? throw new System.ArgumentNullException(nameof(matchLocationDataSource));
            _matchLocationRepository = matchLocationRepository ?? throw new System.ArgumentNullException(nameof(matchLocationRepository));
            _matchDataSource = matchDataSource;
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<ActionResult> DeleteMatchLocation([Bind(Prefix = "ConfirmDeleteRequest", Include = "RequiredText,ConfirmationText")] MatchingTextConfirmation model)
        {
            if (model is null)
            {
                throw new System.ArgumentNullException(nameof(model));
            }

            var viewModel = new DeleteMatchLocationViewModel(CurrentPage, Services.UserService)
            {
                MatchLocation = await _matchLocationDataSource.ReadMatchLocationByRoute(Request.RawUrl, true).ConfigureAwait(false),
            };
            viewModel.IsAuthorized = _authorizationPolicy.IsAuthorized(viewModel.MatchLocation);

            if (viewModel.IsAuthorized[AuthorizedAction.DeleteMatchLocation] && ModelState.IsValid)
            {
                var memberGroup = Services.MemberGroupService.GetById(viewModel.MatchLocation.MemberGroupKey.Value);
                if (memberGroup != null)
                {
                    Services.MemberGroupService.Delete(memberGroup);
                }

                var currentMember = Members.GetCurrentMember();
                await _matchLocationRepository.DeleteMatchLocation(viewModel.MatchLocation, currentMember.Key, currentMember.Name).ConfigureAwait(false);
                viewModel.Deleted = true;
            }
            else
            {
                viewModel.TotalMatches = await _matchDataSource.ReadTotalMatches(new MatchQuery
                {
                    MatchLocationIds = new List<Guid> { viewModel.MatchLocation.MatchLocationId.Value },
                    IncludeTournamentMatches = true
                }).ConfigureAwait(false);
            }

            viewModel.Metadata.PageTitle = $"Delete " + viewModel.MatchLocation.NameAndLocalityOrTown();

            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.MatchLocations, Url = new Uri(Constants.Pages.MatchLocationsUrl, UriKind.Relative) });
            if (!viewModel.Deleted)
            {
                viewModel.Breadcrumbs.Add(new Breadcrumb { Name = viewModel.MatchLocation.NameAndLocalityOrTown(), Url = new Uri(viewModel.MatchLocation.MatchLocationRoute, UriKind.Relative) });
            }

            return View("DeleteMatchLocation", viewModel);
        }
    }
}