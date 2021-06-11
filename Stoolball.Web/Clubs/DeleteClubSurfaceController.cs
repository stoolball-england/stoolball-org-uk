using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Caching;
using Stoolball.Clubs;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;

namespace Stoolball.Web.Clubs
{
    public class DeleteClubSurfaceController : SurfaceController
    {
        private readonly IClubDataSource _clubDataSource;
        private readonly IClubRepository _clubRepository;
        private readonly IAuthorizationPolicy<Club> _authorizationPolicy;
        private readonly ICacheOverride _cacheOverride;

        public DeleteClubSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, IClubDataSource clubDataSource, IClubRepository clubRepository,
            IAuthorizationPolicy<Club> authorizationPolicy, ICacheOverride cacheOverride)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _clubDataSource = clubDataSource ?? throw new System.ArgumentNullException(nameof(clubDataSource));
            _clubRepository = clubRepository ?? throw new System.ArgumentNullException(nameof(clubRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new System.ArgumentNullException(nameof(authorizationPolicy));
            _cacheOverride = cacheOverride ?? throw new ArgumentNullException(nameof(cacheOverride));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<ActionResult> DeleteClub([Bind(Prefix = "ConfirmDeleteRequest", Include = "RequiredText,ConfirmationText")] MatchingTextConfirmation model)
        {
            if (model is null)
            {
                throw new System.ArgumentNullException(nameof(model));
            }

            var viewModel = new DeleteClubViewModel(CurrentPage, Services.UserService)
            {
                Club = await _clubDataSource.ReadClubByRoute(Request.RawUrl).ConfigureAwait(false),
            };
            viewModel.IsAuthorized = _authorizationPolicy.IsAuthorized(viewModel.Club);

            if (viewModel.IsAuthorized[AuthorizedAction.DeleteClub] && ModelState.IsValid)
            {
                var memberGroup = Services.MemberGroupService.GetById(viewModel.Club.MemberGroupKey.Value);
                if (memberGroup != null)
                {
                    Services.MemberGroupService.Delete(memberGroup);
                }

                var currentMember = Members.GetCurrentMember();
                await _clubRepository.DeleteClub(viewModel.Club, currentMember.Key, currentMember.Name).ConfigureAwait(false);
                _cacheOverride.OverrideCacheForCurrentMember(CacheConstants.TeamListingsCacheKeyPrefix);
                viewModel.Deleted = true;
            }

            viewModel.Metadata.PageTitle = $"Delete {viewModel.Club.ClubName}";

            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Teams, Url = new Uri(Constants.Pages.TeamsUrl, UriKind.Relative) });
            if (!viewModel.Deleted)
            {
                viewModel.Breadcrumbs.Add(new Breadcrumb { Name = viewModel.Club.ClubName, Url = new Uri(viewModel.Club.ClubRoute, UriKind.Relative) });
            }

            return View("DeleteClub", viewModel);
        }
    }
}