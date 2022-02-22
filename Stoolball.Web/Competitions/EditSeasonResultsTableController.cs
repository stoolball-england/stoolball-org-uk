using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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
    public class EditSeasonResultsTableController : RenderController, IRenderControllerAsync
    {
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;

        public EditSeasonResultsTableController(ILogger<EditSeasonResultsTableController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            ISeasonDataSource seasonDataSource,
            IAuthorizationPolicy<Competition> authorizationPolicy)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _seasonDataSource = seasonDataSource ?? throw new System.ArgumentNullException(nameof(seasonDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new System.ArgumentNullException(nameof(authorizationPolicy));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true)]
        public async new Task<IActionResult> Index()
        {
            var model = new SeasonViewModel(CurrentPage)
            {
                Season = await _seasonDataSource.ReadSeasonByRoute(Request.Path, true).ConfigureAwait(false),
                UrlReferrer = Request.GetTypedHeaders().Referer
            };


            if (model.Season == null)
            {
                return NotFound();
            }
            else
            {
                model.Season.PointsRules.AddRange(await _seasonDataSource.ReadPointsRules(model.Season.SeasonId!.Value).ConfigureAwait(false));

                model.IsAuthorized = await _authorizationPolicy.IsAuthorized(model.Season.Competition);

                model.Metadata.PageTitle = "Edit results table for " + model.Season.SeasonFullNameAndPlayerType();

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Season.Competition.CompetitionName, Url = new Uri(model.Season.Competition.CompetitionRoute, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Season.SeasonName(), Url = new Uri(model.Season.SeasonRoute, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}