using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Stoolball.Metadata;
using Stoolball.Security;
using Stoolball.Web.Email;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;
using Umbraco.Web.PublishedModels;
using Umbraco.Web.Security;
using static Stoolball.Constants;

namespace Stoolball.Web.Account
{
    public class ResetPasswordSurfaceController : SurfaceController
    {
        private readonly MembershipHelper _membershipHelper;
        private readonly IEmailFormatter _emailFormatter;
        private readonly IEmailSender _emailSender;
        private readonly IVerificationToken _verificationToken;

        public ResetPasswordSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory databaseFactory, ServiceContext services,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, MembershipHelper membershipHelper, IEmailFormatter emailFormatter, IEmailSender emailSender, IVerificationToken verificationToken)
            : base(umbracoContextAccessor, databaseFactory, services, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _membershipHelper = membershipHelper ?? throw new ArgumentNullException(nameof(membershipHelper));
            _emailFormatter = emailFormatter ?? throw new ArgumentNullException(nameof(emailFormatter));
            _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
            _verificationToken = verificationToken ?? throw new ArgumentNullException(nameof(verificationToken));
        }

        [HttpPost]
        [ValidateAntiForgeryToken()]
        [ContentSecurityPolicy(Forms = true)]
        public ActionResult RequestPasswordReset([Bind(Prefix = "resetPasswordRequest")] ResetPasswordRequest model)
        {
            var contentModel = new ResetPassword(CurrentPage);
            contentModel.Metadata = new ViewMetadata
            {
                PageTitle = contentModel.Name,
                Description = contentModel.Description
            };
            contentModel.Email = model?.Email;

            if (!ModelState.IsValid || model == null)
            {
                ModelState.AddModelError("Email", "Please enter a valid email address.");
                return View("ResetPasswordRequest", contentModel);
            }

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

                Logger.Info(typeof(Umbraco.Core.Security.UmbracoMembershipProviderBase), LoggingTemplates.MemberPasswordResetRequested, member.Username, member.Key, GetType(), nameof(RequestPasswordReset));

                contentModel.ShowPasswordResetRequested = true;
                return View("ResetPasswordRequest", contentModel);
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

                contentModel.ShowPasswordResetRequested = true;
                return View("ResetPasswordRequest", contentModel);
            }

        }

        // Gets the password reset token from the querystring in a way that can be overridden for testing
        protected virtual string ReadPasswordResetToken() => Request.QueryString["token"];

        /// <summary>
        /// Gets the authority segment of the request URL
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1055:Uri return values should not be strings", Justification = "It's only a partial URI")]
        protected virtual string GetRequestUrlAuthority() => Request.Url.Host == "localhost" ? Request.Url.Authority : "www.stoolball.org.uk";

        [HttpPost]
        [ValidateAntiForgeryToken()]
        [ContentSecurityPolicy(Forms = true)]
        public ActionResult UpdatePassword([Bind(Prefix = "resetPasswordUpdate")] ResetPasswordUpdate model)
        {
            var contentModel = new ResetPassword(CurrentPage);
            contentModel.Metadata = new ViewMetadata
            {
                PageTitle = contentModel.Name,
                Description = contentModel.Description
            };

            // Assume the token is valid and this will be checked later
            contentModel.PasswordResetToken = ReadPasswordResetToken();
            contentModel.PasswordResetTokenValid = true;

            if (!ModelState.IsValid || model == null)
            {
                contentModel.ShowPasswordResetSuccessful = false;
                return View("ResetPasswordComplete", contentModel);
            }

            try
            {
                var memberId = _verificationToken.ExtractId(contentModel.PasswordResetToken);

                var memberService = Services.MemberService;
                var member = memberService.GetById(memberId);

                if (member != null)
                {
                    if (member.GetValue<string>("passwordResetToken") == model.PasswordResetToken &&
                        !_verificationToken.HasExpired(member.GetValue<DateTime>("passwordResetTokenExpires")))
                    {
                        // If the user has tried repeatedly they might have locked their account
                        // Remove the lockout and expire the token
                        member.IsLockedOut = false;
                        member.SetValue("passwordResetTokenExpires", _verificationToken.ResetExpiryTo());
                        memberService.Save(member);

                        // Reset the password
                        memberService.SavePassword(member, model.NewPassword);

                        Logger.Info(typeof(Umbraco.Core.Security.UmbracoMembershipProviderBase), LoggingTemplates.MemberPasswordReset, member.Username, member.Key, GetType(), nameof(UpdatePassword));

                        // They obviously wanted to login, so be helpful and do it, unless they're blocked
                        if (!member.GetValue<bool>("blockLogin"))
                        {
                            _membershipHelper.Login(member.Username, model.NewPassword);
                        }

                        // Redirect because the login doesn't update the thread identity
                        return RedirectToCurrentUmbracoPage($"token={model.PasswordResetToken}&successful=yes");
                    }
                    else
                    {
                        Logger.Info(GetType(), $"Password reset token invalid {model.PasswordResetToken}");
                        contentModel.ShowPasswordResetSuccessful = false;
                        return View("ResetPasswordComplete", contentModel);
                    }
                }
                else
                {
                    Logger.Info(GetType(), $"Password reset token invalid {model.PasswordResetToken}");
                    contentModel.ShowPasswordResetSuccessful = false;
                    return View("ResetPasswordComplete", contentModel);
                }
            }
            catch (FormatException)
            {
                Logger.Info(GetType(), $"Password reset token invalid {model.PasswordResetToken}");
                contentModel.ShowPasswordResetSuccessful = false;
                return View("ResetPasswordComplete", contentModel);
            }
        }
    }
}