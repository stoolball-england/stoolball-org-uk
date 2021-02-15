using System;
using System.Collections.Generic;
using System.Linq;
using Stoolball.Teams;

namespace Stoolball.Matches
{
    public class PlayerIdentityFinder : IPlayerIdentityFinder
    {
        public IEnumerable<PlayerIdentity> PlayerIdentitiesInMatch(Match match)
        {
            if (match is null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            var comparer = new PlayerIdentityEqualityComparer();
            var playerIdentities = new List<PlayerIdentity>();
            foreach (var innings in match.MatchInnings)
            {
                foreach (var playerInnings in innings.PlayerInnings)
                {
                    if (!playerIdentities.Contains(playerInnings.PlayerIdentity, comparer)) { playerIdentities.Add(playerInnings.PlayerIdentity); }
                    if (playerInnings.DismissedBy != null && !playerIdentities.Contains(playerInnings.DismissedBy, comparer)) { playerIdentities.Add(playerInnings.DismissedBy); }
                    if (playerInnings.Bowler != null && !playerIdentities.Contains(playerInnings.Bowler, comparer)) { playerIdentities.Add(playerInnings.Bowler); }
                }
                foreach (var over in innings.OversBowled)
                {
                    if (!playerIdentities.Contains(over.PlayerIdentity, comparer)) { playerIdentities.Add(over.PlayerIdentity); }
                }
            }
            foreach (var award in match.Awards)
            {
                if (!playerIdentities.Contains(award.PlayerIdentity, comparer)) { playerIdentities.Add(award.PlayerIdentity); }
            }
            return playerIdentities;
        }
    }
}
