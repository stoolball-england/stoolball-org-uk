using Stoolball.Competitions;
using Stoolball.Web.Security;
using System;
using System.Collections.Generic;
using Umbraco.Web.Security;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Web.Competitions
{
    public class CompetitionAuthorizationPolicy : IAuthorizationPolicy<Competition>
    {
        public Dictionary<AuthorizedAction, bool> IsAuthorized(Competition competition, MembershipHelper membershipHelper)
        {
            if (competition is null)
            {
                throw new ArgumentNullException(nameof(competition));
            }

            if (membershipHelper is null)
            {
                throw new ArgumentNullException(nameof(membershipHelper));
            }

            var authorizations = new Dictionary<AuthorizedAction, bool>();
            authorizations[AuthorizedAction.CreateCompetition] = membershipHelper.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.AllMembers }, null);
            authorizations[AuthorizedAction.EditCompetition] = membershipHelper.IsMemberAuthorized(null, new[] { Groups.Administrators, competition.MemberGroupName }, null);
            authorizations[AuthorizedAction.DeleteCompetition] = membershipHelper.IsMemberAuthorized(null, new[] { Groups.Administrators }, null);
            return authorizations;
        }
    }
}