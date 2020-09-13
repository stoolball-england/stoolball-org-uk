using System.Collections.Generic;

namespace Stoolball.Matches
{
    public interface IPlayerInningsScaffolder
    {
        void ScaffoldPlayerInnings(List<PlayerInnings> playerInnings, int playersPerTeam);
    }
}