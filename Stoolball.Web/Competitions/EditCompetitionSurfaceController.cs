﻿using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Caching;
using Stoolball.Competitions;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;

namespace Stoolball.Web.Competitions
{
    public class EditCompetitionSurfaceController : SurfaceController
    {
        private readonly ICompetitionDataSource _competitionDataSource;
        private readonly ICompetitionRepository _competitionRepository;
        private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;
        private readonly IPostSaveRedirector _postSaveRedirector;
        private readonly ICacheOverride _cacheOverride;

        public EditCompetitionSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, ICompetitionDataSource competitionDataSource,
            ICompetitionRepository competitionRepository, IAuthorizationPolicy<Competition> authorizationPolicy, IPostSaveRedirector postSaveRedirector, ICacheOverride cacheOverride)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _competitionDataSource = competitionDataSource ?? throw new ArgumentNullException(nameof(competitionDataSource));
            _competitionRepository = competitionRepository ?? throw new ArgumentNullException(nameof(competitionRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _postSaveRedirector = postSaveRedirector ?? throw new ArgumentNullException(nameof(postSaveRedirector));
            _cacheOverride = cacheOverride ?? throw new ArgumentNullException(nameof(cacheOverride));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(TinyMCE = true, Forms = true)]
        public async Task<ActionResult> UpdateCompetition([Bind(Prefix = "Competition", Include = "CompetitionName,UntilYear,PlayerType,Facebook,Twitter,Instagram,YouTube,Website")] Competition competition)
        {
            if (competition is null)
            {
                throw new ArgumentNullException(nameof(competition));
            }

            var beforeUpdate = await _competitionDataSource.ReadCompetitionByRoute(Request.RawUrl).ConfigureAwait(false);
            competition.CompetitionId = beforeUpdate.CompetitionId;
            competition.CompetitionRoute = beforeUpdate.CompetitionRoute;

            // get this from the unvalidated form instead of via modelbinding so that HTML can be allowed
            competition.Introduction = Request.Unvalidated.Form["Competition.Introduction"];
            competition.PublicContactDetails = Request.Unvalidated.Form["Competition.PublicContactDetails"];
            competition.PrivateContactDetails = Request.Unvalidated.Form["Competition.PrivateContactDetails"];

            var isAuthorized = _authorizationPolicy.IsAuthorized(beforeUpdate);

            if (isAuthorized[AuthorizedAction.EditCompetition] && ModelState.IsValid)
            {
                var currentMember = Members.GetCurrentMember();
                var updatedCompetition = await _competitionRepository.UpdateCompetition(competition, currentMember.Key, currentMember.Name).ConfigureAwait(false);
                _cacheOverride.OverrideCacheForCurrentMember(CacheConstants.CompetitionsPolicyCacheKeyPrefix);

                return _postSaveRedirector.WorkOutRedirect(competition.CompetitionRoute, updatedCompetition.CompetitionRoute, "/edit", Request.Form["UrlReferrer"], null);
            }

            var viewModel = new CompetitionViewModel(CurrentPage, Services.UserService)
            {
                Competition = competition,
                UrlReferrer = string.IsNullOrEmpty(Request.Form["UrlReferrer"]) ? null : new Uri(Request.Form["UrlReferrer"])
            };
            viewModel.IsAuthorized = isAuthorized;
            viewModel.Metadata.PageTitle = $"Edit {competition.CompetitionName}";

            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = viewModel.Competition.CompetitionName, Url = new Uri(viewModel.Competition.CompetitionRoute, UriKind.Relative) });

            return View("EditCompetition", viewModel);
        }
    }
}