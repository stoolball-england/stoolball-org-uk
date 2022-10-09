using System;
using System.Collections.Generic;
using Stoolball.Listings;
using Stoolball.Navigation;
using Stoolball.Teams;

namespace Stoolball.MatchLocations
{
    public class MatchLocationFilter : IListingsFilter
    {
        public string? Query { get; set; }
        public List<Guid> SeasonIds { get; internal set; } = new List<Guid>();
        public List<Guid> ExcludeMatchLocationIds { get; internal set; } = new List<Guid>();
        public bool? HasActiveTeams { get; set; }
        public List<TeamType> TeamTypes { get; internal set; } = new List<TeamType>();
        public Paging Paging { get; set; } = new Paging();
    }
}