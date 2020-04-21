using Stoolball.Audit;
using Stoolball.MatchLocations;
using Stoolball.Teams;
using System;
using System.Collections.Generic;

namespace Stoolball.Matches
{
    public class Match
    {
        public int? MatchId { get; set; }
        public string MatchName { get; set; }
        public bool UpdateMatchNameAutomatically { get; set; }
        public MatchLocation MatchLocation { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public bool StartTimeIsKnown { get; set; }
        public MatchType MatchType { get; set; }
        public PlayerType PlayerType { get; set; }
        public List<TeamInMatch> Teams { get; internal set; } = new List<TeamInMatch>();
        public int? PlayersPerTeam { get; set; }
        public Tournament Tournament { get; set; }
        public int? OversPerInningsDefault { get; set; }
        public int? OrderInTournament { get; set; }
        public bool InningsOrderIsKnown { get; set; }
        public MatchResultType? MatchResultType { get; set; }
        public List<MatchInnings> MatchInnings { get; internal set; } = new List<MatchInnings>();
        public string MatchNotes { get; set; }
        public string MatchRoute { get; set; }
        public List<AuditRecord> History { get; internal set; } = new List<AuditRecord>();
        public Uri EntityUri
        {
            get { return new Uri($"https://www.stoolball.org.uk/id/match/{MatchId}"); }
        }
    }
}
