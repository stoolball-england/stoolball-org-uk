using System;

namespace Stoolball.Matches
{
    public class MatchNotFoundException : Exception
    {
        public MatchNotFoundException(Guid matchId) : base($"Match {matchId} was not found")
        {
        }
    }
}
