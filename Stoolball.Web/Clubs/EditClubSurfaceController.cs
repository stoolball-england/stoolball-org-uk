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
        public async Task<ActionResult> Edit([Bind(Prefix = "Club", Include = "ClubName,ClubMark,Facebook,Twitter,Instagram,YouTube,Website,Teams")]Club club)
        {
            if (club is null)
            {
                throw new System.ArgumentNullException(nameof(club));
            }

            var beforeUpdate = await _clubDataSource.ReadClubByRoute(Request.Url.AbsolutePath).ConfigureAwait(false);
            club.ClubId = beforeUpdate.ClubId;

            var allowedGroup = Services.MemberGroupService.GetById(beforeUpdate.MemberGroupId);
            var isAuthorized = Members.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.Editors, allowedGroup.Name }, null);

            if (isAuthorized && ModelState.IsValid)
            {
                await _clubRepository.UpdateClub(club).ConfigureAwait(false);

                // redirect back to the club
                //  return Redirect(Request.RawUrl.Substring(0, Request.RawUrl.Length - "/edit".Length));
            }

            //ModelState.AddModelError("Club.ClubName", $"{club.ClubName} is not valid");
            //ModelState.AddModelError(string.Empty, $"The whole form is not valid");

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