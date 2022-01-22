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

        private readonly IVariationContextAccessor _variationContextAccessor;
        private readonly ServiceContext _serviceContext;

        public StyleGuideController(ILogger<StyleGuideController> logger, ICompositeViewEngine compositeViewEngine, IUmbracoContextAccessor umbracoContextAccessor, IVariationContextAccessor variationContextAccessor, ServiceContext context,
            IApiKeyProvider apiKeyProvider)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _variationContextAccessor = variationContextAccessor ?? throw new System.ArgumentNullException(nameof(variationContextAccessor));
            _serviceContext = context ?? throw new System.ArgumentNullException(nameof(context));
            _apiKeyProvider = apiKeyProvider ?? throw new System.ArgumentNullException(nameof(apiKeyProvider));
        }

        [HttpGet]
        [ContentSecurityPolicy(GoogleMaps = true, TinyMCE = true, Forms = true, GettyImages = true, YouTube = true)]
        public override IActionResult Index()
        {
            var model = new StyleGuide(CurrentPage, new PublishedValueFallback(_serviceContext, _variationContextAccessor))
            {
                Metadata = new ViewMetadata { PageTitle = CurrentPage.Name },
                GoogleMapsApiKey = _apiKeyProvider.GetApiKey("GoogleMaps")
            };

            return CurrentTemplate(model);
        }
    }
}