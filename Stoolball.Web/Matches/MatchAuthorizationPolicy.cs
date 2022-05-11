using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stoolball.Matches;
using Stoolball.Security;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using static Stoolball.Constants;

namespace Stoolball.Web.Matches
{
    public class MatchAuthorizationPolicy : IAuthorizationPolicy<Match>
    {
        private readonly IMemberManager _memberManager;
        private readonly IMemberService _memberService;

        public MatchAuthorizationPolicy(IMemberManager memberManager, IMemberService memberService)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _memberService = memberService ?? throw new ArgumentNullException(nameof(memberService));
        }

        /// <summary>
        /// Gets whether the current member can edit the given <see cref="Match"/>
        /// </summary>
        public async Task<Dictionary<AuthorizedAction, bool>> IsAuthorized(Match match)
        {
            if (match is null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            var authorizations = new Dictionary<AuthorizedAction, bool>();
            authorizations[AuthorizedAction.EditMatchResult] = _memberManager.IsLoggedIn();
            authorizations[AuthorizedAction.EditMatch] = await CanEditMatch(match, _memberManager);
            authorizations[AuthorizedAction.DeleteMatch] = authorizations[AuthorizedAction.EditMatch];
            return authorizations;
        }

        private static async Task<bool> CanEditMatch(Match match, IMemberManager memberManager)
        {
            var currentMember = await memberManager.GetCurrentMemberAsync();
            if (currentMember == null) return false;

            if (match.MemberKeys().Contains(currentMember.Key)) { return true; }

            var allowedGroups = new List<string>(match.MemberGroupNames());
            allowedGroups.AddRange(new[] { Groups.Administrators });

            return await memberManager.IsMemberAuthorizedAsync(null, allowedGroups, null);
        }

        /// <inheritdoc/>
        public List<string> AuthorizedGroupNames(Match match)
        {
            return new List<string>(match.MemberGroupNames());
        }

        /// <inheritdoc/>
        public async Task<List<string>> AuthorizedMemberNames(Match match)
        {
            if (match is null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            var members = new List<string>();
            var currentMember = await _memberManager.GetCurrentMemberAsync();
            foreach (var groupName in match.MemberGroupNames())
            {
                members.AddRange(_memberService.GetMembersInRole(groupName).Where(x => x.Username != currentMember?.UserName && !members.Contains(x.Name)).Select(x => x.Name));
            }
            foreach (var memberKey in match.MemberKeys())
            {
                if (memberKey != currentMember?.Key)
                {
                    var name = _memberService.GetByKey(memberKey)?.Name;
                    if (!string.IsNullOrEmpty(name) && !members.Contains(name))
                    {
                        members.Add(name);
                    }
                }
            }
            return members;
        }
    }
}
