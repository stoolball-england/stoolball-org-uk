using System.Collections.Generic;
using Stoolball.Security;
using Stoolball.Teams;
using Umbraco.Web.Security;
using static Stoolball.Constants;

namespace Stoolball.Web.Teams
{
    public class TeamAuthorizationPolicy : IAuthorizationPolicy<Team>
    {
        private readonly MembershipHelper _membershipHelper;

        public TeamAuthorizationPolicy(MembershipHelper membershipHelper)
        {
            _membershipHelper = membershipHelper;
        }

        public Dictionary<AuthorizedAction, bool> IsAuthorized(Team team)
        {
            if (team is null)
            {
                throw new System.ArgumentNullException(nameof(team));
            }

            var authorizations = new Dictionary<AuthorizedAction, bool>();
            authorizations[AuthorizedAction.CreateTeam] = _membershipHelper.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.AllMembers }, null);
            authorizations[AuthorizedAction.DeleteTeam] = _membershipHelper.IsMemberAuthorized(null, new[] { Groups.Administrators }, null);
            if (!string.IsNullOrEmpty(team.MemberGroupName))
            {
                authorizations[AuthorizedAction.EditTeam] = _membershipHelper.IsMemberAuthorized(null, new[] { Groups.Administrators, team.MemberGroupName }, null);
            }
            else
            {
                authorizations[AuthorizedAction.EditTeam] = authorizations[AuthorizedAction.DeleteTeam];
            }
            return authorizations;
        }
    }
}