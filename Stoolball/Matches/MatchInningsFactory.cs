using System;
using System.Collections.Generic;

namespace Stoolball.Matches
{
    public class MatchInningsFactory : IMatchInningsFactory
    {
        public MatchInnings CreateMatchInnings(Match match, Guid? battingMatchTeamId, Guid? bowlingMatchTeamId)
        {
            if (match is null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            var overSets = match.Tournament?.DefaultOverSets ?? match.Season?.DefaultOverSets;
            if (overSets == null || overSets.Count == 0)
            {
                overSets = new List<OverSet> { new OverSet { Overs = 12, BallsPerOver = 8 } }; // default if none provided
            }
            return new MatchInnings
            {
                MatchInningsId = Guid.NewGuid(),
                BattingMatchTeamId = battingMatchTeamId,
                BowlingMatchTeamId = bowlingMatchTeamId,
                InningsOrderInMatch = match.MatchInnings.Count + 1,
                OverSets = overSets
            };
        }
    }
}
