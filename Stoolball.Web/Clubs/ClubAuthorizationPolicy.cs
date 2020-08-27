using Stoolball.Clubs;
using Stoolball.Web.Security;
using System.Collections.Generic;
using Umbraco.Web.Security;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Web.Clubs
{
    public class ClubAuthorizationPolicy : IAuthorizationPolicy<Club>
    {
        public Dictionary<AuthorizedAction, bool> IsAuthorized(Club club, MembershipHelper membershipHelper)
        {
            if (club is null)
            {
                throw new System.ArgumentNullException(nameof(club));
            }

            if (membershipHelper is null)
            {
                throw new System.ArgumentNullException(nameof(membershipHelper));
            }

            var authorizations = new Dictionary<AuthorizedAction, bool>();
            authorizations[AuthorizedAction.CreateClub] = membershipHelper.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.AllMembers }, null);
            authorizations[AuthorizedAction.EditClub] = membershipHelper.IsMemberAuthorized(null, new[] { Groups.Administrators, club.MemberGroupName }, null);
            authorizations[AuthorizedAction.DeleteClub] = membershipHelper.IsMemberAuthorized(null, new[] { Groups.Administrators }, null);
            return authorizations;
        }
    }
}