using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Competitions;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Stoolball.Web.Competitions
{
    public class EditCompetitionController : RenderMvcControllerAsync
    {
        private readonly ICompetitionDataSource _competitionDataSource;
        private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;

        public EditCompetitionController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ICompetitionDataSource competitionDataSource,
           IAuthorizationPolicy<Competition> authorizationPolicy)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _competitionDataSource = competitionDataSource ?? throw new System.ArgumentNullException(nameof(competitionDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new System.ArgumentNullException(nameof(authorizationPolicy));
        }

        [HttpGet]
        [ContentSecurityPolicy(TinyMCE = true, Forms = true)]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var model = new CompetitionViewModel(contentModel.Content, Services?.UserService)
            {
                Competition = await _competitionDataSource.ReadCompetitionByRoute(Request.RawUrl).ConfigureAwait(false),
                UrlReferrer = Request.UrlReferrer
            };


            if (model.Competition == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.IsAuthorized = _authorizationPolicy.IsAuthorized(model.Competition);

                model.Metadata.PageTitle = "Edit " + model.Competition.CompetitionName;

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Competition.CompetitionName, Url = new Uri(model.Competition.CompetitionRoute, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}