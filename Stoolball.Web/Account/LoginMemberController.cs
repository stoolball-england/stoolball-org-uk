﻿using Stoolball.Metadata;
using System.Web.Mvc;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;
using Umbraco.Web.PublishedModels;

namespace Stoolball.Web.Account
{
    public class LoginMemberController : RenderMvcController
    {
        [HttpGet]
        public override ActionResult Index(ContentModel contentModel)
        {
            var model = new LoginMember(contentModel?.Content)
            {
                Metadata = new ViewMetadata { PageTitle = contentModel.Content.Name }
            };

            return CurrentTemplate(model);
        }

        /// <summary>
        /// This method fires when <see cref="LoginMemberSurfaceController"/> handles form submissions.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LoginMember(ContentModel contentModel)
        {
            var model = new LoginMember(contentModel?.Content)
            {
                Metadata = new ViewMetadata { PageTitle = contentModel.Content.Name }
            };

            return CurrentTemplate(model);
        }
    }
}