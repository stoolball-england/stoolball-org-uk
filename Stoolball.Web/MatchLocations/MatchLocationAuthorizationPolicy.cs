using Stoolball.MatchLocations;
using Stoolball.Web.Security;
using System;
using System.Collections.Generic;
using Umbraco.Web.Security;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Web.MatchLocations
{
    public class MatchLocationAuthorizationPolicy : IAuthorizationPolicy<MatchLocation>
    {
        public Dictionary<AuthorizedAction, bool> IsAuthorized(MatchLocation matchLocation, MembershipHelper membershipHelper)
        {
            if (matchLocation is null)
            {
                throw new ArgumentNullException(nameof(matchLocation));
            }

            if (membershipHelper is null)
            {
                throw new ArgumentNullException(nameof(membershipHelper));
            }

            var authorizations = new Dictionary<AuthorizedAction, bool>();
            authorizations[AuthorizedAction.CreateMatchLocation] = membershipHelper.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.AllMembers }, null);
            authorizations[AuthorizedAction.EditMatchLocation] = membershipHelper.IsMemberAuthorized(null, new[] { Groups.Administrators, matchLocation.MemberGroupName }, null);
            authorizations[AuthorizedAction.DeleteMatchLocation] = membershipHelper.IsMemberAuthorized(null, new[] { Groups.Administrators }, null);
            return authorizations;
        }
    }
}