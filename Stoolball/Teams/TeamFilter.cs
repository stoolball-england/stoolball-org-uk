using System;
using System.Collections.Generic;
using Stoolball.Navigation;

namespace Stoolball.Teams
{
    public class TeamFilter
    {
        public string Query { get; internal set; }
        public List<Guid> ExcludeTeamIds { get; internal set; } = new List<Guid>();
        public List<TeamType> TeamTypes { get; internal set; } = new List<TeamType>();
        public List<Guid> CompetitionIds { get; internal set; } = new List<Guid>();
        public Paging Paging { get; set; } = new Paging();
        /// <summary>
        /// Gets or sets whether to include teams that are in clubs
        /// </summary>
        public bool IncludeClubTeams { get; internal set; } = true;

        /// <summary>
        /// Gets or sets whether to include active, inactive or all teams
        /// </summary>
        public bool? ActiveTeams { get; set; }
    }
}