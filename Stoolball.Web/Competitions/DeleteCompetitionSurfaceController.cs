using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Caching;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Competitions.Models;
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

namespace Stoolball.Web.Competitions
{
    public class DeleteCompetitionSurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly ICompetitionDataSource _competitionDataSource;
        private readonly ICompetitionRepository _competitionRepository;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly ITeamDataSource _teamDataSource;
        private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;
        private readonly IListingCacheClearer<Competition> _cacheClearer;

        public DeleteCompetitionSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberManager memberManager, ICompetitionDataSource competitionDataSource,
            ICompetitionRepository competitionRepository, IMatchListingDataSource matchDataSource, ITeamDataSource teamDataSource, IAuthorizationPolicy<Competition> authorizationPolicy,
            IListingCacheClearer<Competition> cacheClearer)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _competitionDataSource = competitionDataSource ?? throw new ArgumentNullException(nameof(competitionDataSource));
            _competitionRepository = competitionRepository ?? throw new ArgumentNullException(nameof(competitionRepository));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _cacheClearer = cacheClearer ?? throw new ArgumentNullException(nameof(cacheClearer));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<ActionResult> DeleteCompetition([Bind("RequiredText", "ConfirmationText", Prefix = "ConfirmDeleteRequest")] MatchingTextConfirmation model)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var viewModel = new DeleteCompetitionViewModel(CurrentPage, Services.UserService)
            {
                Competition = await _competitionDataSource.ReadCompetitionByRoute(Request.Path).ConfigureAwait(false),
            };
            viewModel.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(viewModel.Competition);

            if (viewModel.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.DeleteCompetition] && ModelState.IsValid)
            {
                var memberGroup = Services.MemberGroupService.GetById(viewModel.Competition.MemberGroupKey!.Value);
                if (memberGroup != null)
                {
                    Services.MemberGroupService.Delete(memberGroup);
                }

                var currentMember = await _memberManager.GetCurrentMemberAsync();
                await _competitionRepository.DeleteCompetition(viewModel.Competition, currentMember.Key, currentMember.Name).ConfigureAwait(false);
                _cacheClearer.ClearCache();
                viewModel.Deleted = true;
            }
            else
            {
                var competitionIds = new List<Guid> { viewModel.Competition.CompetitionId!.Value };
                viewModel.TotalMatches = await _matchDataSource.ReadTotalMatches(new MatchFilter
                {
                    CompetitionIds = competitionIds,
                    IncludeTournamentMatches = true
                }).ConfigureAwait(false);

                viewModel.TotalTeams = await _teamDataSource.ReadTotalTeams(new TeamFilter
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