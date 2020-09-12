using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Clubs;
using Stoolball.Security;
using Stoolball.Umbraco.Data.Clubs;
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
        public async Task<ActionResult> UpdateClub([Bind(Prefix = "Club", Include = "ClubName,ClubMark,Teams")] Club club)
        {
            if (club is null)
            {
                throw new System.ArgumentNullException(nameof(club));
            }

            var beforeUpdate = await _clubDataSource.ReadClubByRoute(Request.RawUrl).ConfigureAwait(false);
            club.ClubId = beforeUpdate.ClubId;
            club.ClubRoute = beforeUpdate.ClubRoute;

            var isAuthorized = _authorizationPolicy.IsAuthorized(beforeUpdate);

            if (isAuthorized[AuthorizedAction.EditClub] && ModelState.IsValid)
            {
                var currentMember = Members.GetCurrentMember();
                await _clubRepository.UpdateClub(club, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                // redirect back to the club actions page that led here
                return Redirect(club.ClubRoute + "/edit");
            }

            var viewModel = new ClubViewModel(CurrentPage, Services.UserService)
            {
                Club = club,
            };
            viewModel.IsAuthorized = isAuthorized;
            viewModel.Metadata.PageTitle = $"Edit {club.ClubName}";
            return View("EditClub", viewModel);
        }
    }
}