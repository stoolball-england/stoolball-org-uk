using System.Collections.Generic;
using System.Threading.Tasks;
using Stoolball.Clubs;
using Stoolball.Security;
using Umbraco.Cms.Core.Security;
using static Stoolball.Constants;

namespace Stoolball.Web.Clubs
{
    public class ClubAuthorizationPolicy : IAuthorizationPolicy<Club>
    {
        private readonly IMemberManager _memberManager;

        public ClubAuthorizationPolicy(IMemberManager memberManager)
        {
            _memberManager = memberManager ?? throw new System.ArgumentNullException(nameof(memberManager));
        }

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
                authorizations[AuthorizedAction.EditClub] = await _memberManager.IsMemberAuthorizedAsync(null, new[] { Groups.Administrators, club.MemberGroupName }, null);
            }
            else
            {
                authorizations[AuthorizedAction.EditClub] = authorizations[AuthorizedAction.DeleteClub];
            }
            return authorizations;
        }
    }
}