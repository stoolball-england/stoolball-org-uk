using System.Web.Mvc;
using Stoolball.Metadata;
using Stoolball.Web.Security;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;
using Umbraco.Web.PublishedModels;

namespace Stoolball.Web.ContentPage
{
    public class ProductListController : RenderMvcController
    {
        [HttpGet]
        [ContentSecurityPolicy]
        public override ActionResult Index(ContentModel contentModel)
        {
            var model = new ProductList(contentModel?.Content);
            model.Metadata = new ViewMetadata
            {
                PageTitle = model.Name,
                Description = model.Description
            };

            return CurrentTemplate(model);
        }
    }
}