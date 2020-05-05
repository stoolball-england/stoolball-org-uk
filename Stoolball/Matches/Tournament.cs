using Stoolball.Audit;
using Stoolball.Competitions;
using Stoolball.MatchLocations;
using Stoolball.Teams;
using System;
using System.Collections.Generic;

namespace Stoolball.Matches
{
    public class Tournament : IAuditable
    {
        public Guid? TournamentId { get; set; }
        public string TournamentName { get; set; }
        public MatchLocation TournamentLocation { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public bool StartTimeIsKnown { get; set; }
        public PlayerType PlayerType { get; set; }
        public int? PlayersPerTeam { get; set; }
        public TournamentQualificationType QualificationType { get; set; }
        public List<TeamInMatch> Teams { get; internal set; } = new List<TeamInMatch>();
        public int? OversPerInningsDefault { get; set; }
        public int? MaximumTeamsInTournament { get; set; }
        public int? SpacesInTournament { get; set; }
        public string MatchNotes { get; set; }
        public string TournamentRoute { get; set; }
        public List<Season> Seasons { get; internal set; } = new List<Season>();
        public List<AuditRecord> History { get; internal set; } = new List<AuditRecord>();

        public Uri EntityUri
        {
            get { return new Uri($"https://www.stoolball.org.uk/id/tournament/{TournamentId}"); }
        }
    }
}
