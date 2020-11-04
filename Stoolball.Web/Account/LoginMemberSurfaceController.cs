using System.Collections.Generic;
using System.Web.Mvc;
using Stoolball.Security;
using Stoolball.Web.Email;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Controllers;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;
using Umbraco.Web.PublishedModels;
using UmbracoConstants = Umbraco.Core.Constants;

namespace Stoolball.Web.Account
{
    public class LoginMemberSurfaceController : UmbLoginController
    {
        private readonly IEmailFormatter _emailFormatter;
        private readonly IEmailSender _emailSender;
        private readonly IVerificationToken _verificationToken;
        private readonly IStoolballRouterController _stoolballRouterController;

        public LoginMemberSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory databaseFactory, ServiceContext services, AppCaches appCaches,
            ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, IEmailFormatter emailFormatter, IEmailSender emailSender,
            IVerificationToken verificationToken, IStoolballRouterController stoolballRouterController)
            : base(umbracoContextAccessor, databaseFactory, services, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _emailFormatter = emailFormatter ?? throw new System.ArgumentNullException(nameof(emailFormatter));
            _emailSender = emailSender ?? throw new System.ArgumentNullException(nameof(emailSender));
            _verificationToken = verificationToken ?? throw new System.ArgumentNullException(nameof(verificationToken));
            _stoolballRouterController = stoolballRouterController ?? throw new System.ArgumentNullException(nameof(stoolballRouterController));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public ActionResult Login([Bind(Prefix = "loginModel")] LoginModel model)
        {
            // Check whether login is blocked for any reason. If so, don't even try.
            // blockLogin is an extra status nececesary because we use IsApproved for account activation 
            var member = Services.MemberService.GetByEmail(model?.Username);
            if (member != null && (member.GetValue<bool>("blockLogin") || !member.IsApproved || member.IsLockedOut))
            {
                // Re-send activation email if the account is found but not approved or locked out.
                if (!member.IsApproved || member.IsLockedOut)
                {
                    SendEmailIfNotActivatedOrLockedOut(member);
                }

                // Return the same status the built in controller uses if login fails for any reason
                ModelState.AddModelError("loginModel", "Invalid username or password");
                if (CurrentPage.GetType() == typeof(StoolballRouter))
                {
                    return ReturnToStoolballRouterPage();
                }
                return BlockedLoginResult();
            }

            var baseResult = TryUmbracoLogin(model);

            // If they were logged in, increment their total logins.
            if (ModelState.IsValid)
            {
                member.SetValue("totalLogins", member.GetValue<int>("totalLogins") + 1);
                Services.MemberService.Save(member);
                return Redirect(model.RedirectUrl);
            }

            if (CurrentPage.GetType() == typeof(StoolballRouter))
            {
                return ReturnToStoolballRouterPage();
            }
            return baseResult;
        }

        /// <summary>
        /// Calls the base <see cref="CurrentUmbracoPage" /> in a way which can be overridden for testing
        /// </summary>
        /// <returns></returns>
        protected virtual ActionResult BlockedLoginResult()
        {
            return CurrentUmbracoPage();
        }

        /// <summary>
        /// Calls the base <see cref="HandleLogin" /> in a way which can be overridden for testing
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        protected virtual ActionResult TryUmbracoLogin(LoginModel model) => base.HandleLogin(model);

        private ActionResult ReturnToStoolballRouterPage()
        {
            _stoolballRouterController.ControllerContext = ControllerContext;
            _stoolballRouterController.ControllerContext.RouteData.Values["action"] = ((RouteDefinition)RouteData.DataTokens[UmbracoConstants.Web.UmbracoRouteDefinitionDataToken]).ActionName;
            _stoolballRouterController.ModelState.Merge(ModelState);
            return _stoolballRouterController.Index(ControllerContext.RouteData.DataTokens["umbraco"] as ContentModel).Result;
        }

        private void SendEmailIfNotActivatedOrLockedOut(IMember member)
        {
            string tokenField = string.Empty, tokenExpiryField = string.Empty, emailSubjectField = string.Empty, emailBodyField = string.Empty;
            var loginPage = Umbraco.ContentSingleAtXPath("//loginMember");
            if (loginPage == null)
            {
                // We can't send emails if we don't have the content, so just return.
                return;
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

        /// <summary>
        /// Gets the authority segment of the request URL
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1055:Uri return values should not be strings", Justification = "It's only a partial URI")]
        protected virtual string GetRequestUrlAuthority() => Request.Url.Authority;
    }
}