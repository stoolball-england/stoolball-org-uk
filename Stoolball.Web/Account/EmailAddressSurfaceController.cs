﻿using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Security;
using Stoolball.Security;
using Stoolball.Web.Email;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;
using static Stoolball.Constants;

namespace Stoolball.Web.Account
{
    public class EmailAddressSurfaceController : SurfaceController
    {
        private readonly MembershipProvider _membershipProvider;
        private readonly IEmailFormatter _emailFormatter;
        private readonly IEmailSender _emailSender;
        private readonly IVerificationToken _verificationToken;

        public EmailAddressSurfaceController(IUmbracoContextAccessor umbracoContextAccessor,
            IUmbracoDatabaseFactory databaseFactory,
            ServiceContext services,
            AppCaches appCaches,
            ILogger logger,
            IProfilingLogger profilingLogger,
            UmbracoHelper umbracoHelper,
            MembershipProvider membershipProvider,
            IEmailFormatter emailFormatter,
            IEmailSender emailSender,
            IVerificationToken verificationToken)
        : base(umbracoContextAccessor, databaseFactory, services, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _membershipProvider = membershipProvider ?? throw new System.ArgumentNullException(nameof(membershipProvider));
            _emailFormatter = emailFormatter ?? throw new System.ArgumentNullException(nameof(emailFormatter));
            _emailSender = emailSender ?? throw new System.ArgumentNullException(nameof(emailSender));
            _verificationToken = verificationToken ?? throw new System.ArgumentNullException(nameof(verificationToken));
        }

        [HttpPost]
        [ValidateAntiForgeryToken()]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public ActionResult UpdateEmailAddress([Bind(Prefix = "formData")] EmailAddressFormData model)
        {
            if (!_membershipProvider.ValidateUser(Members.CurrentUserName, model?.Password?.Trim()))
            {
                ModelState.AddModelError("formData." + nameof(model.Password), "Your password is incorrect or your account is locked.");
            }

            if (ModelState.IsValid && model != null)
            {
                var member = Members.GetCurrentMember();

                // Check whether the requested email already belongs to another account
                var alreadyTaken = Members.GetByEmail(model.RequestedEmail?.Trim());
                if (alreadyTaken != null && alreadyTaken.Key != member.Key)
                {
                    // Send the address already in use email
                    var (subject, body) = _emailFormatter.FormatEmailContent(CurrentPage.Value<string>("emailTakenSubject"),
                        CurrentPage.Value<string>("emailTakenBody"),
                        new Dictionary<string, string>
                        {
                        {"name", alreadyTaken.Name},
                        {"email", model.RequestedEmail},
                        {"domain", GetRequestUrlAuthority()}
                        });
                    _emailSender.SendEmail(model.RequestedEmail?.Trim(), subject, body);

                    Logger.Info(typeof(EmailAddressSurfaceController), LoggingTemplates.MemberRequestedEmailAddressAlreadyInUse, member.Name, member.Key, typeof(EmailAddressSurfaceController), nameof(UpdateEmailAddress));
                }
                else
                {
                    // Create an requested email token including the id so we can find the member
                    var (token, expires) = _verificationToken.TokenFor(member.Id);
                    var editableMember = Services.MemberService.GetById(member.Id);
                    editableMember.SetValue("requestedEmail", model.RequestedEmail?.Trim());
                    editableMember.SetValue("requestedEmailToken", token);
                    editableMember.SetValue("requestedEmailTokenExpires", expires);
                    Services.MemberService.Save(editableMember);

                    // Send the token by email
                    var (subject, body) = _emailFormatter.FormatEmailContent(CurrentPage.Value<string>("confirmEmailSubject"),
                        CurrentPage.Value<string>("confirmEmailBody"),
                        new Dictionary<string, string>
                        {
                        {"name", member.Name},
                        {"email", model.RequestedEmail},
                        {"domain", GetRequestUrlAuthority()},
                        {"token", token }
                        });
                    _emailSender.SendEmail(model.RequestedEmail?.Trim(), subject, body);

                    Logger.Info(typeof(EmailAddressSurfaceController), LoggingTemplates.MemberRequestedEmailAddress, member.Name, member.Key, typeof(EmailAddressSurfaceController), nameof(UpdateEmailAddress));
                }
                TempData["Success"] = true;
                return RedirectToCurrentUmbracoPage();
            }
            else
            {
                return CurrentUmbracoPage();
            }
        }

        /// <summary>
        /// Gets the authority segment of the request URL
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1055:Uri return values should not be strings", Justification = "It's only a partial URI")]
        protected virtual string GetRequestUrlAuthority() => Request.Url.Host == "localhost" ? Request.Url.Authority : "www.stoolball.org.uk";

        /// <summary>
        /// Calls the base <see cref="SurfaceController.RedirectToCurrentUmbracoPage" /> in a way which can be overridden for testing
        /// </summary>
        protected new virtual RedirectToUmbracoPageResult RedirectToCurrentUmbracoPage() => base.RedirectToCurrentUmbracoPage();
    }
}