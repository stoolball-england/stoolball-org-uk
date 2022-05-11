using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stoolball.Competitions;
using Stoolball.Security;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using static Stoolball.Constants;

namespace Stoolball.Web.Competitions
{
    public class CompetitionAuthorizationPolicy : IAuthorizationPolicy<Competition>
    {
        private readonly IMemberManager _memberManager;
        private readonly IMemberService _memberService;

        public CompetitionAuthorizationPolicy(IMemberManager memberManager, IMemberService memberService)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _memberService = memberService ?? throw new ArgumentNullException(nameof(memberService));
        }

        /// <inheritdoc/>
        public List<string> AuthorizedGroupNames(Competition competition)
        {
            if (competition is null)
            {
                throw new ArgumentNullException(nameof(competition));
            }

            var groups = new List<string>();
            if (!string.IsNullOrEmpty(competition.MemberGroupName))
            {
                groups.Add(competition.MemberGroupName);
            }
            return groups;
        }

        /// <inheritdoc/>
        public async Task<List<string>> AuthorizedMemberNames(Competition competition)
        {
            if (competition is null)
            {
                throw new ArgumentNullException(nameof(competition));
            }

            var members = new List<string>();
            if (!string.IsNullOrEmpty(competition.MemberGroupName))
            {
                var currentMember = await _memberManager.GetCurrentMemberAsync();
                members.AddRange(_memberService.GetMembersInRole(competition.MemberGroupName).Where(x => x.Username != currentMember?.UserName).Select(x => x.Name));
            }
            return members;
        }

        /// <inheritdoc/>
        public async Task<Dictionary<AuthorizedAction, bool>> IsAuthorized(Competition competition)
        {
            if (competition is null)
            {
                throw new ArgumentNullException(nameof(competition));
            }

            var authorizations = new Dictionary<AuthorizedAction, bool>();
            authorizations[AuthorizedAction.CreateCompetition] = await _memberManager.IsMemberAuthorizedAsync(null, new[] { Groups.Administrators, Groups.AllMembers }, null);
            authorizations[AuthorizedAction.DeleteCompetition] = await _memberManager.IsMemberAuthorizedAsync(null, new[] { Groups.Administrators }, null);
            if (!string.IsNullOrEmpty(competition.MemberGroupName))
            {
                var groups = AuthorizedGroupNames(competition);
                groups.Add(Groups.Administrators);
                authorizations[AuthorizedAction.EditCompetition] = await _memberManager.IsMemberAuthorizedAsync(null, groups, null);
            }
            else
            {
                authorizations[AuthorizedAction.EditCompetition] = authorizations[AuthorizedAction.DeleteCompetition];
            }
            return authorizations;
        }
    }
}