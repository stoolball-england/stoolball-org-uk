using System;
using System.Threading.Tasks;
using Umbraco.Cms.Web.Common.Security;

namespace Stoolball.Web.Account
{
    public class LogoutMemberWrapper : ILogoutMemberWrapper
    {
        private readonly IMemberSignInManager _memberSignInManager;

        public LogoutMemberWrapper(IMemberSignInManager memberSignInManager)
        {
            _memberSignInManager = memberSignInManager ?? throw new ArgumentNullException(nameof(memberSignInManager));
        }

        public async Task LogoutMember()
        {
            await _memberSignInManager.SignOutAsync();
        }
    }
}