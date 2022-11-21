using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
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
    public class DeleteSeasonSurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly ISeasonRepository _seasonRepository;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;
        private readonly IMatchListingCacheInvalidator _cacheClearer;

        public DeleteSeasonSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberManager memberManager,
            ISeasonDataSource seasonDataSource, ISeasonRepository seasonRepository, IMatchListingDataSource matchDataSource, IAuthorizationPolicy<Competition> authorizationPolicy,
            IMatchListingCacheInvalidator cacheClearer)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _cacheClearer = cacheClearer ?? throw new ArgumentNullException(nameof(cacheClearer));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<IActionResult> DeleteSeason([Bind("RequiredText", "ConfirmationText", Prefix = "ConfirmDeleteRequest")] MatchingTextConfirmation model)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var viewModel = new DeleteSeasonViewModel(CurrentPage, Services.UserService)
            {
                Season = await _seasonDataSource.ReadSeasonByRoute(Request.Path, true).ConfigureAwait(false),
            };
            viewModel.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(viewModel.Season.Competition);

            // Create a version without circular references before it gets serialised for audit
            viewModel.Season.Teams = viewModel.Season.Teams.Select(x => new TeamInSeason { Team = x.Team, WithdrawnDate = x.WithdrawnDate }).ToList();

            if (viewModel.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.DeleteCompetition] && ModelState.IsValid)
            {
                var currentMember = await _memberManager.GetCurrentMemberAsync();
                await _seasonRepository.DeleteSeason(viewModel.Season, currentMember.Key, currentMember.Name).ConfigureAwait(false);
                _cacheClearer.InvalidateCacheForSeason(viewModel.Season.SeasonId!.Value);
                viewModel.Deleted = true;
            }
            else
            {
                viewModel.TotalMatches = await _matchDataSource.ReadTotalMatches(new MatchFilter
                {
                    SeasonIds = new List<Guid> { viewModel.Season.SeasonId!.Value },
                    IncludeTournamentMatches = true
                }).ConfigureAwait(false);
            }

            viewModel.Metadata.PageTitle = $"Delete {viewModel.Season.SeasonFullNameAndPlayerType()}";

            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = viewModel.Season.Competition.CompetitionName, Url = new Uri(viewModel.Season.Competition.CompetitionRoute, UriKind.Relative) });
            if (!viewModel.Deleted)
            {
                viewModel.Breadcrumbs.Add(new Breadcrumb { Name = viewModel.Season.SeasonName(), Url = new Uri(viewModel.Season.SeasonRoute, UriKind.Relative) });
            }

            return View("DeleteSeason", viewModel);
        }
    }
}