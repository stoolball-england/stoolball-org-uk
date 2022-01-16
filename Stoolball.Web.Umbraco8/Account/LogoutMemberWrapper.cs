using System;
using Umbraco.Web.Security;

namespace Stoolball.Web.Account
{
    public class LogoutMemberWrapper : ILogoutMemberWrapper
    {
        private readonly MembershipHelper _membershipHelper;

        public LogoutMemberWrapper(MembershipHelper membershipHelper)
        {
            _membershipHelper = membershipHelper ?? throw new ArgumentNullException(nameof(membershipHelper));
        }

        public void LogoutMember()
        {
            _membershipHelper.Logout();
        }
    }
}