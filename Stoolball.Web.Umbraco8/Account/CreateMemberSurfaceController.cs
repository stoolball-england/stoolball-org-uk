using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using Stoolball.Email;
using Stoolball.Security;
using Stoolball.Web.Email;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Persistence.Querying;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Controllers;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;
using static Stoolball.Constants;

namespace Stoolball.Web.Account
{
    public class CreateMemberSurfaceController : UmbRegisterController
    {
        private readonly ICreateMemberExecuter _createMemberExecuter;
        private readonly IEmailFormatter _emailFormatter;
        private readonly IEmailSender _emailSender;
        private readonly IVerificationToken _verificationToken;
        private const string UMBRACO_ERROR_IF_MEMBER_EXISTS = "A member with this username already exists.";

        public CreateMemberSurfaceController(IUmbracoContextAccessor umbracoContextAccessor,
            IUmbracoDatabaseFactory databaseFactory,
            ServiceContext services,
            AppCaches appCaches,
            ILogger logger,
            IProfilingLogger profilingLogger,
            UmbracoHelper umbracoHelper,
            ICreateMemberExecuter createMemberExecuter,
            IEmailFormatter emailFormatter,
            IEmailSender emailSender,
            IVerificationToken verificationToken)
            : base(umbracoContextAccessor, databaseFactory, services, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _createMemberExecuter = createMemberExecuter ?? throw new ArgumentNullException(nameof(createMemberExecuter));
            _emailFormatter = emailFormatter ?? throw new ArgumentNullException(nameof(emailFormatter));
            _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
            _verificationToken = verificationToken ?? throw new ArgumentNullException(nameof(verificationToken));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public ActionResult CreateMember([Bind(Prefix = "createMemberModel")] RegisterModel model)
        {
            if (ModelState.IsValid == false || model == null)
            {
                return CurrentUmbracoPage();
            }

            // Put the entered email address in TempData so that it can be accessed in the view
            TempData["Email"] = model.Email;

            // Umbraco code will check if a member already exists, but we also need to check if a member exists
            // that has a different email but has requested this one in the last 24 hours. Treat that the same as if
            // they'd approved the request and that was already their registered email address, and don't try to
            // create a member as that would succeed.
            ActionResult baseResult;
            if (AnotherMemberHasRequestedThisEmailAddress(model.Email))
            {
                baseResult = WhatUmbracoDoesIfAMemberExistsWithThisEmail();
            }
            else
            {
                // Don't login if creating the member succeeds, because we're going to revert approval and ask for validation
                model.LoginOnSuccess = false;
                baseResult = _createMemberExecuter.CreateMember(base.HandleRegisterMember, model);
            }

            if (NewMemberWasCreated())
            {
                // Get the newly-created member so that we can set an approval token
                var member = Services.MemberService.GetByEmail(model.Email);

                // Create an account approval token including the id so we can find the member
                var token = NewMemberMustAwaitActivation(member);

                // Add to a default group which can be used to assign permissions to all members
                Services.MemberService.AssignRole(member.Id, Groups.AllMembers);

                SendActivateNewMemberEmail(model, token);

                Logger.Info(typeof(CreateMemberSurfaceController), LoggingTemplates.CreateMember, member.Username, member.Key, typeof(CreateMemberSurfaceController), nameof(CreateMember));

                return RedirectToCurrentUmbracoPage();
            }
            else
            {
                // Don't expose that an email address is in use already.
                // For security send an email with a link to reset their password.
                // See https://www.troyhunt.com/everything-you-ever-wanted-to-know/
                var errorMessage = ModelState.Values.Where(x => x.Errors.Count > 0).Select(x => x.Errors[0].ErrorMessage).FirstOrDefault();
                if (errorMessage == UMBRACO_ERROR_IF_MEMBER_EXISTS)
                {
                    SendMemberAlreadyExistsEmail(model);

                    // Send back the same status regardless for security
                    TempData["FormSuccess"] = true;
                    return RedirectToCurrentUmbracoPage();
                }
                else
                {
                    // Some other validation error, such as password not strong enough
                    ModelState.AddModelError(string.Empty, errorMessage);
                    return baseResult;
                }
            }
        }

        private void SendMemberAlreadyExistsEmail(RegisterModel model)
        {
            // Send the 'member already exists' email
            var (subject, body) = _emailFormatter.FormatEmailContent(CurrentPage.Value<string>("memberExistsSubject"),
                CurrentPage.Value<string>("memberExistsBody"),
                new Dictionary<string, string>
                {
                            {"name", model.Name},
                            {"email", model.Email},
                            {"domain", GetRequestUrlAuthority()}
                });
            _emailSender.SendEmail(model.Email, subject, body);
        }

        private ActionResult WhatUmbracoDoesIfAMemberExistsWithThisEmail()
        {
            ActionResult baseResult;
            TempData["FormSuccess"] = false;
            ModelState.AddModelError(string.Empty, UMBRACO_ERROR_IF_MEMBER_EXISTS);
            baseResult = new HttpStatusCodeResult(200);
            return baseResult;
        }

        private bool AnotherMemberHasRequestedThisEmailAddress(string email)
        {
#pragma warning disable CA1308 // Normalize strings to uppercase
            var membersRequestingThisEmail = Services.MemberService.GetMembersByPropertyValue("requestedEmail", email?.Trim().ToLowerInvariant(), StringPropertyMatchType.Exact);
#pragma warning restore CA1308 // Normalize strings to uppercase
            return membersRequestingThisEmail.Any(x => !_verificationToken.HasExpired(x.GetValue<DateTime>("requestedEmailTokenExpires")));
        }

        private bool NewMemberWasCreated()
        {

            // The base Umbraco method populates this TempData key with a boolean we can use to check the result
            return TempData.ContainsKey("FormSuccess") && Convert.ToBoolean(TempData["FormSuccess"], CultureInfo.InvariantCulture) == true;
        }

        private void SendActivateNewMemberEmail(RegisterModel model, string token)
        {
            var (subject, body) = _emailFormatter.FormatEmailContent(CurrentPage.Value<string>("approveMemberSubject"),
                CurrentPage.Value<string>("approveMemberBody"),
                new Dictionary<string, string>
                {
                        {"name", model.Name},
                        {"email", model.Email},
                        {"token", token},
                        {"domain", GetRequestUrlAuthority()}
                });
            _emailSender.SendEmail(model.Email, subject, body);
        }

        private string NewMemberMustAwaitActivation(Umbraco.Core.Models.IMember member)
        {
            var (token, expires) = _verificationToken.TokenFor(member.Id);
            member.SetValue("approvalToken", token);
            member.SetValue("approvalTokenExpires", expires);
            member.SetValue("totalLogins", 0);
            member.IsApproved = false;

            Services.MemberService.Save(member);
            return token;
        }

        /// <summary>
        /// Gets the authority segment of the request URL
        /// </summary>
        /// <returns></returns>
        private string GetRequestUrlAuthority() => Request.Url.Host == "localhost" ? Request.Url.Authority : "www.stoolball.org.uk";

        /// <summary>
        /// Calls the base <see cref="SurfaceController.RedirectToCurrentUmbracoPage" /> in a way which can be overridden for testing
        /// </summary>
        protected new virtual RedirectToUmbracoPageResult RedirectToCurrentUmbracoPage() => base.RedirectToCurrentUmbracoPage();
    }
}
