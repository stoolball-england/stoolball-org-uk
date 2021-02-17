using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Stoolball.Matches;

namespace Stoolball.Statistics
{
    public static class StatisticsConstants
    {
        [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Except constants")]
        public const int BALLS_PER_OVER = 8;

        [SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible", Justification = "It's a constant value that C# can't represent as a constant")]
        public static List<DismissalType?> DISMISSALS_CREDITED_TO_BOWLER = new List<DismissalType?> { DismissalType.Caught, DismissalType.CaughtAndBowled, DismissalType.Bowled, DismissalType.BodyBeforeWicket, DismissalType.HitTheBallTwice };

        [SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible", Justification = "It's a constant value that C# can't represent as a constant")]
        public static List<DismissalType?> DISMISSALS_THAT_ARE_OUT = new List<DismissalType?> { DismissalType.Caught, DismissalType.CaughtAndBowled, DismissalType.Bowled, DismissalType.BodyBeforeWicket, DismissalType.HitTheBallTwice, DismissalType.RunOut, DismissalType.TimedOut, null };
    }
}