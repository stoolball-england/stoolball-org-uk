using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stoolball.Competitions;
using Stoolball.Security;
using Umbraco.Cms.Core.Security;
using static Stoolball.Constants;

namespace Stoolball.Web.Competitions
{
    public class CompetitionAuthorizationPolicy : IAuthorizationPolicy<Competition>
    {
        private readonly IMemberManager _memberManager;

        public CompetitionAuthorizationPolicy(IMemberManager memberManager)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
        }

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
                authorizations[AuthorizedAction.EditCompetition] = await _memberManager.IsMemberAuthorizedAsync(null, new[] { Groups.Administrators, competition.MemberGroupName }, null);
            }
            else
            {
                authorizations[AuthorizedAction.EditCompetition] = authorizations[AuthorizedAction.DeleteCompetition];
            }
            return authorizations;
        }
    }
}