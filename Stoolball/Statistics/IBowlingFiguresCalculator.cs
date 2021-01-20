using System.Collections.Generic;
using Stoolball.Matches;

namespace Stoolball.Statistics
{
    public interface IBowlingFiguresCalculator
    {
        List<BowlingFigures> CalculateBowlingFigures(MatchInnings innings);
    }
}