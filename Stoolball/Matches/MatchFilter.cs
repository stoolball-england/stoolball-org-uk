using System;
using System.Collections.Generic;
using Stoolball.Navigation;
using Stoolball.Teams;

namespace Stoolball.Matches
{
    public class MatchFilter
    {
        public string Query { get; set; }
        public List<Guid> TeamIds { get; internal set; } = new List<Guid>();
        public List<Guid> CompetitionIds { get; internal set; } = new List<Guid>();
        public List<Guid> SeasonIds { get; internal set; } = new List<Guid>();
        public List<MatchType> MatchTypes { get; internal set; } = new List<MatchType>();
        public List<PlayerType> PlayerTypes { get; internal set; } = new List<PlayerType>();
        public List<MatchResultType?> MatchResultTypes { get; internal set; } = new List<MatchResultType?>();
        public bool IncludeMatches { get; internal set; } = true;
        public bool IncludeTournamentMatches { get; set; }
        public bool IncludeTournaments { get; set; } = true;
        public DateTimeOffset? FromDate { get; set; }
        public DateTimeOffset? UntilDate { get; set; }
        public Guid? TournamentId { get; set; }
        public List<Guid> MatchLocationIds { get; internal set; } = new List<Guid>();
        public Paging Paging { get; set; } = new Paging();
    }
}