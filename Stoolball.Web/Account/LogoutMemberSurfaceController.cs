using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Umbraco.Web.Mvc;

namespace Stoolball.Web.Account
{
    public class LogoutMemberSurfaceController : SurfaceController
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
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