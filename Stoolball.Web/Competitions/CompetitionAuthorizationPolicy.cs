using System;
using System.Collections.Generic;
using Stoolball.Competitions;
using Stoolball.Security;
using Umbraco.Web.Security;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Web.Competitions
{
    public class CompetitionAuthorizationPolicy : IAuthorizationPolicy<Competition>
    {
        private readonly MembershipHelper _membershipHelper;

        public CompetitionAuthorizationPolicy(MembershipHelper membershipHelper)
        {
            _membershipHelper = membershipHelper;
        }

        public Dictionary<AuthorizedAction, bool> IsAuthorized(Competition competition)
        {
            if (competition is null)
            {
                throw new ArgumentNullException(nameof(competition));
            }

            var authorizations = new Dictionary<AuthorizedAction, bool>();
            authorizations[AuthorizedAction.CreateCompetition] = _membershipHelper.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.AllMembers }, null);
            authorizations[AuthorizedAction.EditCompetition] = _membershipHelper.IsMemberAuthorized(null, new[] { Groups.Administrators, competition.MemberGroupName }, null);
            authorizations[AuthorizedAction.DeleteCompetition] = _membershipHelper.IsMemberAuthorized(null, new[] { Groups.Administrators }, null);
            return authorizations;
        }
    }
}