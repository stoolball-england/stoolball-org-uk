using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Email;
using Stoolball.Logging;
using Stoolball.Metadata;
using Stoolball.Security;
using Stoolball.Web.Models;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Mail;
using Umbraco.Cms.Core.Models.Email;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Website.Controllers;
using Umbraco.Extensions;
using static Stoolball.Constants;

namespace Stoolball.Web.Account
{
    public class ResetPasswordRequestSurfaceController : SurfaceController
    {
        private readonly IVariationContextAccessor _variationContextAccessor;
        private readonly ILogger<ResetPasswordRequestSurfaceController> _logger;
        private readonly IEmailFormatter _emailFormatter;
        private readonly IEmailSender _emailSender;
        private readonly IVerificationToken _verificationToken;

        public ResetPasswordRequestSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IVariationContextAccessor variationContextAccessor,
            IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext, AppCaches appCaches,
            ILogger<ResetPasswordRequestSurfaceController> logger, IProfilingLogger profilingLogger,
            IPublishedUrlProvider publishedUrlProvider, IEmailFormatter emailFormatter, IEmailSender emailSender, IVerificationToken verificationToken)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _variationContextAccessor = variationContextAccessor ?? throw new ArgumentNullException(nameof(variationContextAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _emailFormatter = emailFormatter ?? throw new ArgumentNullException(nameof(emailFormatter));
            _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
            _verificationToken = verificationToken ?? throw new ArgumentNullException(nameof(verificationToken));
        }

        [HttpPost]
        [ValidateAntiForgeryToken()]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<IActionResult> RequestPasswordReset([Bind(Prefix = "resetPasswordRequest")] ResetPasswordRequestFormData? model)
        {
            var contentModel = new ResetPassword(CurrentPage, new PublishedValueFallback(Services, _variationContextAccessor));
            contentModel.Metadata = new ViewMetadata
            {
                PageTitle = contentModel.Name,
                Description = contentModel.Description
            };
            contentModel.Email = model?.Email!;

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
                var publishedValueFallback = new PublishedValueFallback(Services, _variationContextAccessor);
                var (subject, body) = _emailFormatter.FormatEmailContent(CurrentPage.Value<string>(publishedValueFallback, emailSubjectField),
                    CurrentPage.Value<string>(publishedValueFallback, emailBodyField),
                    new Dictionary<string, string>
                    {
                        {"name", member.Name},
                        {"email", model.Email!},
                        {"token", token},
                        {"domain", GetRequestUrlAuthority()}
                    });
                await _emailSender.SendAsync(new EmailMessage(null, model.Email, subject, body, true), null);

                _logger.Info(LoggingTemplates.MemberPasswordResetRequested, member.Username, member.Key, typeof(ResetPasswordRequestSurfaceController), nameof(RequestPasswordReset));

                contentModel.ShowPasswordResetRequested = true;
                return View("ResetPasswordRequest", contentModel);
            }
            else
            {
                // Same result as if a member was found, since password reset should not reveal a valid email address
                // However we can prompt them to create an account. Since it sends an email either way this also guards
                // against detecting the result by timing the response.
                var publishedValueFallback = new PublishedValueFallback(Services, _variationContextAccessor);
                var (sender, body) = _emailFormatter.FormatEmailContent(CurrentPage.Value<string>(publishedValueFallback, "createMemberSubject"),
                    CurrentPage.Value<string>(publishedValueFallback, "createMemberBody"),
                    new Dictionary<string, string>
                    {
                        {"email", model.Email!},
                        {"domain", GetRequestUrlAuthority()}
                    });
                await _emailSender.SendAsync(new EmailMessage(null, model.Email, sender, body, true), null);

                contentModel.ShowPasswordResetRequested = true;
                return View("ResetPasswordRequest", contentModel);
            }

        }

        /// <summary>
        /// Gets the authority segment of the request URL
        /// </summary>
        /// <returns></returns>
        private string GetRequestUrlAuthority() => Request.Host.Host == "localhost" ? Request.Host.Value : "www.stoolball.org.uk";
    }
}