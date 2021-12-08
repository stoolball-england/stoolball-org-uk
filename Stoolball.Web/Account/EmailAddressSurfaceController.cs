using System.Web.Mvc;
using System.Web.Security;
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

        public EmailAddressSurfaceController(IUmbracoContextAccessor umbracoContextAccessor,
            IUmbracoDatabaseFactory databaseFactory,
            ServiceContext services,
            AppCaches appCaches,
            ILogger logger,
            IProfilingLogger profilingLogger,
            UmbracoHelper umbracoHelper,
            MembershipProvider membershipProvider)
        : base(umbracoContextAccessor, databaseFactory, services, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _membershipProvider = membershipProvider ?? throw new System.ArgumentNullException(nameof(membershipProvider));
        }

        [HttpPost]
        [ValidateAntiForgeryToken()]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public ActionResult UpdateEmailAddress([Bind(Prefix = "formData")] EmailAddressFormData model)
        {
            if (model != null && (string.IsNullOrWhiteSpace(model.Password) || !_membershipProvider.ValidateUser(Members.CurrentUserName, model.Password?.Trim())))
            {
                ModelState.AddModelError("formData." + nameof(model.Password), "Your password is incorrect. Enter your current password.");
            }

            if (ModelState.IsValid && model != null)
            {
                var member = Members.GetCurrentMember();

                var editableMember = Services.MemberService.GetById(member.Id);

                Services.MemberService.Save(editableMember);

                Logger.Info(typeof(EmailAddressSurfaceController), LoggingTemplates.MemberEmailAddressRequested, member.Name, member.Key, typeof(EmailAddressSurfaceController), nameof(UpdateEmailAddress));

                TempData["Success"] = true;
                return RedirectToCurrentUmbracoPage();
            }
            else
            {
                return CurrentUmbracoPage();
            }
        }

        /// <summary>
        /// Calls the base <see cref="SurfaceController.RedirectToCurrentUmbracoPage" /> in a way which can be overridden for testing
        /// </summary>
        protected new virtual RedirectToUmbracoPageResult RedirectToCurrentUmbracoPage() => base.RedirectToCurrentUmbracoPage();
    }
}
