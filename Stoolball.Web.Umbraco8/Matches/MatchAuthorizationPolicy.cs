using System.Collections.Generic;
using System.Linq;
using Stoolball.Matches;
using Stoolball.Security;
using Umbraco.Web.Security;
using static Stoolball.Constants;

namespace Stoolball.Web.Matches
{
    public class MatchAuthorizationPolicy : IAuthorizationPolicy<Match>
    {
        private readonly MembershipHelper _membershipHelper;

        public MatchAuthorizationPolicy(MembershipHelper membershipHelper)
        {
            _membershipHelper = membershipHelper;
        }

        /// <summary>
        /// Gets whether the current member can edit the given <see cref="Match"/>
        /// </summary>
        public Dictionary<AuthorizedAction, bool> IsAuthorized(Match match)
        {
            if (match is null)
            {
                throw new System.ArgumentNullException(nameof(match));
            }

            var authorizations = new Dictionary<AuthorizedAction, bool>();
            authorizations[AuthorizedAction.EditMatchResult] = _membershipHelper.IsLoggedIn();
            authorizations[AuthorizedAction.EditMatch] = CanEditMatch(match, _membershipHelper);
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
