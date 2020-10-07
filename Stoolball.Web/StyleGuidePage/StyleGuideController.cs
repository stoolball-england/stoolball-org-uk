using System.Web.Mvc;
using Stoolball.Metadata;
using Stoolball.Web.Configuration;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;
using Umbraco.Web.PublishedModels;

namespace Stoolball.Web.StyleGuidePage
{
    public class StyleGuideController : RenderMvcController
    {
        private readonly IApiKeyProvider _apiKeyProvider;

        public StyleGuideController(IGlobalSettings globalSettings, IUmbracoContextAccessor umbracoContextAccessor, ServiceContext services, AppCaches appCaches, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, IApiKeyProvider apiKeyProvider) :
            base(globalSettings, umbracoContextAccessor, services, appCaches, profilingLogger, umbracoHelper)
        {
            _apiKeyProvider = apiKeyProvider ?? throw new System.ArgumentNullException(nameof(apiKeyProvider));
        }

        [HttpGet]
        [ContentSecurityPolicy(GoogleMaps = true, TinyMCE = true, Forms = true, GettyImages = true, YouTube = true)]
        public override ActionResult Index(ContentModel contentModel)
        {
            var model = new StyleGuide(contentModel?.Content)
            {
                Metadata = new ViewMetadata { PageTitle = contentModel.Content.Name },
                GoogleMapsApiKey = _apiKeyProvider.GetApiKey("GoogleMaps")
            };

            return CurrentTemplate(model);
        }
    }
}