using Stoolball.Clubs;
using Stoolball.Competitions;
using Stoolball.MatchLocations;
using Stoolball.Statistics;
using Stoolball.Teams;

namespace Stoolball
{
    public interface IStoolballEntityCopier
    {
        Club CreateAuditableCopy(Club club);
        Team CreateAuditableCopy(Team team);
        PlayerIdentity CreateAuditableCopy(PlayerIdentity playerIdentity);
        MatchLocation CreateAuditableCopy(MatchLocation matchLocation);
        MatchLocation CreateRedactedCopy(MatchLocation matchLocation);
        Season CreateAuditableCopy(Season season);
        Season CreateRedactedCopy(Season season);
    }
}