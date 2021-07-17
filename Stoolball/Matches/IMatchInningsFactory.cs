using System;

namespace Stoolball.Matches
{
    public interface IMatchInningsFactory
    {
        MatchInnings CreateMatchInnings(Match match, Guid? battingMatchTeamId, Guid? bowlingMatchTeamId);
    }
}