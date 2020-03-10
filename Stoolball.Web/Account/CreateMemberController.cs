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

namespace Stoolball.Web.Account
{
    public class CreateMemberController : UmbRegisterController
    {
        private readonly IEmailHelper _emailHelper;

        public CreateMemberController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory databaseFactory, ServiceContext services, AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, IEmailHelper emailHelper)
            : base(umbracoContextAccessor, databaseFactory, services, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _emailHelper = emailHelper;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        public ActionResult HandleCreateMember([Bind(Prefix = "createMemberModel")]RegisterModel model)
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

                // Create a GUID for the account approval token, combined with the id so we can find the member
                // Set the expiry to be 24 hours
                string approvalToken = $"{member.Id}-{Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)}";
                member.SetValue("approvalToken", approvalToken);
                member.SetValue("approvalTokenExpiry", CalculateApprovalTokenExpiry());
                member.IsApproved = false;

                Services.MemberService.Save(member);

                // Add to a default group which can be used to assign permissions to all members
                Services.MemberService.AssignRole(member.Id, "All Members");

                // Send the approval validation email
                _emailHelper.SendEmail(model.Email,
                    this.CurrentPage.Value<string>("approveMemberSubject"),
                    this.CurrentPage.Value<string>("approveMemberBody"),
                    new Dictionary<string, string>
                    {
                        {"name", model.Name},
                        {"email", model.Email},
                        {"token", approvalToken},
                        {"domain", GetRequestUrlAuthority()}
                    });

                return RedirectToCurrentUmbracoPage();
            }
            else
            {
                // Don't expose that an email address is in use already.
                // For security send an email with a link to reset their password.
                // See https://www.troyhunt.com/everything-you-ever-wanted-to-know/
                var errorMessage = ModelState.Values.Where(x => x.Errors.Count > 0).Select(x => x.Errors[0].ErrorMessage).First();
                if (errorMessage == "A member with this username already exists.")
                {
                    // Send the 'member already exists' email
                    _emailHelper.SendEmail(model.Email,
                        this.CurrentPage.Value<string>("memberExistsSubject"),
                        this.CurrentPage.Value<string>("memberExistsBody"),
                        new Dictionary<string, string>
                        {
                            {"name", model.Name},
                            {"email", model.Email},
                            {"domain", GetRequestUrlAuthority()}
                        });

                    // Send back the same status regardless for security
                    TempData["FormSuccess"] = true;
                    return CurrentUmbracoPage();
                }
                else
                {
                    return baseResult;
                }
            }
        }

        /// <summary>
        /// Calculate the approval token expiry in a way which can be overridden for testing
        /// </summary>
        /// <returns></returns>
        protected virtual DateTime CalculateApprovalTokenExpiry()
        {
            return DateTime.UtcNow.AddDays(1);
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
