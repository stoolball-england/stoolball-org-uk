using Stoolball.Clubs;

namespace Stoolball
{
    public interface IStoolballEntityCopier
    {
        Club CreateAuditableCopy(Club club);
    }
}