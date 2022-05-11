using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stoolball.Matches;
using Stoolball.Security;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using static Stoolball.Constants;

namespace Stoolball.Web.Matches
{
    public class TournamentAuthorizationPolicy : IAuthorizationPolicy<Tournament>
    {
        private readonly IMemberManager _memberManager;
        private readonly IMemberService _memberService;

        public TournamentAuthorizationPolicy(IMemberManager memberManager, IMemberService memberService)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _memberService = memberService ?? throw new ArgumentNullException(nameof(memberService));
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

        /// <inheritdoc/>
        public List<string> AuthorizedGroupNames(Tournament tournament)
        {
            return new List<string>();
        }

        /// <inheritdoc/>
        public async Task<List<string>> AuthorizedMemberNames(Tournament tournament)
        {
            if (tournament is null)
            {
                throw new ArgumentNullException(nameof(tournament));
            }

            var members = new List<string>();
            var currentMember = await _memberManager.GetCurrentMemberAsync();
            if (tournament.MemberKey.HasValue && tournament.MemberKey != currentMember?.Key)
            {
                var name = _memberService.GetByKey(tournament.MemberKey.Value)?.Name;
                if (!string.IsNullOrEmpty(name))
                {
                    members.Add(name);
                }
            }
            return members;
        }
    }
}