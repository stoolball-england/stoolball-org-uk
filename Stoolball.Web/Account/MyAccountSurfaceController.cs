using System.Web.Mvc;
using Stoolball.Web.Security;
using Umbraco.Web.Mvc;
using static Stoolball.Data.SqlServer.Constants;

namespace Stoolball.Web.Account
{
    public class MyAccountSurfaceController : SurfaceController
    {
        [HttpPost]
        [ValidateAntiForgeryToken()]
        [ContentSecurityPolicy(Forms = true)]
        public ActionResult UpdateAccount([Bind(Prefix = "accountUpdate")] MyAccountUpdate model)
        {
            if (ModelState.IsValid && model != null)
            {
                var member = Members.GetCurrentMember();

                var editableMember = Services.MemberService.GetById(member.Id);
                editableMember.Name = model.Name;

                Services.MemberService.Save(editableMember);

                Logger.Info(typeof(Umbraco.Core.Security.UmbracoMembershipProviderBase), LoggingTemplates.MemberAccountUpdated, member.Name, member.Key, GetType(), nameof(UpdateAccount));

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
