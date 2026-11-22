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
    public class HomeController : RenderController
    {
        private readonly IPublishedValueFallback _publishedValueFallback;

        public HomeController(ILogger<HomeController> logger, ICompositeViewEngine compositeViewEngine, IUmbracoContextAccessor umbracoContextAccessor, IPublishedValueFallback publishedValueFallback)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _publishedValueFallback = publishedValueFallback ?? throw new System.ArgumentNullException(nameof(publishedValueFallback));
        }

        [HttpGet]
        [HttpHead] // Umbraco Cloud's health check does a HEAD request to /
        [ContentSecurityPolicy]
        public override IActionResult Index()
        {
            var model = new Home(CurrentPage, _publishedValueFallback)
            {
                Metadata = new ViewMetadata { PageTitle = CurrentPage!.Name }
            };

            return CurrentTemplate(model);
        }
    }
}