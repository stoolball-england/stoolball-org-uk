using Stoolball.MatchLocations;
using System;
using System.Collections.Generic;

namespace Stoolball.Teams
{
    public class TeamListing
    {
        public Guid? TeamListingId { get; set; }
        public string ClubOrTeamName { get; set; }
        public bool Active { get; set; }
        public string ClubOrTeamRoute { get; set; }
        public List<PlayerType> PlayerTypes { get; internal set; } = new List<PlayerType>();
        public List<MatchLocation> MatchLocations { get; internal set; } = new List<MatchLocation>();
    }
}
