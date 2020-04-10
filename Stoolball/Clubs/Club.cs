using Stoolball.Teams;
using System;
using System.Collections.Generic;

namespace Stoolball.Clubs
{
    public class Club
    {
        public int? ClubId { get; set; }
        public string ClubName { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Setter is needed to build the object with an ORM")]
        public List<Team> Teams { get; set; } = new List<Team>();
        public bool? PlaysOutdoors { get; set; }
        public bool? PlaysIndoors { get; set; }
        public string Website { get; set; }
        public string Twitter { get; set; }
        public string Facebook { get; set; }
        public string Instagram { get; set; }
        public string YouTube { get; set; }
        public bool ClubMark { get; set; }
        public int? HowManyPlayers { get; set; }
        public string ClubRoute { get; set; }
        public DateTimeOffset? DateCreated { get; set; }
        public DateTimeOffset? DateUpdated { get; set; }
        public Uri EntityUri
        {
            get { return new Uri($"https://www.stoolball.org.uk/id/club/{ClubId}"); }
        }
    }
}