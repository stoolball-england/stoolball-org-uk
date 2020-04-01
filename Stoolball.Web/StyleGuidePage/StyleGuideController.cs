using Stoolball.Metadata;
using System.Web.Mvc;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;
using Umbraco.Web.PublishedModels;

namespace Stoolball.Web.StyleGuidePage
{
    public class StyleGuideController : RenderMvcController
    {
        [HttpGet]
        public override ActionResult Index(ContentModel contentModel)
        {
            var model = new StyleGuide(contentModel?.Content)
            {
                Metadata = new ViewMetadata { PageTitle = contentModel.Content.Name }
            };

            return CurrentTemplate(model);
        }
    }
}