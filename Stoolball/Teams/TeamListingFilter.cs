using System;
using System.Collections.Generic;
using Stoolball.Navigation;

namespace Stoolball.Teams
{
    public class TeamListingFilter
    {
        public string Query { get; internal set; }
        public List<Guid> ExcludeTeamIds { get; internal set; } = new List<Guid>();
        public List<TeamType?> TeamTypes { get; internal set; } = new List<TeamType?>();
        public List<Guid> CompetitionIds { get; internal set; } = new List<Guid>();
        public Paging Paging { get; set; } = new Paging();

        /// <summary>
        /// Gets or sets whether to include active, inactive or all teams
        /// </summary>
        public bool? ActiveTeams { get; set; }
    }
}
