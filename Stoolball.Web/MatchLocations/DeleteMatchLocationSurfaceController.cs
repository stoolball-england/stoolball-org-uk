using Stoolball.Security;
using Stoolball.Umbraco.Data.Matches;
using Stoolball.Umbraco.Data.MatchLocations;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Web.MatchLocations
{
    public class DeleteMatchLocationSurfaceController : SurfaceController
    {
        private readonly IMatchLocationDataSource _matchLocationDataSource;
        private readonly IMatchLocationRepository _matchLocationRepository;
        private readonly IMatchDataSource _matchDataSource;

        public DeleteMatchLocationSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory,
            ServiceContext serviceContext, AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper,
            IMatchLocationDataSource matchLocationDataSource, IMatchLocationRepository matchLocationRepository, IMatchDataSource matchDataSource)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _matchLocationDataSource = matchLocationDataSource ?? throw new System.ArgumentNullException(nameof(matchLocationDataSource));
            _matchLocationRepository = matchLocationRepository ?? throw new System.ArgumentNullException(nameof(matchLocationRepository));
            _matchDataSource = matchDataSource;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        public async Task<ActionResult> DeleteMatchLocation([Bind(Prefix = "ConfirmDeleteRequest", Include = "RequiredText,ConfirmationText")] MatchingTextConfirmation model)
        {
            if (model is null)
            {
                throw new System.ArgumentNullException(nameof(model));
            }

            var viewModel = new DeleteMatchLocationViewModel(CurrentPage)
            {
                MatchLocation = await _matchLocationDataSource.ReadMatchLocationByRoute(Request.RawUrl, true).ConfigureAwait(false)
            };

            viewModel.IsAuthorized = Members.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.Editors, viewModel.MatchLocation.MemberGroupName }, null);

            if (viewModel.IsAuthorized && ModelState.IsValid)
            {
                Services.MemberGroupService.Delete(Services.MemberGroupService.GetById(viewModel.MatchLocation.MemberGroupId));

                var currentMember = Members.GetCurrentMember();
                await _matchLocationRepository.DeleteMatchLocation(viewModel.MatchLocation, currentMember.Key, currentMember.Name).ConfigureAwait(false);
                viewModel.Deleted = true;
            }
            else
            {
                viewModel.TotalMatches = await _matchDataSource.ReadTotalMatches(new MatchQuery { MatchLocationIds = new List<Guid> { viewModel.MatchLocation.MatchLocationId.Value } }).ConfigureAwait(false);
            }

            viewModel.Metadata.PageTitle = $"Delete " + viewModel.MatchLocation.NameAndLocalityOrTown();
            return View("DeleteMatchLocation", viewModel);
        }
    }
}