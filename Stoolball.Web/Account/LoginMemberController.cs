﻿using System.Web.Mvc;
using Stoolball.Metadata;
using Stoolball.Web.Security;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;
using Umbraco.Web.PublishedModels;

namespace Stoolball.Web.Account
{
    public class LoginMemberController : RenderMvcController
    {
        [HttpGet]
        [ContentSecurityPolicy(Forms = true)]
        public override ActionResult Index(ContentModel contentModel)
        {
            var model = new LoginMember(contentModel?.Content);
            model.Metadata = new ViewMetadata
            {
                PageTitle = model.Name,
                Description = model.Description
            };

            return CurrentTemplate(model);
        }

        /// <summary>
        /// This method fires after <see cref="LoginMemberSurfaceController"/> handles form submissions, if it doesn't redirect.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LoginMember(ContentModel contentModel)
        {
            var model = new LoginMember(contentModel?.Content);
            model.Metadata = new ViewMetadata
            {
                PageTitle = model.Name,
                Description = model.Description
            };

            return CurrentTemplate(model);
        }
    }
}