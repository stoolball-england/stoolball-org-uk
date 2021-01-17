using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
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
    public class EditClubSurfaceController : SurfaceController
    {
        private readonly IClubDataSource _clubDataSource;
        private readonly IClubRepository _clubRepository;
        private readonly IAuthorizationPolicy<Club> _authorizationPolicy;

        public EditClubSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, IClubDataSource clubDataSource, IClubRepository clubRepository,
            IAuthorizationPolicy<Club> authorizationPolicy)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _clubDataSource = clubDataSource ?? throw new System.ArgumentNullException(nameof(clubDataSource));
            _clubRepository = clubRepository ?? throw new System.ArgumentNullException(nameof(clubRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new System.ArgumentNullException(nameof(authorizationPolicy));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<ActionResult> UpdateClub([Bind(Prefix = "Club", Include = "ClubName,Teams")] Club club)
        {
            if (club is null)
            {
                throw new System.ArgumentNullException(nameof(club));
            }

            var beforeUpdate = await _clubDataSource.ReadClubByRoute(Request.RawUrl).ConfigureAwait(false);
            club.ClubId = beforeUpdate.ClubId;
            club.ClubRoute = beforeUpdate.ClubRoute;

            // We're not interested in validating the details of the selected teams
            foreach (var key in ModelState.Keys.Where(x => x.StartsWith("Club.Teams", StringComparison.OrdinalIgnoreCase)))
            {
                ModelState[key].Errors.Clear();
            }

            var isAuthorized = _authorizationPolicy.IsAuthorized(beforeUpdate);

            if (isAuthorized[AuthorizedAction.EditClub] && ModelState.IsValid)
            {
                var currentMember = Members.GetCurrentMember();
                var updatedClub = await _clubRepository.UpdateClub(club, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                // redirect back to the club actions page that led here
                return Redirect(updatedClub.ClubRoute + "/edit");
            }

            var viewModel = new ClubViewModel(CurrentPage, Services.UserService)
            {
                Club = club,
            };
            viewModel.IsAuthorized = isAuthorized;
            viewModel.Metadata.PageTitle = $"Edit {club.ClubName}";

            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Teams, Url = new Uri(Constants.Pages.TeamsUrl, UriKind.Relative) });
            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = viewModel.Club.ClubName, Url = new Uri(viewModel.Club.ClubRoute, UriKind.Relative) });

            return View("EditClub", viewModel);
        }
    }
}