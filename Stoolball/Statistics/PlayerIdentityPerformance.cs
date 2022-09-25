using System;

namespace Stoolball.Statistics
{
    public class PlayerIdentityPerformance
    {
        public Guid? MatchTeamid { get; set; }
        public int? MatchInningsPair { get; set; }
        public int? PlayerInningsNumber { get; set; }
        public int? BattingPosition { get; set; }
        public int? RunsScored { get; set; }
        public bool? PlayerWasDismissed { get; set; }
        public int? RunsConceded { get; set; }
        public int? Wickets { get; set; }
        public int Catches { get; set; }
        public int RunOuts { get; set; }
    }
}
