using Stoolball.Security;
using Stoolball.Web.Email;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;


namespace Stoolball.Web.Account
{
    public class ResetPasswordSurfaceController : SurfaceController
    {
        private readonly IEmailFormatter _emailFormatter;
        private readonly IEmailSender _emailSender;
        private readonly IVerificationToken _verificationToken;

        public ResetPasswordSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory databaseFactory, ServiceContext services, AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, IEmailFormatter emailFormatter, IEmailSender emailSender, IVerificationToken verificationToken)
            : base(umbracoContextAccessor, databaseFactory, services, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _emailFormatter = emailFormatter;
            _emailSender = emailSender;
            _verificationToken = verificationToken;
        }

        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult RequestPasswordReset([Bind(Prefix = "resetPasswordRequest")]ResetPasswordRequest model)
        {
            if (!ModelState.IsValid || model == null)
            {
                ModelState.AddModelError("Email", "Please enter a valid email address.");
                return CurrentUmbracoPage();
            }

            // Put the entered email address in TempData so that it can be accessed in the view
            TempData["Email"] = model.Email;

            // Get the matching member, if there is one
            var memberService = Services.MemberService;
            var member = memberService.GetByEmail(model.Email);

            if (member != null)
            {
                string tokenField, tokenExpiryField, emailSubjectField, emailBodyField;

                if (member.IsApproved)
                {
                    // Approved member, OK to reset their password.
                    tokenField = "passwordResetToken";
                    tokenExpiryField = "passwordResetTokenExpires";
                    emailSubjectField = "resetPasswordSubject";
                    emailBodyField = "resetPasswordBody";
                }
                else
                {
                    // The member has not yet activated their account and is trying to reset the password.
                    tokenField = "approvalToken";
                    tokenExpiryField = "approvalTokenExpires";
                    emailSubjectField = "approveMemberSubject";
                    emailBodyField = "approveMemberBody";
                }

                // Create a password reset / approval token including the id so we can find the member
                var (token, expires) = _verificationToken.TokenFor(member.Id);
                member.SetValue(tokenField, token);
                member.SetValue(tokenExpiryField, expires);

                memberService.Save(member);

                // Send the password reset / member approval email
                var (sender, body) = _emailFormatter.FormatEmailContent(CurrentPage.Value<string>(emailSubjectField),
                    CurrentPage.Value<string>(emailBodyField),
                    new Dictionary<string, string>
                    {
                        {"name", member.Name},
                        {"email", model.Email},
                        {"token", token},
                        {"domain", GetRequestUrlAuthority()}
                    });
                _emailSender.SendEmail(model.Email, sender, body);

                TempData["PasswordResetRequested"] = true;
                return CurrentUmbracoPage();
            }
            else
            {
                // Same result as if a member was found, since password reset should not reveal a valid email address
                // However we can prompt them to create an account. Since it sends an email either way this also guards
                // against detecting the result by timing the response.
                var (sender, body) = _emailFormatter.FormatEmailContent(CurrentPage.Value<string>("createMemberSubject"),
                    CurrentPage.Value<string>("createMemberBody"),
                    new Dictionary<string, string>
                    {
                        {"email", model.Email},
                        {"domain", GetRequestUrlAuthority()}
                    });
                _emailSender.SendEmail(model.Email, sender, body);

                TempData["PasswordResetRequested"] = true;
                return CurrentUmbracoPage();
            }

        }

        /// <summary>
        /// Gets the authority segment of the request URL
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1055:Uri return values should not be strings", Justification = "It's only a partial URI")]
        protected virtual string GetRequestUrlAuthority() => Request.Url.Authority;

        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult UpdatePassword([Bind(Prefix = "resetPasswordUpdate")]ResetPasswordUpdate model)
        {
            if (!ModelState.IsValid || model == null)
            {
                return CurrentUmbracoPage();
            }

            try
            {
                var memberId = _verificationToken.ExtractId(model.PasswordResetToken);

                var memberService = Services.MemberService;
                var member = memberService.GetById(memberId);

                if (member != null)
                {
                    if (member.GetValue<string>("passwordResetToken") == model.PasswordResetToken &&
                        !_verificationToken.HasExpired(member.GetValue<DateTime>("passwordResetTokenExpires"))
                        && model.NewPassword == model.ConfirmNewPassword)
                    {
                        // If the user has tried repeatedly they might have locked their account
                        // Remove the lockout and expire the token
                        member.IsLockedOut = false;
                        member.SetValue("passwordResetTokenExpires", _verificationToken.ResetExpiryTo());
                        memberService.Save(member);

                        // Reset the password
                        memberService.SavePassword(member, model.NewPassword);

                        // They obviously wanted to login, so be helpful and do it
                        Umbraco.MembershipHelper.Login(member.Username, model.NewPassword);

                        TempData["PasswordResetSuccessful"] = true;
                        return CurrentUmbracoPage();
                    }
                    else
                    {
                        Logger.Info(this.GetType(), $"Password reset token invalid {model.PasswordResetToken}");
                        TempData["PasswordResetSuccessful"] = false;
                        return CurrentUmbracoPage();
                    }
                }
                else
                {
                    Logger.Info(this.GetType(), $"Password reset token invalid {model.PasswordResetToken}");
                    TempData["PasswordResetSuccessful"] = false;
                    return CurrentUmbracoPage();
                }
            }
            catch (FormatException)
            {
                Logger.Info(this.GetType(), $"Password reset token invalid {model.PasswordResetToken}");
                TempData["PasswordResetSuccessful"] = false;
                return CurrentUmbracoPage();
            }
        }
    }
}