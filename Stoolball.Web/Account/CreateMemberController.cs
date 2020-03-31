using Stoolball.Metadata;
using System.Web.Mvc;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;
using Umbraco.Web.PublishedModels;

namespace Stoolball.Web.Account
{
    public class CreateMemberController : RenderMvcController
    {
        [HttpGet]
        public override ActionResult Index(ContentModel contentModel)
        {
            var model = new CreateMember(contentModel?.Content)
            {
                Metadata = new ViewMetadata { PageTitle = contentModel.Content.Name }
            };

            return CurrentTemplate(model);
        }

        /// <summary>
        /// This method fires when <see cref="CreateMemberSurfaceController"/> handles form submissions.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateMember(ContentModel contentModel)
        {
            var model = new CreateMember(contentModel?.Content)
            {
                Metadata = new ViewMetadata { PageTitle = contentModel.Content.Name }
            };

            return CurrentTemplate(model);
        }
    }
}