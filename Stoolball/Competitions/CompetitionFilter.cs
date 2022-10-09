using System.Collections.Generic;
using Stoolball.Listings;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Teams;

namespace Stoolball.Competitions
{
    public class CompetitionFilter : IListingsFilter
    {
        public string? Query { get; set; }
        public List<MatchType> MatchTypes { get; internal set; } = new List<MatchType>();
        public List<PlayerType> PlayerTypes { get; internal set; } = new List<PlayerType>();
        public bool? EnableTournaments { get; set; }
        public int? FromYear { get; set; }
        public int? UntilYear { get; set; }
        public Paging Paging { get; set; } = new Paging();
    }
}