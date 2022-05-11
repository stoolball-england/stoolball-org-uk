using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stoolball.MatchLocations;
using Stoolball.Security;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using static Stoolball.Constants;

namespace Stoolball.Web.MatchLocations
{
    public class MatchLocationAuthorizationPolicy : IAuthorizationPolicy<MatchLocation>
    {
        private readonly IMemberManager _memberManager;
        private readonly IMemberService _memberService;

        public MatchLocationAuthorizationPolicy(IMemberManager memberManager, IMemberService memberService)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _memberService = memberService ?? throw new ArgumentNullException(nameof(memberService));
        }

        /// <inheritdoc/>
        public List<string> AuthorizedGroupNames(MatchLocation matchLocation)
        {
            if (matchLocation is null)
            {
                throw new ArgumentNullException(nameof(matchLocation));
            }

            var groups = new List<string>();
            if (!string.IsNullOrEmpty(matchLocation.MemberGroupName))
            {
                groups.Add(matchLocation.MemberGroupName);
            }
            return groups;
        }

        /// <inheritdoc/>
        public async Task<List<string>> AuthorizedMemberNames(MatchLocation matchLocation)
        {
            if (matchLocation is null)
            {
                throw new ArgumentNullException(nameof(matchLocation));
            }

            var members = new List<string>();
            if (!string.IsNullOrEmpty(matchLocation.MemberGroupName))
            {
                var currentMember = await _memberManager.GetCurrentMemberAsync();
                members.AddRange(_memberService.GetMembersInRole(matchLocation.MemberGroupName).Where(x => x.Username != currentMember?.UserName).Select(x => x.Name));
            }
            return members;
        }

        /// <inheritdoc/>
        public async Task<Dictionary<AuthorizedAction, bool>> IsAuthorized(MatchLocation matchLocation)
        {
            if (matchLocation is null)
            {
                throw new ArgumentNullException(nameof(matchLocation));
            }

            var authorizations = new Dictionary<AuthorizedAction, bool>();
            authorizations[AuthorizedAction.CreateMatchLocation] = await _memberManager.IsMemberAuthorizedAsync(null, new[] { Groups.Administrators, Groups.AllMembers }, null);
            authorizations[AuthorizedAction.DeleteMatchLocation] = await _memberManager.IsMemberAuthorizedAsync(null, new[] { Groups.Administrators }, null);
            if (!string.IsNullOrEmpty(matchLocation.MemberGroupName))
            {
                var groups = AuthorizedGroupNames(matchLocation);
                groups.Add(Groups.Administrators);
                authorizations[AuthorizedAction.EditMatchLocation] = await _memberManager.IsMemberAuthorizedAsync(null, groups, null);
            }
            else
            {
                authorizations[AuthorizedAction.EditMatchLocation] = authorizations[AuthorizedAction.DeleteMatchLocation];
            }
            return authorizations;
        }
    }
}