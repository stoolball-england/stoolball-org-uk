﻿using System.Web.Mvc;
using Stoolball.Metadata;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;
using Umbraco.Web.PublishedModels;

namespace Stoolball.Web.Account
{
    public class EmailAddressController : RenderMvcController
    {
        public EmailAddressController(IGlobalSettings globalSettings, IUmbracoContextAccessor umbracoContextAccessor, ServiceContext services, AppCaches appCaches, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper) :
          base(globalSettings, umbracoContextAccessor, services, appCaches, profilingLogger, umbracoHelper)
        {
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true)]
        public override ActionResult Index(ContentModel contentModel)
        {
            var model = new EmailAddress(contentModel?.Content);
            model.Metadata = new ViewMetadata
            {
                PageTitle = model.Name,
                Description = model.Description
            };

            return View(nameof(EmailAddress), model);
        }

        /// <summary>
        /// This method fires after <see cref="EmailAddressSurfaceController"/> handles form submissions, if it doesn't redirect
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EmailAddress(ContentModel contentModel)
        {
            var model = new EmailAddress(contentModel?.Content);
            model.Metadata = new ViewMetadata
            {
                PageTitle = model.Name,
                Description = model.Description
            };

            return View(nameof(EmailAddress), model);
        }
    }
}