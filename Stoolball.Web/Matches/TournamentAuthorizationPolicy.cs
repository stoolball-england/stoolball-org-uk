using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stoolball.Matches;
using Stoolball.Security;
using Umbraco.Cms.Core.Security;
using static Stoolball.Constants;

namespace Stoolball.Web.Matches
{
    public class TournamentAuthorizationPolicy : IAuthorizationPolicy<Tournament>
    {
        private readonly IMemberManager _memberManager;

        public TournamentAuthorizationPolicy(IMemberManager memberManager)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
        }

        /// <summary>
        /// Gets whether the current member can delete the given <see cref="Tournament"/>
        /// </summary>
        public async Task<Dictionary<AuthorizedAction, bool>> IsAuthorized(Tournament tournament)
        {
            if (tournament is null)
            {
                throw new ArgumentNullException(nameof(tournament));
            }

            var authorizations = new Dictionary<AuthorizedAction, bool>();
            authorizations[AuthorizedAction.EditTournament] = await CanEditTournament(tournament, _memberManager);
            authorizations[AuthorizedAction.DeleteTournament] = authorizations[AuthorizedAction.EditTournament];

            return authorizations;
        }

        private async static Task<bool> CanEditTournament(Tournament tournament, IMemberManager memberManager)
        {
            var currentMember = await memberManager.GetCurrentMemberAsync();
            if (currentMember == null) return false;

            if (tournament.MemberKey == currentMember.Key) { return true; }

            return await memberManager.IsMemberAuthorizedAsync(null, new[] { Groups.Administrators }, null);
        }
    }
}