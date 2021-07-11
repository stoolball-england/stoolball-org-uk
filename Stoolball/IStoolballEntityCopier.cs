using Stoolball.Clubs;
using Stoolball.Statistics;
using Stoolball.Teams;

namespace Stoolball
{
    public interface IStoolballEntityCopier
    {
        Club CreateAuditableCopy(Club club);
        Team CreateAuditableCopy(Team team);
        PlayerIdentity CreateAuditableCopy(PlayerIdentity playerIdentity);
    }
}