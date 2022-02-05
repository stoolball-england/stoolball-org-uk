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
using Umbraco.Cms.Web.Common.Security;
using Umbraco.Cms.Web.Website.ActionResults;
using Umbraco.Cms.Web.Website.Controllers;

namespace Stoolball.Web.Account
{
    public class LogoutMemberSurfaceController : SurfaceController
    {
        private readonly IMemberSignInManager _memberSignInManager;

        public LogoutMemberSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberSignInManager memberSignInManager) :
            base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberSignInManager = memberSignInManager ?? throw new System.ArgumentNullException(nameof(memberSignInManager));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy]
        public async Task<IActionResult> HandleLogout()
        {
            if (User.Identity?.IsAuthenticated ?? false)
            {
                await _memberSignInManager.SignOutAsync();
            }
            return RedirectToCurrentUmbracoPage();
        }

        /// <summary>
        /// Calls the base <see cref="SurfaceController.RedirectToCurrentUmbracoPage" /> in a way which can be overridden for testing
        /// </summary>
        protected new virtual RedirectToUmbracoPageResult RedirectToCurrentUmbracoPage() => base.RedirectToCurrentUmbracoPage();
    }
}