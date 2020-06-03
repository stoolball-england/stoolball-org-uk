using Stoolball.Security;
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
    public class DeleteClubSurfaceController : SurfaceController
    {
        private readonly IClubDataSource _clubDataSource;
        private readonly IClubRepository _clubRepository;

        public DeleteClubSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, IClubDataSource clubDataSource, IClubRepository clubRepository)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _clubDataSource = clubDataSource;
            _clubRepository = clubRepository ?? throw new System.ArgumentNullException(nameof(clubRepository));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        public async Task<ActionResult> DeleteClub([Bind(Prefix = "ConfirmDeleteRequest", Include = "RequiredText,ConfirmationText")] MatchingTextConfirmation model)
        {
            if (model is null)
            {
                throw new System.ArgumentNullException(nameof(model));
            }

            var viewModel = new DeleteClubViewModel(CurrentPage)
            {
                Club = await _clubDataSource.ReadClubByRoute(Request.RawUrl).ConfigureAwait(false),
            };

            viewModel.IsAuthorized = Members.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.Editors, viewModel.Club.MemberGroupName }, null);

            if (viewModel.IsAuthorized && ModelState.IsValid)
            {
                Services.MemberGroupService.Delete(Services.MemberGroupService.GetById(viewModel.Club.MemberGroupId));

                var currentMember = Members.GetCurrentMember();
                await _clubRepository.DeleteClub(viewModel.Club, currentMember.Key, currentMember.Name).ConfigureAwait(false);
                viewModel.Deleted = true;
            }

            viewModel.Metadata.PageTitle = $"Delete {viewModel.Club.ClubName}";
            return View("DeleteClub", viewModel);
        }
    }
}