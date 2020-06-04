using Stoolball.Competitions;
using Stoolball.Security;
using Stoolball.Umbraco.Data.Competitions;
using Stoolball.Umbraco.Data.Matches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Web.Competitions
{
    public class DeleteSeasonSurfaceController : SurfaceController
    {
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly ISeasonRepository _seasonRepository;
        private readonly IMatchDataSource _matchDataSource;

        public DeleteSeasonSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, ISeasonDataSource seasonDataSource, ISeasonRepository seasonRepository,
            IMatchDataSource matchDataSource)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _seasonDataSource = seasonDataSource;
            _seasonRepository = seasonRepository ?? throw new System.ArgumentNullException(nameof(seasonRepository));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        public async Task<ActionResult> DeleteSeason([Bind(Prefix = "ConfirmDeleteRequest", Include = "RequiredText,ConfirmationText")] MatchingTextConfirmation model)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var viewModel = new DeleteSeasonViewModel(CurrentPage)
            {
                Season = await _seasonDataSource.ReadSeasonByRoute(Request.RawUrl, true).ConfigureAwait(false),
            };

            // Create a version without circular references before it gets serialised for audit
            viewModel.Season.Teams = viewModel.Season.Teams.Select(x => new TeamInSeason { Team = x.Team, WithdrawnDate = x.WithdrawnDate }).ToList();

            viewModel.IsAuthorized = Members.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.Editors, viewModel.Season.Competition.MemberGroupName }, null);

            if (viewModel.IsAuthorized && ModelState.IsValid)
            {
                var currentMember = Members.GetCurrentMember();
                await _seasonRepository.DeleteSeason(viewModel.Season, currentMember.Key, currentMember.Name).ConfigureAwait(false);
                viewModel.Deleted = true;
            }
            else
            {
                viewModel.TotalMatches = await _matchDataSource.ReadTotalMatches(new MatchQuery
                {
                    SeasonIds = new List<Guid> { viewModel.Season.SeasonId.Value }
                }).ConfigureAwait(false);
            }

            viewModel.Metadata.PageTitle = $"Delete {viewModel.Season.SeasonFullNameAndPlayerType()}";
            return View("DeleteSeason", viewModel);
        }
    }
}