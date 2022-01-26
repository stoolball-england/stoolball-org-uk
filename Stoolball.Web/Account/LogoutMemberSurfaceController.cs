using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Website.ActionResults;
using Umbraco.Cms.Web.Website.Controllers;

namespace Stoolball.Web.Account
{
    public class LogoutMemberSurfaceController : SurfaceController
    {
        private readonly ILogoutMemberWrapper _logoutMemberWrapper;

        public LogoutMemberSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, ILogoutMemberWrapper logoutMemberWrapper) :
            base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _logoutMemberWrapper = logoutMemberWrapper ?? throw new System.ArgumentNullException(nameof(logoutMemberWrapper));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy]
        public async Task<IActionResult> HandleLogout()
        {
            if (User.Identity?.IsAuthenticated ?? false)
            {
                await _logoutMemberWrapper.LogoutMember();
            }
            return RedirectToCurrentUmbracoPage();
        }

        /// <summary>
        /// Calls the base <see cref="SurfaceController.RedirectToCurrentUmbracoPage" /> in a way which can be overridden for testing
        /// </summary>
        protected new virtual RedirectToUmbracoPageResult RedirectToCurrentUmbracoPage() => base.RedirectToCurrentUmbracoPage();
    }
}