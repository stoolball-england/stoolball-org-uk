using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stoolball.Security;
using Stoolball.Teams;
using Umbraco.Cms.Core.Security;
using static Stoolball.Constants;

namespace Stoolball.Web.Teams
{
    public class TeamAuthorizationPolicy : IAuthorizationPolicy<Team>
    {
        private readonly IMemberManager _membershipManager;

        public TeamAuthorizationPolicy(IMemberManager memberManager)
        {
            _membershipManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
        }

        public async Task<Dictionary<AuthorizedAction, bool>> IsAuthorized(Team team)
        {
            if (team is null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            var authorizations = new Dictionary<AuthorizedAction, bool>();
            authorizations[AuthorizedAction.CreateTeam] = await _membershipManager.IsMemberAuthorizedAsync(null, new[] { Groups.Administrators, Groups.AllMembers }, null);
            authorizations[AuthorizedAction.DeleteTeam] = await _membershipManager.IsMemberAuthorizedAsync(null, new[] { Groups.Administrators }, null);
            if (!string.IsNullOrEmpty(team.MemberGroupName))
            {
                authorizations[AuthorizedAction.EditTeam] = await _membershipManager.IsMemberAuthorizedAsync(null, new[] { Groups.Administrators, team.MemberGroupName }, null);
            }
            else
            {
                authorizations[AuthorizedAction.EditTeam] = authorizations[AuthorizedAction.DeleteTeam];
            }
            return authorizations;
        }
    }
}