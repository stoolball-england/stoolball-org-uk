using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Metadata;
using Stoolball.Web.Configuration;
using Stoolball.Web.Models;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Pages
{
    public class StyleGuideController : RenderController
    {
        private readonly IApiKeyProvider _apiKeyProvider;

        private readonly IPublishedValueFallback _publishedValueFallback;

        public StyleGuideController(ILogger<StyleGuideController> logger, ICompositeViewEngine compositeViewEngine, IUmbracoContextAccessor umbracoContextAccessor, IPublishedValueFallback publishedValueFallback,
            IApiKeyProvider apiKeyProvider)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _publishedValueFallback = publishedValueFallback ?? throw new System.ArgumentNullException(nameof(publishedValueFallback));
            _apiKeyProvider = apiKeyProvider ?? throw new System.ArgumentNullException(nameof(apiKeyProvider));
        }

        [HttpGet]
        [ContentSecurityPolicy(GoogleMaps = true, TinyMCE = true, Forms = true, GettyImages = true, YouTube = true)]
        public override IActionResult Index()
        {
            var model = new StyleGuide(CurrentPage, _publishedValueFallback)
            {
                Metadata = new ViewMetadata { PageTitle = CurrentPage.Name },
                GoogleMapsApiKey = _apiKeyProvider.GetApiKey("GoogleMaps")
            };

            return CurrentTemplate(model);
        }
    }
}