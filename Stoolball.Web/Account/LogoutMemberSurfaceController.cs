using System.Web.Mvc;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;

namespace Stoolball.Web.Account
{
    public class LogoutMemberSurfaceController : SurfaceController
    {
        private readonly ILogoutMemberWrapper _logoutMemberWrapper;

        public LogoutMemberSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, ILogoutMemberWrapper logoutMemberWrapper) :
            base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _logoutMemberWrapper = logoutMemberWrapper ?? throw new System.ArgumentNullException(nameof(logoutMemberWrapper));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy]
        public ActionResult HandleLogout()
        {
            if (Umbraco.MemberIsLoggedOn())
            {
                _logoutMemberWrapper.LogoutMember();
            }
            return RedirectToCurrentUmbracoPage();
        }

        /// <summary>
        /// Calls the base <see cref="SurfaceController.RedirectToCurrentUmbracoPage" /> in a way which can be overridden for testing
        /// </summary>
        protected new virtual RedirectToUmbracoPageResult RedirectToCurrentUmbracoPage() => base.RedirectToCurrentUmbracoPage();
    }
}