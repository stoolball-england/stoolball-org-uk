using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Umbraco.Web.Mvc;

namespace Stoolball.Web.Account
{
    public class MyAccountSurfaceController : SurfaceController
    {
        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult UpdateAccount([Bind(Prefix = "accountUpdate")]MyAccountUpdate model)
        {
            if (ModelState.IsValid && model != null)
            {
                var member = Members.GetCurrentMember();

                var editableMember = Services.MemberService.GetById(member.Id);
                editableMember.Name = model.Name;

                Services.MemberService.Save(editableMember);

                TempData["Success"] = true;
                return RedirectToCurrentUmbracoPage();
            }
            else
            {
                return CurrentUmbracoPage();
            }
        }

    }
}
