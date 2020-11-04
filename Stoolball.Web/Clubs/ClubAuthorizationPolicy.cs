using System.Collections.Generic;
using Stoolball.Clubs;
using Stoolball.Security;
using Umbraco.Web.Security;
using static Stoolball.Data.SqlServer.Constants;

namespace Stoolball.Web.Clubs
{
    public class ClubAuthorizationPolicy : IAuthorizationPolicy<Club>
    {
        private readonly MembershipHelper _membershipHelper;

        public ClubAuthorizationPolicy(MembershipHelper membershipHelper)
        {
            _membershipHelper = membershipHelper;
        }

        public Dictionary<AuthorizedAction, bool> IsAuthorized(Club club)
        {
            if (club is null)
            {
                throw new System.ArgumentNullException(nameof(club));
            }

            var authorizations = new Dictionary<AuthorizedAction, bool>();
            authorizations[AuthorizedAction.CreateClub] = _membershipHelper.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.AllMembers }, null);
            authorizations[AuthorizedAction.EditClub] = _membershipHelper.IsMemberAuthorized(null, new[] { Groups.Administrators, club.MemberGroupName }, null);
            authorizations[AuthorizedAction.DeleteClub] = _membershipHelper.IsMemberAuthorized(null, new[] { Groups.Administrators }, null);
            return authorizations;
        }
    }
}