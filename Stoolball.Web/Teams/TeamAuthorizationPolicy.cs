using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stoolball.Security;
using Stoolball.Teams;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using static Stoolball.Constants;

namespace Stoolball.Web.Teams
{
    public class TeamAuthorizationPolicy : IAuthorizationPolicy<Team>
    {
        private readonly IMemberManager _memberManager;
        private readonly IMemberService _memberService;

        public TeamAuthorizationPolicy(IMemberManager memberManager, IMemberService memberService)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _memberService = memberService ?? throw new ArgumentNullException(nameof(memberService));
        }

        /// <inheritdoc/>
        public async Task<List<string>> AuthorizedMemberNames(Team team)
        {
            if (team is null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            var members = new List<string>();
            if (!string.IsNullOrEmpty(team.MemberGroupName))
            {
                var currentMember = await _memberManager.GetCurrentMemberAsync();
                members.AddRange(_memberService.GetMembersInRole(team.MemberGroupName).Where(x => x.Username != currentMember?.UserName).Select(x => x.Name));
            }
            return members;
        }

        /// <inheritdoc/>
        public List<string> AuthorizedGroupNames(Team team)
        {
            if (team is null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            var groups = new List<string>();
            if (!string.IsNullOrEmpty(team.MemberGroupName))
            {
                groups.Add(team.MemberGroupName);
            }
            return groups;
        }

        /// <inheritdoc/>
        public async Task<Dictionary<AuthorizedAction, bool>> IsAuthorized(Team team)
        {
            if (team is null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            var authorizations = new Dictionary<AuthorizedAction, bool>();
            authorizations[AuthorizedAction.CreateTeam] = await _memberManager.IsMemberAuthorizedAsync(null, new[] { Groups.Administrators, Groups.AllMembers }, null);
            authorizations[AuthorizedAction.DeleteTeam] = await _memberManager.IsMemberAuthorizedAsync(null, new[] { Groups.Administrators }, null);
            if (!string.IsNullOrEmpty(team.MemberGroupName))
            {
                var groups = AuthorizedGroupNames(team);
                groups.Add(Groups.Administrators);
                authorizations[AuthorizedAction.EditTeam] = await _memberManager.IsMemberAuthorizedAsync(null, groups, null);
            }
            else
            {
                authorizations[AuthorizedAction.EditTeam] = authorizations[AuthorizedAction.DeleteTeam];
            }
            return authorizations;
        }

    }
}