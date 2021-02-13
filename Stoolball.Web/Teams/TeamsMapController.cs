using System.Globalization;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Web.Configuration;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Stoolball.Web.Teams
{
    public class TeamsMapController : RenderMvcControllerAsync
    {
        private readonly IApiKeyProvider _apiKeyProvider;

        public TeamsMapController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IApiKeyProvider apiKeyProvider)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _apiKeyProvider = apiKeyProvider;
        }

        [HttpGet]
        [ContentSecurityPolicy(GoogleMaps = true)]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var model = new TeamsViewModel(contentModel.Content, Services?.UserService)
            {
                GoogleMapsApiKey = _apiKeyProvider.GetApiKey("GoogleMaps")
            };

            model.Metadata.PageTitle = "Map of " + Constants.Pages.Teams.ToLower(CultureInfo.CurrentCulture);

            return CurrentTemplate(model);
        }
    }
}