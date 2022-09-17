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
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Website.Controllers;
using Umbraco.Extensions;
using static Stoolball.Constants;

namespace Stoolball.Web.Account
{
    public class EmailAddressSurfaceController : SurfaceController
    {
        private readonly IVariationContextAccessor _variationContextAccessor;
        private readonly ILogger<EmailAddressSurfaceController> _logger;
        private readonly IMemberManager _memberManager;
        private readonly IEmailFormatter _emailFormatter;
        private readonly IEmailSender _emailSender;
        private readonly IVerificationToken _verificationToken;

        public EmailAddressSurfaceController(IUmbracoContextAccessor umbracoContextAccessor,
            IVariationContextAccessor variationContextAccessor,
            IUmbracoDatabaseFactory databaseFactory,
            ServiceContext services,
            AppCaches appCaches,
            ILogger<EmailAddressSurfaceController> logger,
            IProfilingLogger profilingLogger,
            IPublishedUrlProvider publishedUrlProvider,
            IMemberManager memberManager,
            IEmailFormatter emailFormatter,
            IEmailSender emailSender,
            IVerificationToken verificationToken)
        : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
        {
            _variationContextAccessor = variationContextAccessor ?? throw new ArgumentNullException(nameof(variationContextAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _memberManager = memberManager ?? throw new System.ArgumentNullException(nameof(memberManager));
            _emailFormatter = emailFormatter ?? throw new System.ArgumentNullException(nameof(emailFormatter));
            _emailSender = emailSender ?? throw new System.ArgumentNullException(nameof(emailSender));
            _verificationToken = verificationToken ?? throw new System.ArgumentNullException(nameof(verificationToken));
        }

        [HttpPost]
        [ValidateAntiForgeryToken()]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<IActionResult> UpdateEmailAddress([Bind(Prefix = "formData")] EmailAddressFormData? postedData)
        {
            if (postedData == null)
            {
                return new StatusCodeResult(400);
            }

            var member = await _memberManager.GetCurrentMemberAsync();
            if (!await _memberManager.CheckPasswordAsync(member, postedData?.Password?.Trim()))
            {
                ModelState.AddModelError("formData." + nameof(postedData.Password), "Your password is incorrect or your account is locked.");
            }

            if (ModelState.IsValid && postedData != null)
            {

                // Create an requested email token including the id so we can find the member.

                // Do this even if the email is already in use, to avoid an enumeration risk where the
                // result of the request is displayed in the UI differently depending on whether a member exists.

                // It's OK to save the token for an invalid situation as the token will not be sent to the requester,
                // and in any case the check for an existing member would happen again at confirmation time.
                var token = SaveConfirmationTokenForMember(postedData.Requested!, Convert.ToInt32(member.Id));

                // Check whether the requested email already belongs to another account
                var alreadyTaken = Services.MemberService.GetByEmail(postedData.Requested?.Trim());
                if (alreadyTaken != null && alreadyTaken.Key != member.Key)
                {
                    // Send the address already in use email
                    var publishedValueFallback = new PublishedValueFallback(Services, _variationContextAccessor);
                    var (subject, body) = _emailFormatter.FormatEmailContent(
                        CurrentPage.Value<string>(publishedValueFallback, "emailTakenSubject"),
                        CurrentPage.Value<string>(publishedValueFallback, "emailTakenBody"),
                        new Dictionary<string, string>
                        {
                        {"name", alreadyTaken.Name},
                        {"email", postedData.Requested!},
                        {"domain", GetRequestUrlAuthority()}
                        });
                    await _emailSender.SendAsync(new EmailMessage(null, postedData.Requested?.Trim(), subject, body, true), null);

                    _logger.Info(LoggingTemplates.MemberRequestedEmailAddressAlreadyInUse, member.Name, member.Key, typeof(EmailAddressSurfaceController), nameof(UpdateEmailAddress));
                }
                else
                {

                    // Send the token by email
                    var publishedValueFallback = new PublishedValueFallback(Services, _variationContextAccessor);
                    var (subject, body) = _emailFormatter.FormatEmailContent(
                        CurrentPage.Value<string>(publishedValueFallback, "confirmEmailSubject"),
                        CurrentPage.Value<string>(publishedValueFallback, "confirmEmailBody"),
                        new Dictionary<string, string>
                        {
                        {"name", member.Name},
                        {"email", postedData.Requested!},
                        {"domain", GetRequestUrlAuthority()},
                        {"token", token }
                        });
                    await _emailSender.SendAsync(new EmailMessage(null, postedData.Requested?.Trim(), subject, body, true), null);

                    _logger.Info(LoggingTemplates.MemberRequestedEmailAddress, member.Name, member.Key, typeof(EmailAddressSurfaceController), nameof(UpdateEmailAddress));
                }
                TempData["Success"] = true;
                return RedirectToCurrentUmbracoPage();
            }
            else
            {
                var model = new EmailAddress(CurrentPage, new PublishedValueFallback(Services, _variationContextAccessor)) { FormData = postedData! };
                model.Metadata = new ViewMetadata
                {
                    PageTitle = model.Name,
                    Description = model.Description
                };
                return View("EmailAddress", model);
            }
        }

        private string SaveConfirmationTokenForMember(string email, int memberId)
        {
            var (token, expires) = _verificationToken.TokenFor(memberId);
            var editableMember = Services.MemberService.GetById(memberId);
#pragma warning disable CA1308 // Normalize strings to uppercase
            editableMember.SetValue("requestedEmail", email?.Trim().ToLowerInvariant());
#pragma warning restore CA1308 // Normalize strings to uppercase
            editableMember.SetValue("requestedEmailToken", token);
            editableMember.SetValue("requestedEmailTokenExpires", expires);
            Services.MemberService.Save(editableMember);
            return token;
        }

        /// <summary>
        /// Gets the authority segment of the request URL
        /// </summary>
        /// <returns></returns>
        protected virtual string GetRequestUrlAuthority() => Request.Host.Host == "localhost" ? Request.Host.Value : "www.stoolball.org.uk";
    }
}
