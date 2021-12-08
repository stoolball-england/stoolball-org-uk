using System.Web.Mvc;
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
    public class PersonalDetailsSurfaceController : SurfaceController
    {
        public PersonalDetailsSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory databaseFactory, ServiceContext services, AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper)
        : base(umbracoContextAccessor, databaseFactory, services, appCaches, logger, profilingLogger, umbracoHelper)
        {
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public ActionResult UpdatePersonalDetails([Bind(Prefix = "formData")] PersonalDetailsFormData model)
        {
            if (ModelState.IsValid && model != null)
            {
                var member = Members.GetCurrentMember();

                var editableMember = Services.MemberService.GetById(member.Id);
                editableMember.Name = model.Name;

                Services.MemberService.Save(editableMember);

                Logger.Info(typeof(PersonalDetailsSurfaceController), LoggingTemplates.MemberPersonalDetailsUpdated, member.Name, member.Key, typeof(PersonalDetailsSurfaceController), nameof(UpdatePersonalDetails));

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
