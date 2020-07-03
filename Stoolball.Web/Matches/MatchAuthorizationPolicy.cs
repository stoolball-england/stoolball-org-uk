using Stoolball.Matches;
using Stoolball.Web.Security;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Web.Security;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Web.Matches
{
    public class MatchAuthorizationPolicy : IAuthorizationPolicy<Match>
    {
        /// <summary>
        /// Gets whether the current member can delete the given <see cref="Match"/>
        /// </summary>
        /// <remarks>It's recommended to inject MembershipHelper but GetCurrentMember() returns null (https://github.com/umbraco/Umbraco-CMS/blob/2f10051ee9780cd22d4d1313e5e7c6b0bc4661b1/src/Umbraco.Web/UmbracoHelper.cs#L98)</remarks>
        public bool CanDelete(Match match, MembershipHelper membershipHelper)
        {
            if (match is null)
            {
                throw new System.ArgumentNullException(nameof(match));
            }

            if (membershipHelper is null)
            {
                throw new System.ArgumentNullException(nameof(membershipHelper));
            }

            var currentMember = membershipHelper.GetCurrentMember();
            if (currentMember == null) return false;

            if (match.MemberKeys().Contains(currentMember.Key)) { return true; }

            var allowedGroups = new List<string>(match.MemberGroupNames());
            allowedGroups.AddRange(new[] { Groups.Administrators });

            return membershipHelper.IsMemberAuthorized(null, allowedGroups, null);
        }
    }
}