using System;
using System.Web.Mvc;
using Stoolball.Metadata;
using Stoolball.Security;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;
using Umbraco.Web.PublishedModels;
using static Stoolball.Constants;

namespace Stoolball.Web.Account
{
    public class ConfirmEmailAddressController : RenderMvcController
    {
        private readonly IVerificationToken _verificationToken;

        public ConfirmEmailAddressController(IGlobalSettings globalSettings,
            IUmbracoContextAccessor umbracoContextAccessor,
            ServiceContext services,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            UmbracoHelper umbracoHelper,
            IVerificationToken verificationToken) :
            base(globalSettings, umbracoContextAccessor, services, appCaches, profilingLogger, umbracoHelper)
        {
            _verificationToken = verificationToken ?? throw new ArgumentNullException(nameof(verificationToken));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public override ActionResult Index(ContentModel contentModel)
        {
            var model = new ConfirmEmailAddress(contentModel?.Content);
            model.Metadata = new ViewMetadata
            {
                PageTitle = model.Name,
                Description = model.Description
            };

            try
            {
                var token = Request.QueryString["token"];
                var memberId = _verificationToken.ExtractId(token);

                var memberService = Services.MemberService;
                var member = memberService.GetById(memberId);

                if (member != null && member.GetValue("requestedEmailToken")?.ToString() == token && !_verificationToken.HasExpired(member.GetValue<DateTime>("requestedEmailTokenExpires")))
                {
                    // Update the email address and expire the token
                    member.Username = member.Email = member.GetValue("requestedEmail").ToString();
                    member.SetValue("requestedEmailTokenExpires", _verificationToken.ResetExpiryTo());
                    memberService.Save(member);

                    model.TokenValid = true;
                    model.MemberName = member.Name;
                    model.EmailAddress = member.Email;

                    Logger.Info(typeof(ConfirmEmailAddressController), LoggingTemplates.ConfirmEmailAddress, member.Username, member.Key, typeof(ConfirmEmailAddressController), nameof(Index));
                }
                else
                {
                    model.TokenValid = false;
                }
            }
            catch (FormatException)
            {
                model.TokenValid = false;
            }
            return View("ConfirmEmailAddress", model);
        }
    }
}