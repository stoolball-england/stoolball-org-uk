using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stoolball.Clubs;
using Stoolball.Security;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using static Stoolball.Constants;

namespace Stoolball.Web.Clubs
{
    public class ClubAuthorizationPolicy : IAuthorizationPolicy<Club>
    {
        private readonly IMemberManager _memberManager;
        private readonly IMemberService _memberService;

        public ClubAuthorizationPolicy(IMemberManager memberManager, IMemberService memberService)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _memberService = memberService ?? throw new ArgumentNullException(nameof(memberService));
        }

        /// <inheritdoc/>
        public List<string> AuthorizedGroupNames(Club club)
        {
            if (club is null)
            {
                throw new ArgumentNullException(nameof(club));
            }

            var groups = new List<string>();
            if (!string.IsNullOrEmpty(club.MemberGroupName))
            {
                groups.Add(club.MemberGroupName);
            }
            return groups;
        }

        /// <inheritdoc/>
        public async Task<List<string>> AuthorizedMemberNames(Club club)
        {
            if (club is null)
            {
                throw new ArgumentNullException(nameof(club));
            }

            var members = new List<string>();
            if (!string.IsNullOrEmpty(club.MemberGroupName))
            {
                var currentMember = await _memberManager.GetCurrentMemberAsync();
                members.AddRange(_memberService.GetMembersInRole(club.MemberGroupName).Where(x => x.Username != currentMember?.UserName).Select(x => x.Name));
            }
            return members;
        }

        /// <inheritdoc/>
        public async Task<Dictionary<AuthorizedAction, bool>> IsAuthorized(Club club)
        {
            if (club is null)
            {
                throw new System.ArgumentNullException(nameof(club));
            }

            var authorizations = new Dictionary<AuthorizedAction, bool>();
            authorizations[AuthorizedAction.CreateClub] = await _memberManager.IsMemberAuthorizedAsync(null, new[] { Groups.Administrators, Groups.AllMembers }, null);
            authorizations[AuthorizedAction.DeleteClub] = await _memberManager.IsMemberAuthorizedAsync(null, new[] { Groups.Administrators }, null);
            if (!string.IsNullOrEmpty(club.MemberGroupName))
            {
                var groups = AuthorizedGroupNames(club);
                groups.Add(Groups.Administrators);
                authorizations[AuthorizedAction.EditClub] = await _memberManager.IsMemberAuthorizedAsync(null, groups, null);
            }
            else
            {
                authorizations[AuthorizedAction.EditClub] = authorizations[AuthorizedAction.DeleteClub];
            }
            return authorizations;
        }
    }
}