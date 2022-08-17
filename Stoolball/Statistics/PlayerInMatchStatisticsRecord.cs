using System;
using Stoolball.Matches;

namespace Stoolball.Statistics
{
    public class PlayerInMatchStatisticsRecord
    {
        public Guid PlayerInMatchStatisticsId { get; set; }

        public Guid PlayerId { get; set; }

        public Guid PlayerIdentityId { get; set; }

        public Guid MatchId { get; set; }

        public Guid MatchTeamId { get; set; }

        public Guid OppositionTeamId { get; set; }

        public int MatchInningsPair { get; set; }
        public int? TeamRunsScored { get; set; }

        public int? TeamWicketsLost { get; set; }

        public int? TeamBonusOrPenaltyRunsAwarded { get; set; }

        public int? TeamRunsConceded { get; set; }
        public int? TeamNoBallsConceded { get; set; }
        public int? TeamWidesConceded { get; set; }
        public int? TeamByesConceded { get; set; }

        public int? TeamWicketsTaken { get; set; }

        public bool? WonToss { get; set; }

        public bool? BattedFirst { get; set; }

        public Guid? BowlingFiguresId { get; set; }

        public int? OverNumberOfFirstOverBowled { get; set; }

        public int? BallsBowled { get; set; }

        public decimal? Overs { get; set; }

        public int? Maidens { get; set; }

        public int? NoBalls { get; set; }

        public int? Wides { get; set; }

        public int? RunsConceded { get; set; }

        public bool HasRunsConceded { get; set; }

        public int? Wickets { get; set; }

        public int? WicketsWithBowling { get; set; }

        public int? PlayerInningsNumber { get; set; }

        public Guid? PlayerInningsId { get; set; }

        public int? BattingPosition { get; set; }

        public DismissalType? DismissalType { get; set; }

        public bool PlayerWasDismissed { get; set; }

        public Guid? BowledByPlayerIdentityId { get; set; }

        public Guid? CaughtByPlayerIdentityId { get; set; }

        public Guid? RunOutByPlayerIdentityId { get; set; }

        public int? RunsScored { get; set; }

        public int? BallsFaced { get; set; }

        public int Catches { get; set; }

        public int RunOuts { get; set; }

        public int? WonMatch { get; set; }

        public bool PlayerOfTheMatch { get; set; }
    }
}
