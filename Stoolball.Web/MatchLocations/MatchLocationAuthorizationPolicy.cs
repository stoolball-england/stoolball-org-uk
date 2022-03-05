using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stoolball.MatchLocations;
using Stoolball.Security;
using Umbraco.Cms.Core.Security;
using static Stoolball.Constants;

namespace Stoolball.Web.MatchLocations
{
    public class MatchLocationAuthorizationPolicy : IAuthorizationPolicy<MatchLocation>
    {
        private readonly IMemberManager _memberManager;

        public MatchLocationAuthorizationPolicy(IMemberManager memberManager)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
        }

        public async Task<Dictionary<AuthorizedAction, bool>> IsAuthorized(MatchLocation matchLocation)
        {
            if (matchLocation is null)
            {
                throw new ArgumentNullException(nameof(matchLocation));
            }

            var authorizations = new Dictionary<AuthorizedAction, bool>();
            authorizations[AuthorizedAction.CreateMatchLocation] = await _memberManager.IsMemberAuthorizedAsync(null, new[] { Groups.Administrators, Groups.AllMembers }, null);
            authorizations[AuthorizedAction.DeleteMatchLocation] = await _memberManager.IsMemberAuthorizedAsync(null, new[] { Groups.Administrators }, null);
            if (!string.IsNullOrEmpty(matchLocation.MemberGroupName))
            {
                authorizations[AuthorizedAction.EditMatchLocation] = await _memberManager.IsMemberAuthorizedAsync(null, new[] { Groups.Administrators, matchLocation.MemberGroupName }, null);
            }
            else
            {
                authorizations[AuthorizedAction.EditMatchLocation] = authorizations[AuthorizedAction.DeleteMatchLocation];
            }
            return authorizations;
        }
    }
}