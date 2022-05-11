using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Competitions;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.Competitions.Models;
using Stoolball.Web.Routing;
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
    public class EditSeasonResultsTableSurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly ISeasonRepository _seasonRepository;
        private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;
        private readonly IPostSaveRedirector _postSaveRedirector;

        public EditSeasonResultsTableSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberManager memberManager, ISeasonDataSource seasonDataSource,
            ISeasonRepository seasonRepository, IAuthorizationPolicy<Competition> authorizationPolicy, IPostSaveRedirector postSaveRedirector)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _postSaveRedirector = postSaveRedirector ?? throw new ArgumentNullException(nameof(postSaveRedirector));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<ActionResult> UpdateSeason([Bind("PointsRules", "ResultsTableType", "EnableRunsScored", "EnableRunsConceded", Prefix = "Season")] Season season)
        {
            if (season is null)
            {
                throw new ArgumentNullException(nameof(season));
            }

            var beforeUpdate = await _seasonDataSource.ReadSeasonByRoute(Request.Path).ConfigureAwait(false);
            season.SeasonId = beforeUpdate.SeasonId;

            var isAuthorized = await _authorizationPolicy.IsAuthorized(beforeUpdate.Competition);

            if (isAuthorized[AuthorizedAction.EditCompetition] && ModelState.IsValid)
            {
                // Update the season
                var currentMember = await _memberManager.GetCurrentMemberAsync();
                await _seasonRepository.UpdateResultsTable(season, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                return _postSaveRedirector.WorkOutRedirect(beforeUpdate.SeasonRoute, beforeUpdate.SeasonRoute, "/edit", Request.Form["UrlReferrer"], null);
            }

            var viewModel = new SeasonViewModel(CurrentPage, Services.UserService)
            {
                Season = season,
                UrlReferrer = string.IsNullOrEmpty(Request.Form["UrlReferrer"]) ? null : new Uri(Request.Form["UrlReferrer"])
            };
            viewModel.Authorization.CurrentMemberIsAuthorized = isAuthorized;
            season.Competition = beforeUpdate.Competition;
            season.FromYear = beforeUpdate.FromYear;
            season.UntilYear = beforeUpdate.UntilYear;
            season.SeasonRoute = beforeUpdate.SeasonRoute;

            viewModel.Metadata.PageTitle = $"Edit results table for {beforeUpdate.SeasonFullNameAndPlayerType()}";

            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = viewModel.Season.Competition.CompetitionName, Url = new Uri(viewModel.Season.Competition.CompetitionRoute, UriKind.Relative) });
            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = viewModel.Season.SeasonName(), Url = new Uri(viewModel.Season.SeasonRoute, UriKind.Relative) });

            return View("EditSeasonResultsTable", viewModel);
        }
    }
}