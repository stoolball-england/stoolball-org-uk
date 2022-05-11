using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Competitions;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.Competitions.Models;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Competitions
{
    public class CreateCompetitionController : RenderController, IRenderControllerAsync
    {
        private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;

        public CreateCompetitionController(ILogger<CreateCompetitionController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IAuthorizationPolicy<Competition> authorizationPolicy)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
        }

        [HttpGet]
        [ContentSecurityPolicy(TinyMCE = true, Forms = true)]
        public async new Task<IActionResult> Index()
        {
            var model = new CompetitionViewModel(CurrentPage)
            {
                Competition = new Competition()
            };

            model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(model.Competition);

            model.Metadata.PageTitle = "Add a competition";

            model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });

            return CurrentTemplate(model);
        }
    }
}