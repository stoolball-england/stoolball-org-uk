using Stoolball.Metadata;
using System.Web.Mvc;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;
using Umbraco.Web.PublishedModels;

namespace Stoolball.Web.Account
{
    public class MyAccountController : RenderMvcController
    {
        [HttpGet]
        public override ActionResult Index(ContentModel contentModel)
        {
            var model = new MyAccount(contentModel?.Content)
            {
                Metadata = new ViewMetadata { PageTitle = contentModel.Content.Name }
            };

            return CurrentTemplate(model);
        }

        /// <summary>
        /// This method fires when <see cref="MyAccountSurfaceController"/> handles form submissions.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MyAccount(ContentModel contentModel)
        {
            var model = new MyAccount(contentModel?.Content)
            {
                Metadata = new ViewMetadata { PageTitle = contentModel.Content.Name }
            };

            return CurrentTemplate(model);
        }
    }
}