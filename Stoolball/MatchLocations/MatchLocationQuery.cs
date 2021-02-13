using System;
using System.Collections.Generic;
using Stoolball.Teams;

namespace Stoolball.MatchLocations
{
    public class MatchLocationQuery
    {
        public string Query { get; internal set; }
        public List<Guid> ExcludeMatchLocationIds { get; internal set; } = new List<Guid>();
        public bool? HasActiveTeams { get; set; }
        public List<TeamType> TeamTypes { get; internal set; } = new List<TeamType>();
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}