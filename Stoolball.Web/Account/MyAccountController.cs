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

namespace Stoolball.Web.Account
{
    public class MyAccountController : RenderController
    {
        private readonly IVariationContextAccessor _variationContextAccessor;
        private readonly ServiceContext _serviceContext;

        public MyAccountController(
            ILogger<MyAccountController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IVariationContextAccessor variationContextAccessor,
            ServiceContext context) :
            base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _variationContextAccessor = variationContextAccessor ?? throw new System.ArgumentNullException(nameof(variationContextAccessor));
            _serviceContext = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public override IActionResult Index()
        {
            var model = new MyAccount(CurrentPage, new PublishedValueFallback(_serviceContext, _variationContextAccessor));
            model.Metadata = new ViewMetadata
            {
                PageTitle = model.Name,
                Description = model.Description
            };

            return View(nameof(MyAccount), model);
        }
    }
}