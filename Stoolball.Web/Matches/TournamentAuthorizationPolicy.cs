using Stoolball.Matches;
using Stoolball.Web.Security;
using Umbraco.Web.Security;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Web.Matches
{
    public class TournamentAuthorizationPolicy : IAuthorizationPolicy<Tournament>
    {
        /// <summary>
        /// Gets whether the current member can delete the given <see cref="Tournament"/>
        /// </summary>
        /// <remarks>It's recommended to inject MembershipHelper but GetCurrentMember() returns null (https://github.com/umbraco/Umbraco-CMS/blob/2f10051ee9780cd22d4d1313e5e7c6b0bc4661b1/src/Umbraco.Web/UmbracoHelper.cs#L98)</remarks>
        public bool CanEdit(Tournament tournament, MembershipHelper membershipHelper)
        {
            if (tournament is null)
            {
                throw new System.ArgumentNullException(nameof(tournament));
            }

            if (membershipHelper is null)
            {
                throw new System.ArgumentNullException(nameof(membershipHelper));
            }

            var currentMember = membershipHelper.GetCurrentMember();
            if (currentMember == null) return false;

            if (tournament.MemberKey == currentMember.Key) { return true; }

            return membershipHelper.IsMemberAuthorized(null, new[] { Groups.Administrators }, null);
        }

        /// <summary>
        /// Gets whether the current member can delete the given <see cref="Tournament"/>
        /// </summary>
        /// <remarks>It's recommended to inject MembershipHelper but GetCurrentMember() returns null (https://github.com/umbraco/Umbraco-CMS/blob/2f10051ee9780cd22d4d1313e5e7c6b0bc4661b1/src/Umbraco.Web/UmbracoHelper.cs#L98)</remarks>
        public bool CanDelete(Tournament tournament, MembershipHelper membershipHelper)
        {
            return CanEdit(tournament, membershipHelper);
        }
    }
}