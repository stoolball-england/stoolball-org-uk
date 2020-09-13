using System;
using System.Collections.Generic;
using System.Linq;
using Stoolball.Teams;

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

            var byes = playerInnings.SingleOrDefault(x => x.PlayerIdentity.PlayerRole == PlayerRole.Byes);
            var wides = playerInnings.SingleOrDefault(x => x.PlayerIdentity.PlayerRole == PlayerRole.Wides);
            var noBalls = playerInnings.SingleOrDefault(x => x.PlayerIdentity.PlayerRole == PlayerRole.NoBalls);
            var bonusRuns = playerInnings.SingleOrDefault(x => x.PlayerIdentity.PlayerRole == PlayerRole.BonusRuns);
            playerInnings.RemoveAll(x => x.PlayerIdentity.PlayerRole != PlayerRole.Player);

            while (playerInnings.Where(x => x.PlayerIdentity.PlayerRole == PlayerRole.Player).Count() < playersPerTeam)
            {
                playerInnings.Add(new PlayerInnings { PlayerIdentity = new PlayerIdentity { PlayerRole = PlayerRole.Player } });
            }

            playerInnings.Add(byes != null ? byes : new PlayerInnings { PlayerIdentity = new PlayerIdentity { PlayerRole = PlayerRole.Byes } });
            playerInnings.Add(wides != null ? wides : new PlayerInnings { PlayerIdentity = new PlayerIdentity { PlayerRole = PlayerRole.Wides } });
            playerInnings.Add(noBalls != null ? noBalls : new PlayerInnings { PlayerIdentity = new PlayerIdentity { PlayerRole = PlayerRole.NoBalls } });
            playerInnings.Add(bonusRuns != null ? bonusRuns : new PlayerInnings { PlayerIdentity = new PlayerIdentity { PlayerRole = PlayerRole.BonusRuns } });
        }
    }
}
