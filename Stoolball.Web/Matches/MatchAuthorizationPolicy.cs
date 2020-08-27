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
        /// Gets whether the current member can edit the given <see cref="Match"/>
        /// </summary>
        /// <remarks>It's recommended to inject MembershipHelper but GetCurrentMember() returns null (https://github.com/umbraco/Umbraco-CMS/blob/2f10051ee9780cd22d4d1313e5e7c6b0bc4661b1/src/Umbraco.Web/UmbracoHelper.cs#L98)</remarks>
        public Dictionary<AuthorizedAction, bool> IsAuthorized(Match match, MembershipHelper membershipHelper)
        {
            if (match is null)
            {
                throw new System.ArgumentNullException(nameof(match));
            }

            if (membershipHelper is null)
            {
                throw new System.ArgumentNullException(nameof(membershipHelper));
            }

            var authorizations = new Dictionary<AuthorizedAction, bool>();
            authorizations[AuthorizedAction.EditMatchResult] = membershipHelper.IsLoggedIn();
            authorizations[AuthorizedAction.EditMatch] = CanEditMatch(match, membershipHelper);
            authorizations[AuthorizedAction.DeleteMatch] = authorizations[AuthorizedAction.EditMatch];
            return authorizations;
        }

        private static bool CanEditMatch(Match match, MembershipHelper membershipHelper)
        {
            var currentMember = membershipHelper.GetCurrentMember();
            if (currentMember == null) return false;

            if (match.MemberKeys().Contains(currentMember.Key)) { return true; }

            var allowedGroups = new List<string>(match.MemberGroupNames());
            allowedGroups.AddRange(new[] { Groups.Administrators });

            return membershipHelper.IsMemberAuthorized(null, allowedGroups, null);
        }
    }
}
