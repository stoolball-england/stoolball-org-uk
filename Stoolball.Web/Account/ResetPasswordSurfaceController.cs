using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Logging;
using Stoolball.Metadata;
using Stoolball.Security;
using Stoolball.Web.Models;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Common.Security;
using Umbraco.Cms.Web.Website.ActionResults;
using Umbraco.Cms.Web.Website.Controllers;
using static Stoolball.Constants;

namespace Stoolball.Web.Account
{
    public class ResetPasswordSurfaceController : SurfaceController
    {
        private readonly IVariationContextAccessor _variationContextAccessor;
        private readonly ILogger<ResetPasswordSurfaceController> _logger;
        private readonly IMemberSignInManager _memberSignInManager;
        private readonly IVerificationToken _verificationToken;
        private readonly IPasswordHasher _passwordHasher;

        public ResetPasswordSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IVariationContextAccessor variationContextAccessor,
            IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext, AppCaches appCaches,
            ILogger<ResetPasswordSurfaceController> logger, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider,
            IMemberSignInManager memberSignInManager, IVerificationToken verificationToken, IPasswordHasher passwordHasher)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _variationContextAccessor = variationContextAccessor ?? throw new ArgumentNullException(nameof(variationContextAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _memberSignInManager = memberSignInManager ?? throw new ArgumentNullException(nameof(memberSignInManager));
            _verificationToken = verificationToken ?? throw new ArgumentNullException(nameof(verificationToken));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<IActionResult> UpdatePassword([Bind(Prefix = "resetPasswordUpdate")] ResetPasswordFormData model)
        {
            var contentModel = new ResetPassword(CurrentPage, new PublishedValueFallback(Services, _variationContextAccessor));
            contentModel.Metadata = new ViewMetadata
            {
                PageTitle = contentModel.Name,
                Description = contentModel.Description
            };

            if (model == null)
            {
                contentModel.ShowPasswordResetSuccessful = false;
                return View("ResetPasswordComplete", contentModel);
            }

            // Assume the token is valid and this will be checked later
            contentModel.PasswordResetToken = Request.Query["token"];

            if (!ModelState.IsValid)
            {
                contentModel.PasswordResetTokenValid = true;
                return View("ResetPassword", contentModel);
            }

            try
            {
                var memberId = _verificationToken.ExtractId(contentModel.PasswordResetToken);

                var memberService = Services.MemberService;
                var member = memberService.GetById(memberId);

                if (member != null)
                {
                    if (member.GetValue<string>("passwordResetToken") == contentModel.PasswordResetToken &&
                        !_verificationToken.HasExpired(member.GetValue<DateTime>("passwordResetTokenExpires")))
                    {
                        // If the user has tried repeatedly they might have locked their account
                        // Remove the lockout, expire the token and reset the password
                        member.IsLockedOut = false;
                        member.SetValue("passwordResetTokenExpires", _verificationToken.ResetExpiryTo());
                        member.RawPasswordValue = _passwordHasher.HashPassword(model.NewPassword);
                        memberService.Save(member);

                        _logger.Info(LoggingTemplates.MemberPasswordReset, member.Username, member.Key, typeof(ResetPasswordSurfaceController), nameof(UpdatePassword));

                        // They obviously wanted to login, so be helpful and do it, unless they're blocked
                        if (!member.GetValue<bool>("blockLogin"))
                        {
                            _ = await _memberSignInManager.PasswordSignInAsync(member.Username, model.NewPassword, false, false);
                        }

                        // Redirect because the login doesn't update the thread identity
                        return RedirectToCurrentUmbracoPage(new QueryString($"?token={contentModel.PasswordResetToken}&successful=yes"));
                    }
                    else
                    {
                        _logger.Info(LoggingTemplates.MemberPasswordResetTokenInvalid, contentModel.PasswordResetToken, typeof(ResetPasswordSurfaceController), nameof(UpdatePassword));
                        contentModel.ShowPasswordResetSuccessful = false;
                        return View("ResetPasswordComplete", contentModel);
                    }
                }
                else
                {
                    _logger.Info(LoggingTemplates.MemberPasswordResetTokenInvalid, contentModel.PasswordResetToken, typeof(ResetPasswordSurfaceController), nameof(UpdatePassword));
                    contentModel.ShowPasswordResetSuccessful = false;
                    return View("ResetPasswordComplete", contentModel);
                }
            }
            catch (FormatException)
            {
                _logger.Info(LoggingTemplates.MemberPasswordResetTokenInvalid, contentModel.PasswordResetToken, typeof(ResetPasswordSurfaceController), nameof(UpdatePassword));
                contentModel.ShowPasswordResetSuccessful = false;
                return View("ResetPasswordComplete", contentModel);
            }
        }

        /// <summary>
        /// Calls the base <see cref="SurfaceController.RedirectToCurrentUmbracoPage" /> in a way which can be overridden for testing
        /// </summary>
        protected new virtual RedirectToUmbracoPageResult RedirectToCurrentUmbracoPage(QueryString queryString) => base.RedirectToCurrentUmbracoPage(queryString);
    }
}