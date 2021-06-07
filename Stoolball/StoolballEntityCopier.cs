using System.Linq;
using Stoolball.Clubs;
using Stoolball.Teams;

namespace Stoolball
{
    public class StoolballEntityCopier : IStoolballEntityCopier
    {
        public Club CreateAuditableCopy(Club club)
        {
            return new Club
            {
                ClubId = club.ClubId,
                ClubName = club.ClubName,
                Teams = club.Teams.Select(x => new Team { TeamId = x.TeamId }).ToList(),
                ClubRoute = club.ClubRoute,
                MemberGroupKey = club.MemberGroupKey,
                MemberGroupName = club.MemberGroupName
            };
        }
    }
}
