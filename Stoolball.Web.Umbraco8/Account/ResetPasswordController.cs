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
    public class ResetPasswordController : RenderMvcController
    {
        private readonly IVerificationToken _verificationToken;

        public ResetPasswordController(
            IGlobalSettings globalSettings,
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
        [ContentSecurityPolicy(Forms = true)]
        public override ActionResult Index(ContentModel contentModel)
        {
            var model = new ResetPassword(contentModel?.Content);
            model.Metadata = new ViewMetadata
            {
                PageTitle = model.Name,
                Description = model.Description
            };

            try
            {
                model.PasswordResetToken = Request.QueryString["token"];

                // If there's no token, show the form to request a password reset
                if (string.IsNullOrEmpty(model.PasswordResetToken))
                {
                    return View("ResetPasswordRequest", model);
                }

                // Show a message saying the reset was successful
                if (Request.QueryString["successful"] == "yes")
                {
                    model.ShowPasswordResetSuccessful = true;
                    return View("ResetPasswordComplete", model);
                }

                var memberId = _verificationToken.ExtractId(model.PasswordResetToken);

                var member = Services.MemberService.GetById(memberId);

                if (member.GetValue("passwordResetToken").ToString() == model.PasswordResetToken && !_verificationToken.HasExpired(member.GetValue<DateTime>("passwordResetTokenExpires")))
                {
                    // Show the set a new password form
                    model.PasswordResetTokenValid = true;
                }
                else
                {
                    // Show a message saying the token was not valid
                    Logger.Info(typeof(ResetPasswordController), LoggingTemplates.MemberPasswordResetTokenInvalid, model.PasswordResetToken, typeof(ResetPasswordController), nameof(ResetPasswordController.Index));
                    model.PasswordResetTokenValid = false;
                }
            }
            catch (FormatException)
            {
                // Show a message saying the token was not valid
                Logger.Info(typeof(ResetPasswordController), LoggingTemplates.MemberPasswordResetTokenInvalid, model.PasswordResetToken, typeof(ResetPasswordController), nameof(ResetPasswordController.Index));
                model.PasswordResetTokenValid = false;
            }
            return View("ResetPassword", model);
        }
    }
}