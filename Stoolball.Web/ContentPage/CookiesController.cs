using System.Web.Mvc;
using Stoolball.Metadata;
using Stoolball.Web.Security;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;
using Umbraco.Web.PublishedModels;

namespace Stoolball.Web.ContentPage
{
    public class CookiesController : RenderMvcController
    {
        [HttpGet]
        [ContentSecurityPolicy(Forms = true, TinyMCE = true)]
        public override ActionResult Index(ContentModel contentModel)
        {
            var model = new Content(contentModel?.Content);
            model.Metadata = new ViewMetadata
            {
                PageTitle = model.Name,
                Description = model.Description
            };

            return CurrentTemplate(model);
        }
    }
}