using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;
using Umbraco.Web.Controllers;
using Umbraco.Core.Persistence;
using Umbraco.Web;
using Umbraco.Core.Services;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Stoolball.Web.Email;
using System.Globalization;
using Stoolball.Security;

namespace Stoolball.Web.Account
{
    public class CreateMemberSurfaceController : UmbRegisterController
    {
        private readonly IEmailFormatter _emailFormatter;
        private readonly IEmailSender _emailSender;
        private readonly IVerificationToken _verificationToken;

        public CreateMemberSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory databaseFactory, ServiceContext services, AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, IEmailFormatter emailFormatter, IEmailSender emailSender, IVerificationToken verificationToken)
            : base(umbracoContextAccessor, databaseFactory, services, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _emailFormatter = emailFormatter;
            _emailSender = emailSender;
            _verificationToken = verificationToken;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        public ActionResult CreateMember([Bind(Prefix = "createMemberModel")]RegisterModel model)
        {
            if (ModelState.IsValid == false || model == null)
            {
                return CurrentUmbracoPage();
            }

            // Don't login if creating the member succeeds, because we're going to revert approval and ask for validation
            model.LoginOnSuccess = false;
            var baseResult = CreateMemberInUmbraco(model);

            // Put the entered email address in TempData so that it can be accessed in the view
            TempData["Email"] = model.Email;

            // The base Umbraco method populates this TempData key with a boolean we can use to check the result
            var memberCreated = TempData.ContainsKey("FormSuccess") && Convert.ToBoolean(TempData["FormSuccess"], CultureInfo.InvariantCulture) == true;
            if (memberCreated)
            {
                // Get the newly-created member so that we can set an approval token
                var member = Services.MemberService.GetByEmail(model.Email);

                // Create an account approval token including the id so we can find the member
                var (token, expires) = _verificationToken.TokenFor(member.Id);
                member.SetValue("approvalToken", token);
                member.SetValue("approvalTokenExpires", expires);
                member.SetValue("totalLogins", 0);
                member.IsApproved = false;

                Services.MemberService.Save(member);

                // Add to a default group which can be used to assign permissions to all members
                Services.MemberService.AssignRole(member.Id, "All Members");

                // Send the approval validation email
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

                return RedirectToCurrentUmbracoPage();
            }
            else
            {
                // Don't expose that an email address is in use already.
                // For security send an email with a link to reset their password.
                // See https://www.troyhunt.com/everything-you-ever-wanted-to-know/
                var errorMessage = ModelState.Values.Where(x => x.Errors.Count > 0).Select(x => x.Errors[0].ErrorMessage).FirstOrDefault();
                if (errorMessage == "A member with this username already exists.")
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

                    // Send back the same status regardless for security
                    TempData["FormSuccess"] = true;
                    return RedirectToCurrentUmbracoPage();
                }
                else
                {
                    // Some other validation error, such as password not strong enough
                    TempData["Error"] = errorMessage;
                    return baseResult;
                }
            }
        }

        /// <summary>
        /// Gets the authority segment of the request URL
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1055:Uri return values should not be strings", Justification = "It's only a partial URI")]
        protected virtual string GetRequestUrlAuthority() => Request.Url.Authority;

        /// <summary>
        /// Calls the base <see cref="SurfaceController.RedirectToCurrentUmbracoPage" /> in a way which can be overridden for testing
        /// </summary>
        protected new virtual RedirectToUmbracoPageResult RedirectToCurrentUmbracoPage() => base.RedirectToCurrentUmbracoPage();

        /// <summary>
        /// Calls the base <see cref="HandleRegisterMember" /> in a way which can be overridden for testing
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        protected virtual ActionResult CreateMemberInUmbraco(RegisterModel model) => base.HandleRegisterMember(model);
    }
}
