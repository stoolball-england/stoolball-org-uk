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
    public class ContentController : RenderController
    {
        private readonly IVariationContextAccessor _variationContextAccessor;
        private readonly ServiceContext _serviceContext;

        public ContentController(ILogger<ContentController> logger, ICompositeViewEngine compositeViewEngine, IUmbracoContextAccessor umbracoContextAccessor, IVariationContextAccessor variationContextAccessor, ServiceContext context)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _variationContextAccessor = variationContextAccessor ?? throw new System.ArgumentNullException(nameof(variationContextAccessor));
            _serviceContext = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true, TinyMCE = true, GettyImages = true, YouTube = true)]
        public override IActionResult Index()
        {
            var model = new Content(CurrentPage, new PublishedValueFallback(_serviceContext, _variationContextAccessor));
            model.Metadata = new ViewMetadata
            {
                PageTitle = model.Name,
                Description = model.Description
            };

            return CurrentTemplate(model);
        }

        ///* This action is triggered when an Umbraco Forms form is submitted without a separate 'thank you' page */
        //[HttpPost]
        //public IActionResult Content()
        //{
        //    var model = new Content(CurrentPage, new PublishedValueFallback(_serviceContext, _variationContextAccessor));
        //    model.Metadata = new ViewMetadata
        //    {
        //        PageTitle = model.Name,
        //        Description = model.Description
        //    };

        //    return CurrentTemplate(model);
        //}
    }
}