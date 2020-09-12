using System;
using System.Collections.Generic;
using Stoolball.MatchLocations;
using Stoolball.Security;
using Umbraco.Web.Security;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Web.MatchLocations
{
    public class MatchLocationAuthorizationPolicy : IAuthorizationPolicy<MatchLocation>
    {
        private readonly MembershipHelper _membershipHelper;

        public MatchLocationAuthorizationPolicy(MembershipHelper membershipHelper)
        {
            _membershipHelper = membershipHelper;
        }

        public Dictionary<AuthorizedAction, bool> IsAuthorized(MatchLocation matchLocation)
        {
            if (matchLocation is null)
            {
                throw new ArgumentNullException(nameof(matchLocation));
            }

            var authorizations = new Dictionary<AuthorizedAction, bool>();
            authorizations[AuthorizedAction.CreateMatchLocation] = _membershipHelper.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.AllMembers }, null);
            authorizations[AuthorizedAction.EditMatchLocation] = _membershipHelper.IsMemberAuthorized(null, new[] { Groups.Administrators, matchLocation.MemberGroupName }, null);
            authorizations[AuthorizedAction.DeleteMatchLocation] = _membershipHelper.IsMemberAuthorized(null, new[] { Groups.Administrators }, null);
            return authorizations;
        }
    }
}