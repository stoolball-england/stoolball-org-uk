using System.Collections.Generic;
using Stoolball.Matches;
using Stoolball.Security;
using Umbraco.Web.Security;
using static Stoolball.Constants;

namespace Stoolball.Web.Matches
{
    public class TournamentAuthorizationPolicy : IAuthorizationPolicy<Tournament>
    {
        private readonly MembershipHelper _membershipHelper;

        public TournamentAuthorizationPolicy(MembershipHelper membershipHelper)
        {
            _membershipHelper = membershipHelper;
        }

        /// <summary>
        /// Gets whether the current member can delete the given <see cref="Tournament"/>
        /// </summary>
        public Dictionary<AuthorizedAction, bool> IsAuthorized(Tournament tournament)
        {
            if (tournament is null)
            {
                throw new System.ArgumentNullException(nameof(tournament));
            }

            var authorizations = new Dictionary<AuthorizedAction, bool>();
            authorizations[AuthorizedAction.EditTournament] = CanEditTournament(tournament, _membershipHelper);
            authorizations[AuthorizedAction.DeleteTournament] = authorizations[AuthorizedAction.EditTournament];

            return authorizations;
        }

        private static bool CanEditTournament(Tournament tournament, MembershipHelper membershipHelper)
        {
            var currentMember = membershipHelper.GetCurrentMember();
            if (currentMember == null) return false;

            if (tournament.MemberKey == currentMember.Key) { return true; }

            return membershipHelper.IsMemberAuthorized(null, new[] { Groups.Administrators }, null);
        }
    }
}