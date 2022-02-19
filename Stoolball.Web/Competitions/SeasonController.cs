using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Competitions;
using Stoolball.Email;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Competitions
{
    public class SeasonController : RenderController, IRenderControllerAsync
    {
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;
        private readonly IEmailProtector _emailProtector;

        public SeasonController(ILogger<SeasonController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            ISeasonDataSource seasonDataSource,
            IAuthorizationPolicy<Competition> authorizationPolicy,
            IEmailProtector emailProtector)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _seasonDataSource = seasonDataSource ?? throw new System.ArgumentNullException(nameof(seasonDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new System.ArgumentNullException(nameof(authorizationPolicy));
            _emailProtector = emailProtector ?? throw new System.ArgumentNullException(nameof(emailProtector));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var model = new SeasonViewModel(CurrentPage)
            {
                Season = await _seasonDataSource.ReadSeasonByRoute(Request.Path, true).ConfigureAwait(false)
            };

            if (model.Season == null)
            {
                return NotFound();
            }
            else
            {
                model.IsAuthorized = await _authorizationPolicy.IsAuthorized(model.Season.Competition);

                model.Metadata.PageTitle = model.Season.SeasonFullNameAndPlayerType();
                model.Metadata.Description = model.Season.Description();

                model.Season.Competition.Introduction = _emailProtector.ProtectEmailAddresses(model.Season.Competition.Introduction, User.Identity.IsAuthenticated);
                model.Season.Competition.PublicContactDetails = _emailProtector.ProtectEmailAddresses(model.Season.Competition.PublicContactDetails, User.Identity.IsAuthenticated);

                model.Season.Introduction = _emailProtector.ProtectEmailAddresses(model.Season.Introduction, User.Identity.IsAuthenticated);

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Season.Competition.CompetitionName, Url = new Uri(model.Season.Competition.CompetitionRoute, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}