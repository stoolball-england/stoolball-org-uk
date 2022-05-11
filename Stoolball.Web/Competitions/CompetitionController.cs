using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Competitions;
using Stoolball.Email;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.Competitions.Models;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Competitions
{
    public class CompetitionController : RenderController, IRenderControllerAsync
    {
        private readonly ICompetitionDataSource _competitionDataSource;
        private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;
        private readonly IEmailProtector _emailProtector;

        public CompetitionController(ILogger<CompetitionController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            ICompetitionDataSource competitionDataSource,
            IAuthorizationPolicy<Competition> authorizationPolicy,
            IEmailProtector emailProtector)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _competitionDataSource = competitionDataSource ?? throw new ArgumentNullException(nameof(competitionDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _emailProtector = emailProtector ?? throw new ArgumentNullException(nameof(emailProtector));
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
                model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(model.Competition);

                model.Metadata.PageTitle = model.Competition.CompetitionName;
                model.Metadata.Description = model.Competition.Description();

                model.Competition.Introduction = _emailProtector.ProtectEmailAddresses(model.Competition.Introduction, User.Identity?.IsAuthenticated ?? false);
                model.Competition.PublicContactDetails = _emailProtector.ProtectEmailAddresses(model.Competition.PublicContactDetails, User.Identity?.IsAuthenticated ?? false);

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}