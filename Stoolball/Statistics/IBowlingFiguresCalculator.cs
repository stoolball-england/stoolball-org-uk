using System.Collections.Generic;
using Stoolball.Matches;

namespace Stoolball.Statistics
{
    public interface IBowlingFiguresCalculator
    {
        /// <summary>
        /// Calculate bowling figures (overs, maidens, runs, wickets) from data in a <see cref="MatchInnings"/>.
        /// </summary>
        List<BowlingFigures> CalculateBowlingFigures(MatchInnings innings);

        /// <summary>
        /// Gets the number of runs conceded per wicket taken for a bowling performance
        /// </summary>
        /// <returns></returns>
        decimal? BowlingAverage(BowlingFigures bowlingFigures);

        /// <summary>
        /// Gets the average number of runs conceded per over
        /// </summary>
        /// <returns></returns>
        decimal? BowlingEconomy(BowlingFigures bowlingFigures);

        /// <summary>
        /// Gets the number of balls bowled per wicket
        /// </summary>
        /// <returns></returns>
        decimal? BowlingStrikeRate(BowlingFigures bowlingFigures);
    }
}