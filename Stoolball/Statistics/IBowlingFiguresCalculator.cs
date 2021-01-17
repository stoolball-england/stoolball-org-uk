using System.Collections.Generic;
using Stoolball.Matches;

namespace Stoolball.Statistics
{
    public interface IBowlingFiguresCalculator
    {
        IList<BowlingFigures> CalculateBowlingFigures(MatchInnings innings);
    }
}