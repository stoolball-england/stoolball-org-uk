using System;
using Stoolball.Matches;
using Stoolball.Teams;

namespace Stoolball.Statistics
{
    public class PlayerInMatchStatisticsRecord
    {
        public Guid PlayerInMatchStatisticsId { get; set; }

        public Guid PlayerId { get; set; }

        public Guid PlayerIdentityId { get; set; }

        public string PlayerIdentityName { get; set; }

        public string PlayerRoute { get; set; }

        public Guid MatchId { get; set; }

        public DateTimeOffset MatchStartTime { get; set; }

        public MatchType MatchType { get; set; }

        public PlayerType MatchPlayerType { get; set; }

        public string MatchName { get; set; }

        public string MatchRoute { get; set; }

        public Guid? TournamentId { get; set; }

        public Guid? SeasonId { get; set; }

        public Guid? CompetitionId { get; set; }

        public Guid MatchTeamId { get; set; }

        public Guid TeamId { get; set; }

        public string TeamName { get; set; }

        public string TeamRoute { get; set; }

        public Guid OppositionTeamId { get; set; }

        public string OppositionTeamName { get; set; }

        public string OppositionTeamRoute { get; set; }

        public Guid? MatchLocationId { get; set; }

        public Guid MatchInningsId { get; set; }

        public int? MatchInningsRuns { get; set; }

        public int? MatchInningsWickets { get; set; }

        public int InningsOrderInMatch { get; set; }
        public bool InningsOrderIsKnown { get; set; }

        public int? OverNumberOfFirstOverBowled { get; set; }

        public int? BallsBowled { get; set; }

        public decimal? OversBowled { get; set; }

        public decimal? OversBowledDecimal { get; set; }

        public int? MaidensBowled { get; set; }

        public int? RunsConceded { get; set; }

        public bool? HasRunsConceded { get; set; }

        public int? Wickets { get; set; }

        public int? WicketsWithBowling { get; set; }

        public int? PlayerInningsInMatchInnings { get; set; }

        public int? BattingPosition { get; set; }

        public DismissalType DismissalType { get; set; }

        public bool? PlayerWasDismissed { get; set; }

        public Guid? BowledByPlayerIdentityId { get; set; }

        public string BowledByName { get; set; }

        public string BowledByRoute { get; set; }

        public Guid? CaughtByPlayerIdentityId { get; set; }

        public string CaughtByName { get; set; }

        public string CaughtByRoute { get; set; }

        public Guid? RunOutByPlayerIdentityId { get; set; }

        public string RunOutByName { get; set; }

        public string RunOutByRoute { get; set; }

        public int? RunsScored { get; set; }

        public int? BallsFaced { get; set; }

        public int? Catches { get; set; }

        public int? RunOuts { get; set; }

        public bool? WonMatch { get; set; }

        public bool? PlayerOfTheMatch { get; set; }

    }
}
