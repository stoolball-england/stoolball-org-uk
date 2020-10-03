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

namespace Stoolball.Web.Account
{
    public class ResetPasswordController : RenderMvcController
    {
        private readonly IVerificationToken _verificationToken;

        public ResetPasswordController(IGlobalSettings globalSettings, IUmbracoContextAccessor umbracoContextAccessor, ServiceContext services, AppCaches appCaches, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, IVerificationToken verificationToken) :
            base(globalSettings, umbracoContextAccessor, services, appCaches, profilingLogger, umbracoHelper)
        {
            _verificationToken = verificationToken;
        }

        // Gets the password reset token from the querystring in a way that can be overridden for testing
        protected virtual string ReadPasswordResetToken() => Request.QueryString["token"];

        protected virtual bool PasswordResetSuccessful() => Request.QueryString["successful"] == "yes";

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
                model.PasswordResetToken = ReadPasswordResetToken();

                // Show a message saying the reset was successful
                if (PasswordResetSuccessful())
                {
                    TempData["PasswordResetSuccessful"] = true;
                    return CurrentTemplate(model);
                }

                // If there's no token, show the form to request a password reset
                if (string.IsNullOrEmpty(model.PasswordResetToken))
                {
                    return CurrentTemplate(model);
                }

                var memberId = _verificationToken.ExtractId(model.PasswordResetToken);

                var memberService = Services.MemberService;
                var member = memberService.GetById(memberId);

                if (member.GetValue("passwordResetToken").ToString() == model.PasswordResetToken && !_verificationToken.HasExpired(member.GetValue<DateTime>("passwordResetTokenExpires")))
                {
                    // Show the set a new password form
                    memberService.Save(member);

                    model.PasswordResetTokenValid = true;
                    model.MemberName = member.Name;
                }
                else
                {
                    // Show a message saying the token was not valid
                    Logger.Info(GetType(), $"Password reset token invalid {model.PasswordResetToken}");
                    model.PasswordResetTokenValid = false;
                }
            }
            catch (FormatException)
            {
                // Show a message saying the token was not valid
                Logger.Info(GetType(), $"Password reset token invalid {model.PasswordResetToken}");
                model.PasswordResetTokenValid = false;
            }
            return CurrentTemplate(model);
        }

        /// <summary>
        /// This method fires after <see cref="ResetPasswordSurfaceController"/> handles form submissions, if it doesn't redirect.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(ContentModel contentModel)
        {
            // Record the presence of a token. This informs the view whether 
            // the password reset request or password update form was submitted.
            // Assume it's valid and this will be checked later, but this is used in the view when 
            // routing an invalid password update form submission.
            var model = new ResetPassword(contentModel?.Content);
            model.Metadata = new ViewMetadata
            {
                PageTitle = model.Name,
                Description = model.Description
            };
            model.PasswordResetToken = ReadPasswordResetToken();
            model.PasswordResetTokenValid = true;
            return CurrentTemplate(model);
        }
    }
}