using System;
using System.Collections.Generic;

namespace Stoolball.Teams
{
    public class TeamQuery
    {
        public string Query { get; internal set; }
        public List<Guid> ExcludeTeamIds { get; internal set; } = new List<Guid>();
        public List<Guid> CompetitionIds { get; internal set; } = new List<Guid>();
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}