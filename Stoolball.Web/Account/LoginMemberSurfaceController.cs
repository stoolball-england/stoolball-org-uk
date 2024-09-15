using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Email;
using Stoolball.Security;
using Stoolball.Web.Models;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Email;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Common.Models;
using Umbraco.Cms.Web.Common.Security;
using Umbraco.Cms.Web.Website.Controllers;
using Umbraco.Extensions;

namespace Stoolball.Web.Account
{
    public class LoginMemberSurfaceController : UmbLoginController
    {
        private readonly IEmailFormatter _emailFormatter;
        private readonly Umbraco.Cms.Core.Mail.IEmailSender _emailSender;
        private readonly IVerificationToken _verificationToken;
        private readonly IStoolballRouterController _stoolballRouterController;

        public LoginMemberSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory databaseFactory, ServiceContext services,
            AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider,
            IMemberSignInManager memberSignInManager, IMemberManager memberManager, ITwoFactorLoginService twoFactorLoginService,
            IEmailFormatter emailFormatter, Umbraco.Cms.Core.Mail.IEmailSender emailSender,
            IVerificationToken verificationToken, IStoolballRouterController stoolballRouterController)
            : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider, memberSignInManager, memberManager, twoFactorLoginService)
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
        public async Task<IActionResult> Login([Bind(Prefix = "loginModel")] LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }

            // Check whether login is blocked for any reason. If so, don't even try.
            // blockLogin is an extra status nececesary because we use IsApproved for account activation 
            var member = Services.MemberService.GetByEmail(model.Username);
            if (member != null && (member.GetValue<bool>("blockLogin") || !member.IsApproved || member.IsLockedOut))
            {
                // Re-send activation email if the account is found but not approved or locked out.
                if (!member.IsApproved || member.IsLockedOut)
                {
                    await SendEmailIfNotActivatedOrLockedOut(member);
                }

                // Return the same status the built in controller uses if login fails for any reason
                ModelState.AddModelError("loginModel", "Invalid username or password");
                if (CurrentPage.GetType() == typeof(StoolballRouter))
                {
                    return ReturnToStoolballRouterPage();
                }
                return BlockedLoginResult();
            }

            var baseResult = await TryUmbracoLogin(model);

            // If they were logged in, increment their total logins.
            if (member != null && ModelState.IsValid)
            {
                member.SetValue("totalLogins", member.GetValue<int>("totalLogins") + 1);
                Services.MemberService.Save(member);
                return Redirect(model.RedirectUrl);
            }

            // If this was a page demanding permissions, return to that page. Otherwise return the base result to the standard login page.
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
        protected virtual IActionResult BlockedLoginResult()
        {
            return CurrentUmbracoPage();
        }

        /// <summary>
        /// Calls the base <see cref="HandleLogin" /> in a way which can be overridden for testing
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        protected async virtual Task<IActionResult> TryUmbracoLogin(LoginModel model) => await base.HandleLogin(model);

        private IActionResult ReturnToStoolballRouterPage()
        {
            _stoolballRouterController.ControllerContext = ControllerContext;
            _stoolballRouterController.ModelState.Merge(ModelState);
            return _stoolballRouterController.Index().Result;
        }

        private async Task SendEmailIfNotActivatedOrLockedOut(IMember member)
        {
            string tokenField = string.Empty, tokenExpiryField = string.Empty, emailSubjectField = string.Empty, emailBodyField = string.Empty;
            var loginPage = UmbracoContext.Content?.GetByRoute(Constants.Pages.SignInUrl);
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
            var (subject, body) = _emailFormatter.FormatEmailContent(loginPage.Value<string>(emailSubjectField),
                loginPage.Value<string>(emailBodyField),
                new Dictionary<string, string>
                {
                    {"name", member.Name},
                    {"email", member.Email},
                    {"token", token},
                    {"domain", GetRequestUrlAuthority()}
                });

            await _emailSender.SendAsync(new EmailMessage(null, member.Email, subject, body, true), null);
        }

        /// <summary>
        /// Gets the authority segment of the request URL
        /// </summary>
        /// <returns></returns>
        protected virtual string GetRequestUrlAuthority() => Request.Host.Host == "localhost" ? Request.Host.Value : "www.stoolball.org.uk";
    }
}