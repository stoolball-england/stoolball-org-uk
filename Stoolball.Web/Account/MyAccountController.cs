using Stoolball.Metadata;
using Stoolball.Web.Security;
using System.Web.Mvc;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;
using Umbraco.Web.PublishedModels;

namespace Stoolball.Web.Account
{
    public class MyAccountController : RenderMvcController
    {
        [HttpGet]
        [ContentSecurityPolicy(Forms = true)]
        public override ActionResult Index(ContentModel contentModel)
        {
            var model = new MyAccount(contentModel?.Content)
            {
                Metadata = new ViewMetadata { PageTitle = contentModel.Content.Name }
            };

            return CurrentTemplate(model);
        }

        /// <summary>
        /// This method fires after <see cref="MyAccountSurfaceController"/> handles form submissions, if it doesn't redirect
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