using System;
using System.Collections.Generic;

namespace Stoolball.Umbraco.Data.Teams
{
    public class TeamQuery
    {
        public string Query { get; internal set; }
        public List<Guid> ExcludeTeamIds { get; internal set; } = new List<Guid>();
        public List<Guid> CompetitionIds { get; internal set; } = new List<Guid>();
    }
}