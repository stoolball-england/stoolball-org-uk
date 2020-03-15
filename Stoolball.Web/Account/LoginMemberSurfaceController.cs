using Stoolball.Security;
using Stoolball.Web.Email;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Controllers;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;

namespace Stoolball.Web.Account
{
    public class LoginMemberSurfaceController : UmbLoginController
    {
        private readonly IEmailFormatter _emailFormatter;
        private readonly IEmailSender _emailSender;
        private readonly IVerificationToken _verificationToken;

        public LoginMemberSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory databaseFactory, ServiceContext services, AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, IEmailFormatter emailFormatter, IEmailSender emailSender, IVerificationToken verificationToken)
            : base(umbracoContextAccessor, databaseFactory, services, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _emailFormatter = emailFormatter;
            _emailSender = emailSender;
            _verificationToken = verificationToken;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        public ActionResult Login([Bind(Prefix = "loginModel")]LoginModel model)
        {
            // Check whether login is blocked. If so, don't even try.
            // This is nececesary because we use IsApproved for account activation 
            // and can re-send an activation email below.
            var member = Services.MemberService.GetByEmail(model?.Username);
            if (member != null && member.GetValue<bool>("blockLogin"))
            {
                // Return the same status the built in controller uses if login fails for any reason
                ModelState.AddModelError("loginModel", "Invalid username or password");
                return CurrentUmbracoPage();
            }

            var result = base.HandleLogin(model);

            // If they were logged in, increment their total logins.
            if (ModelState.IsValid)
            {
                member.SetValue("totalLogins", member.GetValue<int>("totalLogins") + 1);
                Services.MemberService.Save(member);
            }

            // Re-send activation email if the account is found but not approved or locked out.
            // Don't bother checking result since this tells us it will have failed, 
            // and in any case the failure message wouldn't reveal the reason.
            if (member != null && (!member.IsApproved || member.IsLockedOut))
            {
                string tokenField = string.Empty, tokenExpiryField = string.Empty, emailSubjectField = string.Empty, emailBodyField = string.Empty;
                var loginPage = Umbraco.ContentSingleAtXPath("//loginMember");
                if (loginPage == null)
                {
                    // We can't send emails if we don't have the content, so just return the original result.
                    return result;
                }

                if (!member.IsApproved)
                {
                    // The member has not yet activated their account and is trying to login.
                    tokenField = "approvalToken";
                    tokenExpiryField = "approvalTokenExpires";
                    emailSubjectField = "approveMemberSubject";
                    emailBodyField = "approveMemberBody";
                }
                else if (member.IsLockedOut)
                {
                    // Approved member, OK to reset their password.
                    tokenField = "passwordResetToken";
                    tokenExpiryField = "passwordResetTokenExpires";
                    emailSubjectField = "resetPasswordSubject";
                    emailBodyField = "resetPasswordBody";
                }

                // Create a password reset / approval token including the id so we can find the member
                var (token, expires) = _verificationToken.TokenFor(member.Id);
                member.SetValue(tokenField, token);
                member.SetValue(tokenExpiryField, expires);

                Services.MemberService.Save(member);

                // Send the password reset / member approval email
                var (sender, body) = _emailFormatter.FormatEmailContent(loginPage.Value<string>(emailSubjectField),
                    loginPage.Value<string>(emailBodyField),
                    new Dictionary<string, string>
                    {
                        {"name", member.Name},
                        {"email", member.Email},
                        {"token", token},
                        {"domain", GetRequestUrlAuthority()}
                    });
                _emailSender.SendEmail(member.Email, sender, body);
            }

            return result;
        }

        /// <summary>
        /// Gets the authority segment of the request URL
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1055:Uri return values should not be strings", Justification = "It's only a partial URI")]
        protected virtual string GetRequestUrlAuthority() => Request.Url.Authority;
    }
}