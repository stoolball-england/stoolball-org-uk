using System;
using System.Collections.Generic;

namespace Stoolball.Matches
{
    public class MatchInTournament
    {
        public Guid? MatchId { get; set; }
        public string? MatchName { get; set; }
        public List<TeamInTournament> Teams { get; internal set; } = new List<TeamInTournament>();
    }
}
