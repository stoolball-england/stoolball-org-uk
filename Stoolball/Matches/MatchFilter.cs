using System;
using System.Collections.Generic;

namespace Stoolball.Matches
{
    public class MatchFilter
    {
        public List<Guid> TeamIds { get; internal set; } = new List<Guid>();
        public List<Guid> CompetitionIds { get; internal set; } = new List<Guid>();
        public List<Guid> SeasonIds { get; internal set; } = new List<Guid>();
        public List<MatchType> MatchTypes { get; internal set; } = new List<MatchType>();
        public bool IncludeMatches { get; internal set; } = true;
        public bool IncludeTournamentMatches { get; set; }
        public bool IncludeTournaments { get; set; } = true;
        public DateTimeOffset? FromDate { get; set; }
        public DateTimeOffset? UntilDate { get; set; }
        public Guid? TournamentId { get; set; }
        public List<Guid> MatchLocationIds { get; internal set; } = new List<Guid>();
    }
}