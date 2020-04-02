using System;

namespace Stoolball.Schools
{
    public class School
    {
        public int? SchoolId { get; set; }
        public string SchoolName { get; set; }
        public bool? PlaysOutdoors { get; set; }
        public bool? PlaysIndoors { get; set; }
        public string Website { get; set; }
        public string Twitter { get; set; }
        public string Facebook { get; set; }
        public string Instagram { get; set; }
        public string YouTube { get; set; }
        public int? HowManyPlayers { get; set; }
        public string SchoolRoute { get; set; }
        public DateTimeOffset? DateCreated { get; set; }
        public DateTimeOffset? DateUpdated { get; set; }
        public Uri EntityUri
        {
            get { return new Uri($"https://www.stoolball.org.uk/id/school/{SchoolId}"); }
        }
    }
}