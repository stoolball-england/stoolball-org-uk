using System;

namespace Stoolball.Clubs
{
    public class Club
    {
        public int? ClubId { get; set; }
        public string ClubName { get; set; }
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
        public DateTime? DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }
    }
}