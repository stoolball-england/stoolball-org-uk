using Stoolball.Clubs;
using Stoolball.Umbraco.Data.Clubs;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Web.Clubs
{
    public class EditClubSurfaceController : SurfaceController
    {
        private readonly IClubDataSource _clubDataSource;
        private readonly IClubRepository _clubRepository;

        public EditClubSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, IClubDataSource clubDataSource, IClubRepository clubRepository)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _clubDataSource = clubDataSource;
            _clubRepository = clubRepository ?? throw new System.ArgumentNullException(nameof(clubRepository));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        public async Task<ActionResult> UpdateClub([Bind(Prefix = "Club", Include = "ClubName,ClubMark,Facebook,Twitter,Instagram,YouTube,Website,Teams")]Club club)
        {
            if (club is null)
            {
                throw new System.ArgumentNullException(nameof(club));
            }

            var beforeUpdate = await _clubDataSource.ReadClubByRoute(Request.RawUrl).ConfigureAwait(false);
            club.ClubId = beforeUpdate.ClubId;
            club.ClubRoute = beforeUpdate.ClubRoute;

            var isAuthorized = Members.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.Editors, beforeUpdate.MemberGroupName }, null);

            if (isAuthorized && ModelState.IsValid)
            {
                await _clubRepository.UpdateClub(club).ConfigureAwait(false);

                // redirect back to the club
                return Redirect(club.ClubRoute);
            }

            var viewModel = new ClubViewModel(CurrentPage)
            {
                Club = club,
                IsAuthorized = isAuthorized
            };
            viewModel.Metadata.PageTitle = $"Edit {club.ClubName}";
            return View("EditClub", viewModel);
        }
    }
}