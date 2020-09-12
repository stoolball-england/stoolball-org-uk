using System.Collections.Generic;
using System.Linq;
using Stoolball.Teams;

namespace Stoolball.Matches
{
    public class PlayerTypeSelector : IPlayerTypeSelector
    {
        /// <summary>
        /// Infer the player type of a <see cref="Match"/> from the teams involved
        /// </summary>
        public PlayerType SelectPlayerType(Match match)
        {
            if (match is null)
            {
                throw new System.ArgumentNullException(nameof(match));
            }

            // First strategy, look at the teams playing the match
            if (match.Teams.Count > 0)
            {
                return SelectPlayerTypeHelper(match.Teams.Select(x => x.Team.PlayerType).Distinct().ToList());
            }

            // Second strategy, if no teams it could be a cup match where the teams will be known later?
            // Look at the player types of the season it's in
            if (match.Season != null)
            {
                return SelectPlayerTypeHelper(new List<PlayerType> { match.Season.Competition.PlayerType });
            }

            // For a match without any seasons or any teams, default to mixed as it can involve the most players
            return PlayerType.Mixed;
        }

        /// <summary>
        /// Work out the player type of a match from the player types involved.
        /// </summary>
        private static PlayerType SelectPlayerTypeHelper(IList<PlayerType> playerTypes)
        {
            // only one type of player, that's the one to choose
            if (playerTypes.Count == 1) { return playerTypes[0]; }

            // check which player types are involved
            var mixed = playerTypes.Contains(PlayerType.Mixed);
            var ladies = playerTypes.Contains(PlayerType.Ladies);
            var men = playerTypes.Contains(PlayerType.Men);
            var girls = playerTypes.Contains(PlayerType.JuniorGirls);
            var boys = playerTypes.Contains(PlayerType.JuniorBoys);

            //if there's a mixed team involved, it's got to be a mixed match
            if (mixed) return PlayerType.Mixed;

            //if adult men and women, must be mixed
            if (ladies && men) return PlayerType.Mixed;

            // Any further matches must involve children, but maybe adults too!
            // Check first for single-sex matches involving adults
            if (ladies && girls) return PlayerType.Ladies;
            if (men && boys) return PlayerType.Men;

            // Any further matches involving adults must be mixed-sex
            if (ladies || men) return PlayerType.Mixed;

            // Now it's children only. Anything that only involved one gender
            // would have been caught by the very first test, so it must be mixed
            return PlayerType.JuniorMixed;
        }
    }
}
