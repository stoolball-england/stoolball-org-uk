using Stoolball.Metadata;
using System.Web.Mvc;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;
using Umbraco.Web.PublishedModels;

namespace Stoolball.Web.HomePage
{
    public class HomeController : RenderMvcController
    {
        [HttpGet]
        public override ActionResult Index(ContentModel contentModel)
        {
            var model = new Home(contentModel?.Content)
            {
                Metadata = new ViewMetadata { PageTitle = contentModel.Content.Name }
            };

            return CurrentTemplate(model);
        }
    }
}