using Stoolball.Security;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
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
            base(globalSettings, umbracoContextAccessor, services, appCaches, profilingLogger, umbracoHelper) {
            _verificationToken = verificationToken;
        }

        // Gets the password reset token from the querystring in a way that can be overridden for testing
        protected virtual string ReadPasswordResetToken() => Request.QueryString["token"];

        [HttpGet]
        public override ActionResult Index(ContentModel model)
        {
            var contentModel = new ResetPassword(model?.Content);

            try
            {
                contentModel.PasswordResetToken = ReadPasswordResetToken();

                // If there's no token, show the form to request a password reset
                if (string.IsNullOrEmpty(contentModel.PasswordResetToken))
                {
                    return CurrentTemplate(contentModel);
                }

                var memberId = _verificationToken.ExtractId(contentModel.PasswordResetToken);

                var memberService = Services.MemberService;
                var member = memberService.GetById(memberId);

                if (member.GetValue("passwordResetToken").ToString() == contentModel.PasswordResetToken && !_verificationToken.HasExpired(member.GetValue<DateTime>("passwordResetTokenExpires")))
                {
                    // Show the set a new password form
                    memberService.Save(member);

                    contentModel.PasswordResetTokenValid = true;
                    contentModel.MemberName = member.Name;
                }
                else
                {
                    // Show a message saying the token was not valid
                    Logger.Info(this.GetType(), $"Password reset token invalid {contentModel.PasswordResetToken}");
                    contentModel.PasswordResetTokenValid = false;
                }
            }
            catch (FormatException)
            {
                // Show a message saying the token was not valid
                Logger.Info(this.GetType(), $"Password reset token invalid {contentModel.PasswordResetToken}");
                contentModel.PasswordResetTokenValid = false;
            }
            return CurrentTemplate(contentModel);
        }

        /// <summary>
        /// This method fires when <see cref="ResetPasswordSurfaceController"/> handles a form submissions.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(ContentModel model)
        {
            // Record the presence of a token. This informs the view whether 
            // the password reset request or password update form was submitted.
            // Assume it's valid and this will be checked later, but this is used in the view when 
            // routing an invalid password update form submission.
            var contentModel = new ResetPassword(model?.Content);
            contentModel.PasswordResetToken = ReadPasswordResetToken();
            contentModel.PasswordResetTokenValid = true;
            return CurrentTemplate(contentModel);
        }
    }
}