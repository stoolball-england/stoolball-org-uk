using System.Linq;
using Stoolball.Clubs;
using Stoolball.Statistics;
using Stoolball.Teams;

namespace Stoolball
{
    public class StoolballEntityCopier : IStoolballEntityCopier
    {
        public Club CreateAuditableCopy(Club club)
        {
            if (club == null) { return null; }
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

        public PlayerIdentity CreateAuditableCopy(PlayerIdentity playerIdentity)
        {
            if (playerIdentity == null) { return null; }
            return new PlayerIdentity
            {
                PlayerIdentityId = playerIdentity.PlayerIdentityId,
                PlayerIdentityName = playerIdentity.PlayerIdentityName,
                Team = CreateAuditableCopy(playerIdentity.Team)
            };
        }

        public Team CreateAuditableCopy(Team team)
        {
            if (team == null) { return null; }
            return new Team { TeamId = team.TeamId };
        }
    }
}
