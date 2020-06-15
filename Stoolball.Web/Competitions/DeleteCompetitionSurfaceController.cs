using Stoolball.Security;
using Stoolball.Umbraco.Data.Competitions;
using Stoolball.Umbraco.Data.Matches;
using Stoolball.Umbraco.Data.Teams;
using Stoolball.Web.Security;
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

namespace Stoolball.Web.Competitions
{
    public class DeleteCompetitionSurfaceController : SurfaceController
    {
        private readonly ICompetitionDataSource _competitionDataSource;
        private readonly ICompetitionRepository _competitionRepository;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly ITeamDataSource _teamDataSource;

        public DeleteCompetitionSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, ICompetitionDataSource competitionDataSource, ICompetitionRepository competitionRepository,
            IMatchListingDataSource matchDataSource, ITeamDataSource teamDataSource)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _competitionDataSource = competitionDataSource;
            _competitionRepository = competitionRepository ?? throw new System.ArgumentNullException(nameof(competitionRepository));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<ActionResult> DeleteCompetition([Bind(Prefix = "ConfirmDeleteRequest", Include = "RequiredText,ConfirmationText")] MatchingTextConfirmation model)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var viewModel = new DeleteCompetitionViewModel(CurrentPage)
            {
                Competition = await _competitionDataSource.ReadCompetitionByRoute(Request.RawUrl).ConfigureAwait(false),
                IsAuthorized = Members.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.Editors }, null)
            };


            if (viewModel.IsAuthorized && ModelState.IsValid)
            {
                Services.MemberGroupService.Delete(Services.MemberGroupService.GetById(viewModel.Competition.MemberGroupId));

                var currentMember = Members.GetCurrentMember();
                await _competitionRepository.DeleteCompetition(viewModel.Competition, currentMember.Key, currentMember.Name).ConfigureAwait(false);
                viewModel.Deleted = true;
            }
            else
            {
                var competitionIds = new List<Guid> { viewModel.Competition.CompetitionId.Value };
                viewModel.TotalMatches = await _matchDataSource.ReadTotalMatches(new MatchQuery
                {
                    CompetitionIds = competitionIds,
                    IncludeTournamentMatches = true
                }).ConfigureAwait(false);

                viewModel.TotalTeams = await _teamDataSource.ReadTotalTeams(new TeamQuery
                {
                    CompetitionIds = competitionIds
                }).ConfigureAwait(false);
            }

            viewModel.Metadata.PageTitle = $"Delete {viewModel.Competition.CompetitionName}";
            return View("DeleteCompetition", viewModel);
        }
    }
}