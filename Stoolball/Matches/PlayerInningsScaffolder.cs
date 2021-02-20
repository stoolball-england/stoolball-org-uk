using System;
using System.Collections.Generic;
using Stoolball.Statistics;

namespace Stoolball.Matches
{
    public class PlayerInningsScaffolder : IPlayerInningsScaffolder
    {
        public void ScaffoldPlayerInnings(List<PlayerInnings> playerInnings, int playersPerTeam)
        {
            if (playerInnings is null)
            {
                throw new ArgumentNullException(nameof(playerInnings));
            }

            while (playerInnings.Count < playersPerTeam)
            {
                playerInnings.Add(new PlayerInnings { Batter = new PlayerIdentity() });
            }
        }
    }
}
