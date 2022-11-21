using System;
using System.Linq;
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
    public class EditClubSurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly IClubDataSource _clubDataSource;
        private readonly IClubRepository _clubRepository;
        private readonly IAuthorizationPolicy<Club> _authorizationPolicy;
        private readonly IListingCacheInvalidator<Team> _cacheClearer;

        public EditClubSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
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
        public async Task<IActionResult> UpdateClub([Bind("ClubName", "Teams", Prefix = "Club")] Club club)
        {
            if (club is null)
            {
                throw new ArgumentNullException(nameof(club));
            }

            var beforeUpdate = await _clubDataSource.ReadClubByRoute(Request.Path).ConfigureAwait(false);
            club.ClubId = beforeUpdate.ClubId;
            club.ClubRoute = beforeUpdate.ClubRoute;

            // We're not interested in validating the details of the selected teams
            foreach (var key in ModelState.Keys.Where(x => x.StartsWith("Club.Teams", StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.Remove(key);
            }

            var isAuthorized = await _authorizationPolicy.IsAuthorized(beforeUpdate);

            if (isAuthorized[AuthorizedAction.EditClub] && ModelState.IsValid)
            {
                var currentMember = await _memberManager.GetCurrentMemberAsync();
                var updatedClub = await _clubRepository.UpdateClub(club, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                _cacheClearer.InvalidateCache();

                // redirect back to the club actions page that led here
                return Redirect(updatedClub.ClubRoute + "/edit");
            }

            var viewModel = new ClubViewModel(CurrentPage, Services.UserService)
            {
                Club = club,
            };
            viewModel.Authorization.CurrentMemberIsAuthorized = isAuthorized;
            viewModel.Metadata.PageTitle = $"Edit {club.ClubName}";

            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Teams, Url = new Uri(Constants.Pages.TeamsUrl, UriKind.Relative) });
            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = viewModel.Club.ClubName, Url = new Uri(viewModel.Club.ClubRoute, UriKind.Relative) });

            return View("EditClub", viewModel);
        }
    }
}