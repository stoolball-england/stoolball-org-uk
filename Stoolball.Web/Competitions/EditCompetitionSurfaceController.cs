using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Caching;
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
    public class EditCompetitionSurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly ICompetitionDataSource _competitionDataSource;
        private readonly ICompetitionRepository _competitionRepository;
        private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;
        private readonly IPostSaveRedirector _postSaveRedirector;
        private readonly IListingCacheClearer<Competition> _cacheClearer;

        public EditCompetitionSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberManager memberManager, ICompetitionDataSource competitionDataSource,
            ICompetitionRepository competitionRepository, IAuthorizationPolicy<Competition> authorizationPolicy, IPostSaveRedirector postSaveRedirector, IListingCacheClearer<Competition> cacheClearer)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _competitionDataSource = competitionDataSource ?? throw new ArgumentNullException(nameof(competitionDataSource));
            _competitionRepository = competitionRepository ?? throw new ArgumentNullException(nameof(competitionRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _postSaveRedirector = postSaveRedirector ?? throw new ArgumentNullException(nameof(postSaveRedirector));
            _cacheClearer = cacheClearer ?? throw new ArgumentNullException(nameof(cacheClearer));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(TinyMCE = true, Forms = true)]
        public async Task<ActionResult> UpdateCompetition([Bind("CompetitionName", "UntilYear", "PlayerType", "Facebook", "Twitter", "Instagram", "YouTube", "Website", Prefix = "Competition")] Competition competition)
        {
            if (competition is null)
            {
                throw new ArgumentNullException(nameof(competition));
            }

            var beforeUpdate = await _competitionDataSource.ReadCompetitionByRoute(Request.Path).ConfigureAwait(false);
            competition.CompetitionId = beforeUpdate.CompetitionId;
            competition.CompetitionRoute = beforeUpdate.CompetitionRoute;

            // get this from the form instead of via modelbinding so that HTML can be allowed
            competition.Introduction = Request.Form["Competition.Introduction"];
            competition.PublicContactDetails = Request.Form["Competition.PublicContactDetails"];
            competition.PrivateContactDetails = Request.Form["Competition.PrivateContactDetails"];

            var isAuthorized = await _authorizationPolicy.IsAuthorized(beforeUpdate);

            if (isAuthorized[AuthorizedAction.EditCompetition] && ModelState.IsValid)
            {
                var currentMember = await _memberManager.GetCurrentMemberAsync();
                var updatedCompetition = await _competitionRepository.UpdateCompetition(competition, currentMember.Key, currentMember.Name).ConfigureAwait(false);
                _cacheClearer.ClearCache();

                return _postSaveRedirector.WorkOutRedirect(competition.CompetitionRoute, updatedCompetition.CompetitionRoute, "/edit", Request.Form["UrlReferrer"]);
            }

            var viewModel = new CompetitionViewModel(CurrentPage, Services.UserService)
            {
                Competition = competition,
                UrlReferrer = string.IsNullOrEmpty(Request.Form["UrlReferrer"]) ? null : new Uri(Request.Form["UrlReferrer"])
            };
            viewModel.Authorization.CurrentMemberIsAuthorized = isAuthorized;
            viewModel.Metadata.PageTitle = $"Edit {competition.CompetitionName}";

            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = viewModel.Competition.CompetitionName, Url = new Uri(viewModel.Competition.CompetitionRoute, UriKind.Relative) });

            return View("EditCompetition", viewModel);
        }
    }
}