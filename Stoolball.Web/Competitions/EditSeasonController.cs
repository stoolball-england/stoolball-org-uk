using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.Competitions.Models;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Competitions
{
    public class EditSeasonController : RenderController, IRenderControllerAsync
    {
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;

        public EditSeasonController(ILogger<EditSeasonController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            ISeasonDataSource seasonDataSource,
            IAuthorizationPolicy<Competition> authorizationPolicy)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
        }

        [HttpGet]
        [ContentSecurityPolicy(TinyMCE = true, Forms = true)]
        public async new Task<IActionResult> Index()
        {
            var model = new SeasonViewModel(CurrentPage)
            {
                Season = await _seasonDataSource.ReadSeasonByRoute(Request.Path, true).ConfigureAwait(false),
            };


            if (model.Season == null)
            {
                return NotFound();
            }
            else
            {
                model.IsAuthorized = await _authorizationPolicy.IsAuthorized(model.Season.Competition);

                if (!model.Season.DefaultOverSets.Any())
                {
                    model.Season.DefaultOverSets.Add(new OverSet());
                }

                model.Metadata.PageTitle = "Edit " + model.Season.SeasonFullNameAndPlayerType();

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Season.Competition.CompetitionName, Url = new Uri(model.Season.Competition.CompetitionRoute, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Season.SeasonName(), Url = new Uri(model.Season.SeasonRoute, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}