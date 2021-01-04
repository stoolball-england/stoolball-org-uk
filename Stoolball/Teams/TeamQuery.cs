using System;
using System.Collections.Generic;

namespace Stoolball.Teams
{
    public class TeamQuery
    {
        public string Query { get; internal set; }
        public List<Guid> ExcludeTeamIds { get; internal set; } = new List<Guid>();
        public List<TeamType> TeamTypes { get; internal set; } = new List<TeamType>();
        public List<Guid> CompetitionIds { get; internal set; } = new List<Guid>();
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        /// <summary>
        /// Gets or sets whether to include teams that are in clubs
        /// </summary>
        public bool IncludeClubTeams { get; internal set; } = true;
    }
}