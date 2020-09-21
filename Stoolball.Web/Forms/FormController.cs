using System.Web.Mvc;
using Stoolball.Metadata;
using Stoolball.Web.Security;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;
using Umbraco.Web.PublishedModels;

namespace Stoolball.Web.HomePage
{
    public class FormController : RenderMvcController
    {
        [HttpGet]
        [ContentSecurityPolicy(Forms = true, TinyMCE = true)]
        public override ActionResult Index(ContentModel contentModel)
        {
            var model = new Form(contentModel?.Content)
            {
                Metadata = new ViewMetadata { PageTitle = contentModel.Content.Name }
            };

            return CurrentTemplate(model);
        }

        [HttpPost]
        public ActionResult Form(ContentModel contentModel)
        {
            var model = new Form(contentModel?.Content)
            {
                Metadata = new ViewMetadata { PageTitle = contentModel.Content.Name }
            };

            return CurrentTemplate(model);
        }
    }
}