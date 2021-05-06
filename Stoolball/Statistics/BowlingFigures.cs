using System;
using Stoolball.Matches;

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
        /// Gets or sets the number of runs conceded per wicket taken for a bowling performance
        /// </summary>
        /// <returns></returns>
        public decimal? BowlingAverage { get; set; }

        /// <summary>
        /// Gets or sets the average number of runs conceded per over
        /// </summary>
        /// <returns></returns>
        public decimal? BowlingEconomy { get; set; }

        /// <summary>
        /// Gets or sets the number of balls bowled per wicket
        /// </summary>
        /// <returns></returns>
        public decimal? BowlingStrikeRate { get; set; }
    }
}
