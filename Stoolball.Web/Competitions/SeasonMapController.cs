using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Competitions;
using Stoolball.Navigation;
using Stoolball.Web.Configuration;
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
    public class SeasonMapController : RenderMvcControllerAsync
    {
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly IApiKeyProvider _apiKeyProvider;

        public SeasonMapController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ISeasonDataSource seasonDataSource,
           IApiKeyProvider apiKeyProvider)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _apiKeyProvider = apiKeyProvider ?? throw new ArgumentNullException(nameof(apiKeyProvider));
        }

        [HttpGet]
        [ContentSecurityPolicy(GoogleMaps = true)]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var model = new SeasonViewModel(contentModel.Content, Services?.UserService)
            {
                Season = await _seasonDataSource.ReadSeasonByRoute(Request.RawUrl, false).ConfigureAwait(false),
                GoogleMapsApiKey = _apiKeyProvider.GetApiKey("GoogleMaps")
            };

            if (model.Season == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                var the = model.Season.Competition.CompetitionName.StartsWith("THE ", StringComparison.OrdinalIgnoreCase);
                model.Metadata.PageTitle = $"Map of teams in {(the ? string.Empty : "the ")}{model.Season.SeasonFullNameAndPlayerType()}";
                model.Metadata.Description = model.Season.Description();

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Season.Competition.CompetitionName, Url = new Uri(model.Season.Competition.CompetitionRoute, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}