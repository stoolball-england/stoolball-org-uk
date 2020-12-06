using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;

namespace Stoolball.Web.Competitions
{
    public class DeleteCompetitionSurfaceController : SurfaceController
    {
        private readonly ICompetitionDataSource _competitionDataSource;
        private readonly ICompetitionRepository _competitionRepository;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly ITeamDataSource _teamDataSource;
        private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;

        public DeleteCompetitionSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, ICompetitionDataSource competitionDataSource,
            ICompetitionRepository competitionRepository, IMatchListingDataSource matchDataSource, ITeamDataSource teamDataSource, IAuthorizationPolicy<Competition> authorizationPolicy)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _competitionDataSource = competitionDataSource ?? throw new ArgumentNullException(nameof(competitionDataSource));
            _competitionRepository = competitionRepository ?? throw new ArgumentNullException(nameof(competitionRepository));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
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

            var viewModel = new DeleteCompetitionViewModel(CurrentPage, Services.UserService)
            {
                Competition = await _competitionDataSource.ReadCompetitionByRoute(Request.RawUrl).ConfigureAwait(false),
            };
            viewModel.IsAuthorized = _authorizationPolicy.IsAuthorized(viewModel.Competition);

            if (viewModel.IsAuthorized[AuthorizedAction.DeleteCompetition] && ModelState.IsValid)
            {
                Services.MemberGroupService.Delete(Services.MemberGroupService.GetById(viewModel.Competition.MemberGroupKey.Value));

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

            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
            if (!viewModel.Deleted)
            {
                viewModel.Breadcrumbs.Add(new Breadcrumb { Name = viewModel.Competition.CompetitionName, Url = new Uri(viewModel.Competition.CompetitionRoute, UriKind.Relative) });
            }

            return View("DeleteCompetition", viewModel);
        }
    }
}