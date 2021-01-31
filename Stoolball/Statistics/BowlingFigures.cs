using System;
using Stoolball.Matches;
using Stoolball.Teams;

namespace Stoolball.Statistics
{
    public class BowlingFigures
    {
        public Guid? BowlingFiguresId { get; set; }
        public MatchInnings MatchInnings { get; set; }
        public PlayerIdentity Bowler { get; set; }
        public decimal? Overs { get; set; }
        public int? Maidens { get; set; }
        public int? RunsConceded { get; set; }
        public int Wickets { get; set; }

        /// <summary>
        /// Converts a number of 8-ball overs into a number of balls bowled
        /// </summary>
        private static int OversToBalls(decimal overs)
        {
            // Work out how many balls were bowled - have to assume 8 ball overs, even though on rare occasions that may not be the case
            var completedOvers = (int)Math.Floor(overs);
            var ballsInCompletedOvers = (completedOvers * StatisticsConstants.BALLS_PER_OVER);
            var extraBalls = (int)((overs - completedOvers) * 10); // *10 changes 0.6 overs into 6 balls, for example
            return (ballsInCompletedOvers + extraBalls);
        }

        /// <summary>
        /// Gets the number of runs conceded per wicket taken for a bowling performance
        /// </summary>
        /// <returns></returns>
        public decimal? BowlingAverage()
        {
            if (Wickets > 0 && RunsConceded.HasValue)
            {
                return Math.Round((decimal)RunsConceded / Wickets, 2);
            }

            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the average number of runs conceded per over
        /// </summary>
        /// <returns></returns>
        public decimal? BowlingEconomy()
        {
            if (Overs > 0 && RunsConceded.HasValue)
            {
                var oversInDecimal = ((decimal)OversToBalls(Overs.Value)) / StatisticsConstants.BALLS_PER_OVER;
                return Math.Round((decimal)RunsConceded / oversInDecimal, 2);
            }

            else return null;
        }

        /// <summary>
        /// Gets the number of balls bowled per wicket
        /// </summary>
        /// <returns></returns>
        public decimal? BowlingStrikeRate()
        {
            if (Overs > 0 && Wickets > 0)
            {
                return Math.Round((decimal)OversToBalls(Overs.Value) / Wickets, 2);
            }

            else
            {
                return null;
            }
        }
    }
}
