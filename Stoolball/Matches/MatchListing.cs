using Stoolball.Teams;
using System;

namespace Stoolball.Matches
{
    public class MatchListing
    {
        public string MatchName { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public bool StartTimeIsKnown { get; set; }
        public MatchType MatchType { get; set; }
        public PlayerType PlayerType { get; set; }
        public TournamentQualificationType TournamentQualificationType { get; set; }
        public int? SpacesInTournament { get; set; }
        public string MatchRoute { get; set; }
    }
}
