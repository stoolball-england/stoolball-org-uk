using System;
using System.Web.Mvc;
using Stoolball.Metadata;
using Stoolball.Security;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;
using Umbraco.Web.PublishedModels;
using static Stoolball.Constants;

namespace Stoolball.Web.Account
{
    public class ResetPasswordSurfaceController : SurfaceController
    {
        private readonly ILoginMemberWrapper _loginMemberWrapper;
        private readonly IVerificationToken _verificationToken;

        public ResetPasswordSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory databaseFactory, ServiceContext services,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, ILoginMemberWrapper loginMemberWrapper, IVerificationToken verificationToken)
            : base(umbracoContextAccessor, databaseFactory, services, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _loginMemberWrapper = loginMemberWrapper ?? throw new ArgumentNullException(nameof(loginMemberWrapper));
            _verificationToken = verificationToken ?? throw new ArgumentNullException(nameof(verificationToken));
        }

        [HttpPost]
        [ValidateAntiForgeryToken()]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public ActionResult UpdatePassword([Bind(Prefix = "resetPasswordUpdate")] ResetPasswordFormData model)
        {
            var contentModel = new ResetPassword(CurrentPage);
            contentModel.Metadata = new ViewMetadata
            {
                PageTitle = contentModel.Name,
                Description = contentModel.Description
            };

            // Assume the token is valid and this will be checked later
            contentModel.PasswordResetToken = Request.QueryString["token"];

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

                        Logger.Info(typeof(ResetPasswordSurfaceController), LoggingTemplates.MemberPasswordReset, member.Username, member.Key, typeof(ResetPasswordSurfaceController), nameof(UpdatePassword));

                        // They obviously wanted to login, so be helpful and do it, unless they're blocked
                        if (!member.GetValue<bool>("blockLogin"))
                        {
                            _loginMemberWrapper.LoginMember(member.Username, model.NewPassword);
                        }

                        // Redirect because the login doesn't update the thread identity
                        return RedirectToCurrentUmbracoPage($"token={model.PasswordResetToken}&successful=yes");
                    }
                    else
                    {
                        Logger.Info(typeof(ResetPasswordSurfaceController), LoggingTemplates.MemberPasswordResetTokenInvalid, model.PasswordResetToken, typeof(ResetPasswordSurfaceController), nameof(UpdatePassword));
                        contentModel.ShowPasswordResetSuccessful = false;
                        return View("ResetPasswordComplete", contentModel);
                    }
                }
                else
                {
                    Logger.Info(typeof(ResetPasswordSurfaceController), LoggingTemplates.MemberPasswordResetTokenInvalid, model.PasswordResetToken, typeof(ResetPasswordSurfaceController), nameof(UpdatePassword));
                    contentModel.ShowPasswordResetSuccessful = false;
                    return View("ResetPasswordComplete", contentModel);
                }
            }
            catch (FormatException)
            {
                Logger.Info(typeof(ResetPasswordSurfaceController), LoggingTemplates.MemberPasswordResetTokenInvalid, model.PasswordResetToken, typeof(ResetPasswordSurfaceController), nameof(UpdatePassword));
                contentModel.ShowPasswordResetSuccessful = false;
                return View("ResetPasswordComplete", contentModel);
            }
        }

        /// <summary>
        /// Calls the base <see cref="SurfaceController.RedirectToCurrentUmbracoPage" /> in a way which can be overridden for testing
        /// </summary>
        protected new virtual RedirectToUmbracoPageResult RedirectToCurrentUmbracoPage(string queryString) => base.RedirectToCurrentUmbracoPage(queryString);
    }
}