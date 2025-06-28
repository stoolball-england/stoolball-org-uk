using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Stoolball.Email;
using Stoolball.Logging;
using Stoolball.Security;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Mail;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Email;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Persistence.Querying;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Common.Security;
using Umbraco.Cms.Web.Website.Controllers;
using Umbraco.Extensions;
using static Stoolball.Constants;

namespace Stoolball.Web.Account
{
    public class CreateMemberSurfaceController : UmbRegisterController
    {
        private readonly ILogger<CreateMemberSurfaceController> _logger;
        private readonly IMemberService _memberService;
        private readonly IMemberSignInManager _memberSignInManager;
        private readonly ICreateMemberExecuter _createMemberExecuter;
        private readonly IEmailFormatter _emailFormatter;
        private readonly IEmailSender _emailSender;
        private readonly IVerificationToken _verificationToken;
        private readonly IVariationContextAccessor _variationContextAccessor;
        private const string UMBRACO_ERROR_IF_MEMBER_EXISTS = "Username '{0}' is already taken";

        public CreateMemberSurfaceController(IUmbracoContextAccessor umbracoContextAccessor,
            IVariationContextAccessor variationContextAccessor,
            IUmbracoDatabaseFactory databaseFactory,
            ServiceContext services,
            AppCaches appCaches,
            ILogger<CreateMemberSurfaceController> logger,
            IProfilingLogger profilingLogger,
            IPublishedUrlProvider publishedUrlProvider,
            IMemberManager memberManager,
            IMemberService memberService,
            IMemberSignInManager memberSignInManager,
            ICoreScopeProvider scopeProvider,
            ICreateMemberExecuter createMemberExecuter,
            IEmailFormatter emailFormatter,
            IEmailSender emailSender,
            IVerificationToken verificationToken)
            : base(memberManager, memberService, umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider, memberSignInManager, scopeProvider)
        {
            _variationContextAccessor = variationContextAccessor ?? throw new ArgumentNullException(nameof(variationContextAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _memberService = memberService ?? throw new ArgumentNullException(nameof(memberService));
            _memberSignInManager = memberSignInManager ?? throw new ArgumentNullException(nameof(memberSignInManager));
            _createMemberExecuter = createMemberExecuter ?? throw new ArgumentNullException(nameof(createMemberExecuter));
            _emailFormatter = emailFormatter ?? throw new ArgumentNullException(nameof(emailFormatter));
            _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
            _verificationToken = verificationToken ?? throw new ArgumentNullException(nameof(verificationToken));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<IActionResult> CreateMember([Bind(Prefix = "createMemberModel")] CreateMemberFormData model)
        {
            if (ModelState.ContainsKey("createMemberModel.ConfirmPassword"))
            {
                ModelState["createMemberModel.ConfirmPassword"]!.ValidationState = ModelValidationState.Skipped;
            }
            if (ModelState.IsValid == false || model is null)
            {
                return CurrentUmbracoPage();
            }

            // Put the entered email address in TempData so that it can be accessed in the view
            TempData["Email"] = model.Email;

            // Umbraco code will check if a member already exists, but we also need to check if a member exists
            // that has a different email but has requested this one in the last 24 hours. Treat that the same as if
            // they'd approved the request and that was already their registered email address, and don't try to
            // create a member as that would succeed.
            IActionResult baseResult;

            if (AnotherMemberHasRequestedThisEmailAddress(model.Email!))
            {
                baseResult = WhatUmbracoDoesIfAMemberExistsWithThisEmail(model.Email!);
            }
            else
            {
                baseResult = await _createMemberExecuter.CreateMember(base.HandleRegisterMember, model);
            }

            if (NewMemberWasCreated())
            {
                // Get the newly-created member so that we can set an approval token
                var member = _memberService.GetByEmail(model.Email!);
                if (member is null) { throw new InvalidOperationException($"Member for {model.Email} was created but could not be found."); }

                // Create an account approval token including the id so we can find the member
                var token = await NewMemberMustAwaitActivation(member);

                // Add to a default group which can be used to assign permissions to all members
                _memberService.AssignRole(member.Id, Groups.AllMembers);

                await SendActivateNewMemberEmail(model, token);

                _logger.Info(LoggingTemplates.CreateMember, member.Username, member.Key, typeof(CreateMemberSurfaceController), nameof(CreateMember));

                return RedirectToCurrentUmbracoPage();
            }
            else
            {
                // Don't expose that an email address is in use already.
                // For security send an email with a link to reset their password.
                // See https://www.troyhunt.com/everything-you-ever-wanted-to-know/
                var errorMessage = ModelState.Values.Where(x => x.Errors.Count > 0).Select(x => x.Errors[0].ErrorMessage).FirstOrDefault();
                if (errorMessage == string.Format(UMBRACO_ERROR_IF_MEMBER_EXISTS, model.Email))
                {
                    await SendMemberAlreadyExistsEmail(model.Name, model.Email!);

                    _logger.Info(LoggingTemplates.MemberAlreadyExists, model.Email?.Length > 8 ? $"********{model.Email.Substring(8)}" : "********", typeof(CreateMemberSurfaceController), nameof(CreateMember));

                    // Send back the same status regardless for security
                    TempData["FormSuccess"] = true;
                    return RedirectToCurrentUmbracoPage();
                }
                else
                {
                    // Some other validation error, such as password not strong enough
                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        ModelState.AddModelError(string.Empty, errorMessage);
                    }
                    return baseResult;
                }
            }
        }

        private async Task SendMemberAlreadyExistsEmail(string? name, string email)
        {
            // Send the 'member already exists' email
            var publishedValueFallback = new PublishedValueFallback(Services, _variationContextAccessor);
            var (subject, body) = _emailFormatter.FormatEmailContent(CurrentPage.Value<string>(publishedValueFallback, "memberExistsSubject"),
                CurrentPage.Value<string>(publishedValueFallback, "memberExistsBody"),
                new Dictionary<string, string>
                {
                            {"name", name},
                            {"email", email},
                            {"domain", GetRequestUrlAuthority()}
                });

            await _emailSender.SendAsync(new EmailMessage(null, email, subject, body, true), null);
        }

        private IActionResult WhatUmbracoDoesIfAMemberExistsWithThisEmail(string email)
        {
            IActionResult baseResult;
            TempData["FormSuccess"] = false;
            ModelState.AddModelError(string.Empty, string.Format(UMBRACO_ERROR_IF_MEMBER_EXISTS, email));
            baseResult = new StatusCodeResult(200);
            return baseResult;
        }

        private bool AnotherMemberHasRequestedThisEmailAddress(string email)
        {
            var membersRequestingThisEmail = _memberService.GetMembersByPropertyValue("requestedEmail", email.Trim().ToLowerInvariant(), StringPropertyMatchType.Exact);
            return membersRequestingThisEmail.Any(x => !_verificationToken.HasExpired(x.GetValue<DateTime>("requestedEmailTokenExpires")));
        }

        private bool NewMemberWasCreated()
        {

            // The base Umbraco method populates this TempData key with a boolean we can use to check the result
            return TempData.ContainsKey("FormSuccess") && Convert.ToBoolean(TempData["FormSuccess"], CultureInfo.InvariantCulture) == true;
        }

        private async Task SendActivateNewMemberEmail(CreateMemberFormData model, string token)
        {
            var publishedValueFallback = new PublishedValueFallback(Services, _variationContextAccessor);
            var (subject, body) = _emailFormatter.FormatEmailContent(CurrentPage.Value<string>(publishedValueFallback, "approveMemberSubject"),
                CurrentPage.Value<string>(publishedValueFallback, "approveMemberBody"),
                new Dictionary<string, string>
                {
                        {"name", model.Name},
                        {"email", model.Email},
                        {"token", token},
                        {"domain", GetRequestUrlAuthority()}
                });
            await _emailSender.SendAsync(new EmailMessage(null, model.Email, subject, body, true), null);
        }

        private async Task<string> NewMemberMustAwaitActivation(IMember member)
        {
            // Umbraco signed them in but we don't want that, because we're going to revert approval and ask for validation
            await _memberSignInManager.SignOutAsync();

            var (token, expires) = _verificationToken.TokenFor(member.Id);
            member.SetValue("approvalToken", token);
            member.SetValue("approvalTokenExpires", expires);
            member.SetValue("totalLogins", 0);
            member.IsApproved = false;

            _memberService.Save(member);
            return token;
        }

        /// <summary>
        /// Gets the authority segment of the request URL
        /// </summary>
        /// <returns></returns>
        private string GetRequestUrlAuthority() => Request.Host.Host == "localhost" ? Request.Host.Value : "www.stoolball.org.uk";
    }
}
