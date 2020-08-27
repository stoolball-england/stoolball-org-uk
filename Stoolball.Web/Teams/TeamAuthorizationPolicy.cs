using Stoolball.Teams;
using Stoolball.Web.Security;
using System.Collections.Generic;
using Umbraco.Web.Security;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Web.Teams
{
    public class TeamAuthorizationPolicy : IAuthorizationPolicy<Team>
    {
        public Dictionary<AuthorizedAction, bool> IsAuthorized(Team team, MembershipHelper membershipHelper)
        {
            if (team is null)
            {
                throw new System.ArgumentNullException(nameof(team));
            }

            if (membershipHelper is null)
            {
                throw new System.ArgumentNullException(nameof(membershipHelper));
            }

            var authorizations = new Dictionary<AuthorizedAction, bool>();
            authorizations[AuthorizedAction.CreateTeam] = membershipHelper.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.AllMembers }, null);
            authorizations[AuthorizedAction.EditTeam] = membershipHelper.IsMemberAuthorized(null, new[] { Groups.Administrators, team.MemberGroupName }, null);
            authorizations[AuthorizedAction.DeleteTeam] = membershipHelper.IsMemberAuthorized(null, new[] { Groups.Administrators }, null);
            return authorizations;
        }
    }
}