using System.Web.Mvc;
using Stoolball.Metadata;
using Stoolball.Web.Security;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;
using Umbraco.Web.PublishedModels;

namespace Stoolball.Web.ContentPage
{
    public class ContentController : RenderMvcController
    {
        [HttpGet]
        [ContentSecurityPolicy(Forms = true, TinyMCE = true)]
        public override ActionResult Index(ContentModel contentModel)
        {
            var model = new Content(contentModel?.Content)
            {
                Metadata = new ViewMetadata { PageTitle = contentModel.Content.Name }
            };

            return CurrentTemplate(model);
        }

        [HttpPost]
        public ActionResult Content(ContentModel contentModel)
        {
            var model = new Content(contentModel?.Content)
            {
                Metadata = new ViewMetadata { PageTitle = contentModel.Content.Name }
            };

            return CurrentTemplate(model);
        }
    }
}