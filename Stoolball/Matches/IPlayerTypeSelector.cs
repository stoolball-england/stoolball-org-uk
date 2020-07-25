using Stoolball.Teams;

namespace Stoolball.Matches
{
    public interface IPlayerTypeSelector
    {
        PlayerType SelectPlayerType(Match match);
    }
}