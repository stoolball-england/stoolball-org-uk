using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.Competitions.Models;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Competitions
{
    public class CompetitionActionsController : RenderController, IRenderControllerAsync
    {
        private readonly ICompetitionDataSource _competitionDataSource;
        private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;

        public CompetitionActionsController(ILogger<CompetitionActionsController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            ICompetitionDataSource competitionDataSource,
            IAuthorizationPolicy<Competition> authorizationPolicy)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _competitionDataSource = competitionDataSource ?? throw new ArgumentNullException(nameof(competitionDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var model = new CompetitionViewModel(CurrentPage)
            {
                Competition = await _competitionDataSource.ReadCompetitionByRoute(Request.Path).ConfigureAwait(false)
            };

            if (model.Competition == null)
            {
                return NotFound();
            }
            else
            {
                model.Authorization.AuthorizationFor = model.Competition.CompetitionName;
                model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(model.Competition);
                model.Authorization.AuthorizedGroupNames = _authorizationPolicy.AuthorizedGroupNames(model.Competition);
                model.Authorization.AuthorizedMemberNames = await _authorizationPolicy.AuthorizedMemberNames(model.Competition);

                model.Metadata.PageTitle = "Edit " + model.Competition.CompetitionName;

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Competition.CompetitionName, Url = new Uri(model.Competition.CompetitionRoute, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}