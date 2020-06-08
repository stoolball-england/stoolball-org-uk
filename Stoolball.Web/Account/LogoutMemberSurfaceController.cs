using Stoolball.Web.Security;
using System.Web.Mvc;
using Umbraco.Web.Mvc;

namespace Stoolball.Web.Account
{
    public class LogoutMemberSurfaceController : SurfaceController
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy]
        public ActionResult HandleLogout()
        {
            if (Umbraco.MemberIsLoggedOn())
            {
                Umbraco.MembershipHelper.Logout();
            }
            return RedirectToCurrentUmbracoPage();
        }
    }
}