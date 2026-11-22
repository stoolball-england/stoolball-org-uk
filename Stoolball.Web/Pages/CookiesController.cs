using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Metadata;
using Stoolball.Web.Models;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Pages
{
    public class CookiesController : RenderController
    {
        private readonly IPublishedValueFallback _publishedValueFallback;

        public CookiesController(ILogger<CookiesController> logger, ICompositeViewEngine compositeViewEngine, IUmbracoContextAccessor umbracoContextAccessor, IPublishedValueFallback publishedValueFallback)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _publishedValueFallback = publishedValueFallback ?? throw new System.ArgumentNullException(nameof(publishedValueFallback));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true, TinyMCE = true)]
        public override IActionResult Index()
        {
            var model = new Content(CurrentPage, _publishedValueFallback);
            model.Metadata = new ViewMetadata
            {
                PageTitle = model.Name,
                Description = model.Description
            };

            return CurrentTemplate(model);
        }
    }
}