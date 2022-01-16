using System;
using Umbraco.Web.Security;

namespace Stoolball.Web.Account
{
    public class LoginMemberWrapper : ILoginMemberWrapper
    {
        private readonly MembershipHelper _membershipHelper;

        public LoginMemberWrapper(MembershipHelper membershipHelper)
        {
            _membershipHelper = membershipHelper ?? throw new ArgumentNullException(nameof(membershipHelper));
        }

        public void LoginMember(string username, string password)
        {
            _membershipHelper.Login(username, password);
        }
    }
}