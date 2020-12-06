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
    public class CreateCompetitionController : RenderMvcControllerAsync
    {
        private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;

        public CreateCompetitionController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IAuthorizationPolicy<Competition> authorizationPolicy)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _authorizationPolicy = authorizationPolicy;
        }

        [HttpGet]
        [ContentSecurityPolicy(TinyMCE = true, Forms = true)]
        public override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var model = new CompetitionViewModel(contentModel.Content, Services?.UserService)
            {
                Competition = new Competition()
            };

            model.IsAuthorized = _authorizationPolicy.IsAuthorized(model.Competition);

            model.Metadata.PageTitle = "Add a competition";

            model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });

            return Task.FromResult(CurrentTemplate(model));
        }
    }
}