using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Clubs;
using Stoolball.Data.Abstractions;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Teams;
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

namespace Stoolball.Web.Clubs
{
    public class DeleteClubSurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly IClubDataSource _clubDataSource;
        private readonly IClubRepository _clubRepository;
        private readonly IAuthorizationPolicy<Club> _authorizationPolicy;
        private readonly IListingCacheInvalidator<Team> _cacheClearer;

        public DeleteClubSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberManager memberManager,
            IClubDataSource clubDataSource, IClubRepository clubRepository, IAuthorizationPolicy<Club> authorizationPolicy,
            IListingCacheInvalidator<Team> cacheClearer)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _clubDataSource = clubDataSource ?? throw new ArgumentNullException(nameof(clubDataSource));
            _clubRepository = clubRepository ?? throw new ArgumentNullException(nameof(clubRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _cacheClearer = cacheClearer ?? throw new ArgumentNullException(nameof(cacheClearer));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<IActionResult> DeleteClub([Bind("RequiredText", "ConfirmationText", Prefix = "ConfirmDeleteRequest")] MatchingTextConfirmation model)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var viewModel = new DeleteClubViewModel(CurrentPage, Services.UserService)
            {
                Club = await _clubDataSource.ReadClubByRoute(Request.Path).ConfigureAwait(false),
            };
            viewModel.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(viewModel.Club);

            if (viewModel.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.DeleteClub] && ModelState.IsValid)
            {
                var memberGroup = Services.MemberGroupService.GetById(viewModel.Club.MemberGroupKey!.Value);
                if (memberGroup != null)
                {
                    Services.MemberGroupService.Delete(memberGroup);
                }

                var currentMember = await _memberManager.GetCurrentMemberAsync();
                await _clubRepository.DeleteClub(viewModel.Club, currentMember.Key, currentMember.Name).ConfigureAwait(false);
                _cacheClearer.InvalidateCache();
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