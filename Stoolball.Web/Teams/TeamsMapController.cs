using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Web.Configuration;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Stoolball.Web.Teams.Models;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Teams
{
    public class TeamsMapController : RenderController, IRenderControllerAsync
    {
        private readonly IApiKeyProvider _apiKeyProvider;

        public TeamsMapController(ILogger<TeamsMapController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IApiKeyProvider apiKeyProvider)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _apiKeyProvider = apiKeyProvider ?? throw new ArgumentNullException(nameof(apiKeyProvider));
        }

        [HttpGet]
        [ContentSecurityPolicy(GoogleMaps = true)]
        public new Task<IActionResult> Index()
        {
            var model = new TeamsViewModel(CurrentPage)
            {
                GoogleMapsApiKey = _apiKeyProvider.GetApiKey("GoogleMaps")
            };

            model.Metadata.PageTitle = "Map of " + Constants.Pages.Teams.ToLower(CultureInfo.CurrentCulture);

            return Task.FromResult(CurrentTemplate(model));
        }
    }
}